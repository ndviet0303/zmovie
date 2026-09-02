using ErrorOr;
using MediatR;
using ZMovie.Application.Common;
using ZMovie.Domain.Engagement;

namespace ZMovie.Application.Engagement;

public sealed record ShortClipDto(
    Guid Id,
    string Title,
    string VideoUrl,
    string ThumbnailUrl,
    string TargetMovieSlug,
    int LikesCount,
    int SharesCount);

public sealed record UserLevelDto(
    int Level,
    string Title,
    int CurrentExp,
    int NextLevelExp,
    double ProgressPercentage);

public sealed record GetShortClipsFeedQuery(int Limit = 20) : IQuery<IReadOnlyList<ShortClipDto>>;
public sealed record GetUserLevelQuery(Guid UserId) : IQuery<UserLevelDto>;
public sealed record AwardUserExpCommand(Guid UserId, int Exp, string Reason) : ICommand<UserLevelDto>;

public interface IShortClipRepository
{
    Task<IReadOnlyList<ShortClip>> GetFeedAsync(int limit, CancellationToken ct);
    Task AddAsync(ShortClip clip, CancellationToken ct);
}

public interface IUserExpRepository
{
    Task<int> GetTotalExpAsync(Guid userId, CancellationToken ct);
    Task AddAsync(UserExpLedger ledger, CancellationToken ct);
}

public sealed class GetShortClipsFeedHandler(IShortClipRepository repo)
    : IRequestHandler<GetShortClipsFeedQuery, ErrorOr<IReadOnlyList<ShortClipDto>>>
{
    public async Task<ErrorOr<IReadOnlyList<ShortClipDto>>> Handle(GetShortClipsFeedQuery request, CancellationToken ct)
    {
        var clips = await repo.GetFeedAsync(request.Limit, ct);
        var dtos = clips.Select(c => new ShortClipDto(
            c.Id.Value,
            c.Title,
            c.VideoUrl,
            c.ThumbnailUrl,
            c.TargetMovieSlug,
            c.LikesCount,
            c.SharesCount)).ToList();

        // If no clips in DB yet, return curated initial short trailers
        if (dtos.Count == 0)
        {
            dtos =
            [
                new ShortClipDto(
                    Guid.NewGuid(),
                    "Na Tra: Ma Đồng Náo Hải - Cực Cháy! 🔥",
                    "https://commondatastorage.googleapis.com/gtv-videos-bucket/sample/ForBiggerBlazes.mp4",
                    "https://image.tmdb.org/t/p/w500/1E5baAaEse26fej7uHcjOgEE2t2.jpg",
                    "na-tra-ma-dong-nao-hai",
                    1250,
                    88),
                new ShortClipDto(
                    Guid.NewGuid(),
                    "Dune 2 - Phân cảnh cỡi giun cát huyền thoại 🏜️",
                    "https://commondatastorage.googleapis.com/gtv-videos-bucket/sample/ForBiggerEscapes.mp4",
                    "https://image.tmdb.org/t/p/w500/8b8R8l88Qje9dn9OE8PY05Nxl1X.jpg",
                    "dune-phan-hai",
                    3420,
                    210),
                new ShortClipDto(
                    Guid.NewGuid(),
                    "Spider-Man: Across the Spider-Verse - Vũ đạo thị giác 🕸️",
                    "https://commondatastorage.googleapis.com/gtv-videos-bucket/sample/ForBiggerJoyBlazes.mp4",
                    "https://image.tmdb.org/t/p/w500/8Vt6mWEReuy4Of61Lnj5Xj704m8.jpg",
                    "spider-man-du-hanh-vu-tru-nhen",
                    5100,
                    430),
            ];
        }

        return dtos;
    }
}

public sealed class UserLevelHandlers(IUserExpRepository repo, TimeProvider timeProvider)
    : IRequestHandler<GetUserLevelQuery, ErrorOr<UserLevelDto>>,
      IRequestHandler<AwardUserExpCommand, ErrorOr<UserLevelDto>>
{
    public async Task<ErrorOr<UserLevelDto>> Handle(GetUserLevelQuery request, CancellationToken ct)
    {
        var totalExp = await repo.GetTotalExpAsync(request.UserId, ct);
        var (level, title, current, next) = BilibiliLevelCalculator.Calculate(totalExp);
        var progress = next > 0 ? Math.Min(100.0, (double)current / next * 100.0) : 100.0;

        return new UserLevelDto(level, title, current, next, progress);
    }

    public async Task<ErrorOr<UserLevelDto>> Handle(AwardUserExpCommand request, CancellationToken ct)
    {
        var ledger = UserExpLedger.Create(
            UserExpLedgerId.New(),
            request.UserId,
            request.Exp,
            request.Reason,
            timeProvider.GetUtcNow());

        await repo.AddAsync(ledger, ct);

        var totalExp = await repo.GetTotalExpAsync(request.UserId, ct);
        var (level, title, current, next) = BilibiliLevelCalculator.Calculate(totalExp);
        var progress = next > 0 ? Math.Min(100.0, (double)current / next * 100.0) : 100.0;

        return new UserLevelDto(level, title, current, next, progress);
    }
}
