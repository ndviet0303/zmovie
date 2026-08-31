using ErrorOr;
using FluentValidation;
using MediatR;
using ZMovie.Application.Common;
using ZMovie.Application.Engagement;
using ZMovie.Domain.Personalization;

namespace ZMovie.Application.Personalization;

public interface IPersonalizationLearningRepository
{
    void Add(AssistantLearningEvent learningEvent);
    void AddRange(IEnumerable<AssistantLearningEvent> learningEvents);
    Task<AssistantLearningEvent?> FindLatestImpressionAsync(UserId userId, RecommendationId recommendationId, TitleId titleId, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}

public interface IPersonalizationQueries
{
    Task<IReadOnlyDictionary<Guid, double>> GetTitleScoresAsync(UserId userId, IReadOnlyDictionary<string, int> tokens, DateTimeOffset now, CancellationToken ct);
}

public interface IAssistantImpressionRecorder
{
    Task<Guid?> RecordImpressionAsync(Guid userId, string message, IReadOnlyList<string> titleSlugs, CancellationToken ct);
}

public sealed record RecordPersonalizationFeedbackCommand(
    Guid UserId,
    Guid RecommendationId,
    string Slug,
    string EventType) : ICommand<bool>;

public sealed class RecordPersonalizationFeedbackValidator : AbstractValidator<RecordPersonalizationFeedbackCommand>
{
    public RecordPersonalizationFeedbackValidator()
    {
        RuleFor(x => x.Slug).NotEmpty().MaximumLength(160);
        RuleFor(x => x.EventType).Must(eventType => FeedbackEventType.TryParse(eventType, out var parsed) && !parsed.IsImpression)
            .WithMessage("Unsupported assistant feedback event.");
    }
}

public sealed class RecordPersonalizationFeedbackHandler(
    IPersonalizationLearningRepository repository,
    ILibraryCatalogReader catalog,
    TimeProvider timeProvider) : IRequestHandler<RecordPersonalizationFeedbackCommand, ErrorOr<bool>>
{
    public async Task<ErrorOr<bool>> Handle(RecordPersonalizationFeedbackCommand request, CancellationToken ct)
    {
        if (!FeedbackEventType.TryParse(request.EventType, out var eventType) || eventType.IsImpression)
        {
            return false;
        }

        var titleIdGuid = await catalog.FindTitleIdAsync(request.Slug.Trim(), ct);
        if (titleIdGuid is null)
        {
            return Error.NotFound("catalog.title.not_found", "Title not found.");
        }

        var userId = new UserId(request.UserId);
        var recId = new RecommendationId(request.RecommendationId);
        var titleId = new TitleId(titleIdGuid.Value);

        var impression = await repository.FindLatestImpressionAsync(userId, recId, titleId, ct);
        if (impression is null)
        {
            return false;
        }

        var reward = RewardPolicy.GetReward(eventType);
        var feedbackEvent = AssistantLearningEvent.RecordFeedback(
            LearningEventId.New(),
            recId,
            userId,
            titleId,
            impression.Features,
            impression.Rank,
            eventType,
            reward,
            timeProvider.GetUtcNow());

        repository.Add(feedbackEvent);
        await repository.SaveChangesAsync(ct);
        return true;
    }
}
