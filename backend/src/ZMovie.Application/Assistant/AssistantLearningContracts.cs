using ErrorOr;
using FluentValidation;
using MediatR;
using ZMovie.Application.Common;
using ZMovie.Application.Engagement;

namespace ZMovie.Application.Assistant;

public static class AssistantFeedbackEvents
{
    private static readonly IReadOnlyDictionary<string, double> Rewards = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
    {
        ["click"] = 0.5,
        ["save"] = 2,
        ["watch"] = 3,
        ["complete"] = 5,
        ["like"] = 4,
        ["dislike"] = -4,
    };

    public static bool TryGetReward(string? eventType, out double reward)
    {
        if (eventType is not null && Rewards.TryGetValue(eventType.Trim(), out reward)) return true;
        reward = 0;
        return false;
    }
}

public interface IAssistantLearningStore
{
    Task<Guid?> RecordImpressionAsync(Guid userId, string message, IReadOnlyList<AssistantCatalogTitle> suggestions, CancellationToken ct);
    Task<bool> RecordFeedbackAsync(Guid userId, Guid recommendationId, Guid titleId, string eventType, CancellationToken ct);
    Task<IReadOnlyDictionary<Guid, double>> GetTitleScoresAsync(Guid userId, IReadOnlyDictionary<string, int> tokens, CancellationToken ct);
}

public sealed record RecordAssistantFeedbackCommand(Guid UserId, Guid RecommendationId, string Slug, string EventType) : ICommand<bool>;

public sealed class RecordAssistantFeedbackValidator : AbstractValidator<RecordAssistantFeedbackCommand>
{
    public RecordAssistantFeedbackValidator()
    {
        RuleFor(x => x.Slug).NotEmpty().MaximumLength(160);
        RuleFor(x => x.EventType).Must(eventType => AssistantFeedbackEvents.TryGetReward(eventType, out _))
            .WithMessage("Unsupported assistant feedback event.");
    }
}

public sealed class RecordAssistantFeedbackHandler(IAssistantLearningStore learning, ILibraryCatalogReader catalog) : IRequestHandler<RecordAssistantFeedbackCommand, ErrorOr<bool>>
{
    public async Task<ErrorOr<bool>> Handle(RecordAssistantFeedbackCommand request, CancellationToken ct)
    {
        var titleId = await catalog.FindTitleIdAsync(request.Slug.Trim(), ct);
        if (titleId is null) return Error.NotFound("catalog.title.not_found", "Title not found.");
        return await learning.RecordFeedbackAsync(request.UserId, request.RecommendationId, titleId.Value, request.EventType, ct);
    }
}
