using ErrorOr;
using MediatR;
using ZMovie.Application.Common;
using ZMovie.Domain.Engagement;

namespace ZMovie.Application.Engagement;

public sealed record DanmakuItemDto(
    Guid Id,
    string TitleSlug,
    int EpisodeNumber,
    int TimeSeconds,
    string Content,
    string Color,
    string AuthorName,
    DateTimeOffset CreatedAt);

public sealed record GetTimedDanmakuQuery(
    string TitleSlug,
    int EpisodeNumber,
    int? FromSeconds = null,
    int? ToSeconds = null) : IQuery<IReadOnlyList<DanmakuItemDto>>;

public sealed record SendDanmakuCommand(
    string TitleSlug,
    int EpisodeNumber,
    int TimeSeconds,
    string Content,
    string Color,
    Guid? UserId,
    string AuthorName) : ICommand<DanmakuItemDto>;

public interface IDanmakuRepository
{
    Task<IReadOnlyList<DanmakuComment>> GetTimedCommentsAsync(
        string titleSlug,
        int episodeNumber,
        int? fromSeconds,
        int? toSeconds,
        CancellationToken ct);

    Task AddAsync(DanmakuComment comment, CancellationToken ct);
}

public sealed class GetTimedDanmakuHandler(IDanmakuRepository repository)
    : IRequestHandler<GetTimedDanmakuQuery, ErrorOr<IReadOnlyList<DanmakuItemDto>>>
{
    public async Task<ErrorOr<IReadOnlyList<DanmakuItemDto>>> Handle(GetTimedDanmakuQuery request, CancellationToken ct)
    {
        var comments = await repository.GetTimedCommentsAsync(
            request.TitleSlug,
            request.EpisodeNumber,
            request.FromSeconds,
            request.ToSeconds,
            ct);

        var dtos = comments.Select(c => new DanmakuItemDto(
            c.Id.Value,
            c.TitleSlug,
            c.EpisodeNumber,
            c.TimeSeconds,
            c.Content,
            c.Color,
            c.AuthorName,
            c.CreatedAt)).ToList();

        return dtos;
    }
}

public sealed class SendDanmakuHandler(IDanmakuRepository repository, TimeProvider timeProvider)
    : IRequestHandler<SendDanmakuCommand, ErrorOr<DanmakuItemDto>>
{
    public async Task<ErrorOr<DanmakuItemDto>> Handle(SendDanmakuCommand request, CancellationToken ct)
    {
        var content = request.Content?.Trim();
        if (string.IsNullOrWhiteSpace(content))
        {
            return Error.Validation("danmaku.content.empty", "Nội dung bình luận không được để trống.");
        }

        var comment = DanmakuComment.Create(
            DanmakuCommentId.New(),
            request.TitleSlug,
            request.EpisodeNumber,
            request.TimeSeconds,
            content,
            request.Color,
            request.UserId,
            request.AuthorName,
            timeProvider.GetUtcNow());

        await repository.AddAsync(comment, ct);

        return new DanmakuItemDto(
            comment.Id.Value,
            comment.TitleSlug,
            comment.EpisodeNumber,
            comment.TimeSeconds,
            comment.Content,
            comment.Color,
            comment.AuthorName,
            comment.CreatedAt);
    }
}
