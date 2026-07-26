using ErrorOr;
using FluentValidation;
using MediatR;
using ZMovie.Application.Common;
using ZMovie.Domain.Identity;

namespace ZMovie.Application.Administration;

public static class AdminTitleTypes
{
    public const string Movie = "movie";
    public const string Series = "series";
    public static bool IsKnown(string? type) => type?.Trim().ToLowerInvariant() is Movie or Series;
}

public sealed record UpdateAdminTitleCommand(string Slug, AdminTitleEdit Edit) : ICommand<AdminTitleDetail>;
public sealed class UpdateAdminTitleValidator : AbstractValidator<UpdateAdminTitleCommand>
{
    public UpdateAdminTitleValidator()
    {
        RuleFor(x => x.Slug).NotEmpty().MaximumLength(160);
        RuleFor(x => x.Edit).NotNull();
        RuleFor(x => x.Edit.VietnameseTitle).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Edit.EnglishTitle).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Edit.VietnameseSynopsis).NotEmpty().MaximumLength(4_000);
        RuleFor(x => x.Edit.EnglishSynopsis).NotEmpty().MaximumLength(4_000);
        RuleFor(x => x.Edit.Genre).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Edit.Type).Must(AdminTitleTypes.IsKnown).WithMessage("Type must be 'movie' or 'series'.");
        RuleFor(x => x.Edit.PosterUrl).NotEmpty().MaximumLength(2_000).Must(BeAnHttpUrl).WithMessage("Poster URL must be an absolute http(s) URL.");
        RuleFor(x => x.Edit.Year).InclusiveBetween(1888, 2100);
        RuleFor(x => x.Edit.RuntimeMinutes).InclusiveBetween(0, 100_000);
    }

    private static bool BeAnHttpUrl(string? value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}
public sealed class UpdateAdminTitleHandler(IAdminStore store) : IRequestHandler<UpdateAdminTitleCommand, ErrorOr<AdminTitleDetail>>
{
    public async Task<ErrorOr<AdminTitleDetail>> Handle(UpdateAdminTitleCommand request, CancellationToken ct)
    {
        var edit = request.Edit with
        {
            VietnameseTitle = request.Edit.VietnameseTitle.Trim(),
            EnglishTitle = request.Edit.EnglishTitle.Trim(),
            VietnameseSynopsis = request.Edit.VietnameseSynopsis.Trim(),
            EnglishSynopsis = request.Edit.EnglishSynopsis.Trim(),
            Genre = request.Edit.Genre.Trim(),
            Type = request.Edit.Type.Trim().ToLowerInvariant(),
            PosterUrl = request.Edit.PosterUrl.Trim(),
        };
        return await store.UpdateTitleAsync(request.Slug.Trim(), edit, ct) is { } title
            ? title
            : Error.NotFound("admin.title.not_found", "Catalog title not found.");
    }
}

public sealed record SetAdminTitleFeaturedCommand(string Slug, bool Featured) : ICommand<AdminTitleDetail>;
public sealed class SetAdminTitleFeaturedValidator : AbstractValidator<SetAdminTitleFeaturedCommand>
{
    public SetAdminTitleFeaturedValidator() => RuleFor(x => x.Slug).NotEmpty().MaximumLength(160);
}
public sealed class SetAdminTitleFeaturedHandler(IAdminStore store) : IRequestHandler<SetAdminTitleFeaturedCommand, ErrorOr<AdminTitleDetail>>
{
    public async Task<ErrorOr<AdminTitleDetail>> Handle(SetAdminTitleFeaturedCommand request, CancellationToken ct) =>
        await store.SetTitleFeaturedAsync(request.Slug.Trim(), request.Featured, ct) is { } title
            ? title
            : Error.NotFound("admin.title.not_found", "Catalog title not found.");
}

public sealed record DeleteAdminTitleCommand(string Slug) : ICommand<bool>;
public sealed class DeleteAdminTitleValidator : AbstractValidator<DeleteAdminTitleCommand>
{
    public DeleteAdminTitleValidator() => RuleFor(x => x.Slug).NotEmpty().MaximumLength(160);
}
public sealed class DeleteAdminTitleHandler(IAdminStore store) : IRequestHandler<DeleteAdminTitleCommand, ErrorOr<bool>>
{
    public async Task<ErrorOr<bool>> Handle(DeleteAdminTitleCommand request, CancellationToken ct) =>
        await store.DeleteTitleAsync(request.Slug.Trim(), ct)
            ? true
            : Error.NotFound("admin.title.not_found", "Catalog title not found.");
}

public sealed record SetUserRoleCommand(Guid ActorId, Guid UserId, string Role) : ICommand<AdminUserSummary>;
public sealed class SetUserRoleValidator : AbstractValidator<SetUserRoleCommand>
{
    public SetUserRoleValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Role).Must(ZMovieRoles.IsKnown).WithMessage("Role must be 'member' or 'admin'.");
    }
}
public sealed class SetUserRoleHandler(IAdminStore store) : IRequestHandler<SetUserRoleCommand, ErrorOr<AdminUserSummary>>
{
    public async Task<ErrorOr<AdminUserSummary>> Handle(SetUserRoleCommand request, CancellationToken ct)
    {
        var role = ZMovieRoles.Normalize(request.Role);
        var target = await store.GetUserAsync(request.UserId, ct);
        if (target is null) return Error.NotFound("admin.user.not_found", "User not found.");
        if (string.Equals(target.Role, role, StringComparison.Ordinal)) return target;

        var isDemotion = ZMovieRoles.IsAdmin(target.Role) && !ZMovieRoles.IsAdmin(role);
        // Locking yourself out of /v1/admin is not recoverable through the UI.
        if (isDemotion && request.ActorId == request.UserId)
            return Error.Forbidden("admin.user.self_demotion", "You cannot remove your own admin role.");

        // The last-admin check runs inside the store's write transaction, not here, so it
        // cannot be defeated by two concurrent demotions each reading a stale count.
        var result = await store.SetUserRoleAsync(request.UserId, role, isDemotion, ct);
        return result.Outcome switch
        {
            SetRoleOutcome.Updated => result.User!,
            SetRoleOutcome.LastAdmin => Error.Conflict("admin.user.last_admin", "The last remaining admin cannot be demoted."),
            _ => Error.NotFound("admin.user.not_found", "User not found."),
        };
    }
}

public sealed record DeleteAdminReviewCommand(Guid ReviewId) : ICommand<bool>;
public sealed class DeleteAdminReviewValidator : AbstractValidator<DeleteAdminReviewCommand>
{
    public DeleteAdminReviewValidator() => RuleFor(x => x.ReviewId).NotEmpty();
}
public sealed class DeleteAdminReviewHandler(IAdminStore store) : IRequestHandler<DeleteAdminReviewCommand, ErrorOr<bool>>
{
    public async Task<ErrorOr<bool>> Handle(DeleteAdminReviewCommand request, CancellationToken ct) =>
        await store.DeleteReviewAsync(request.ReviewId, ct)
            ? true
            : Error.NotFound("admin.review.not_found", "Review not found.");
}

public sealed record CreateAdminGenreCommand(string Slug, string Name) : ICommand<AdminGenreSummary>;
public sealed class CreateAdminGenreValidator : AbstractValidator<CreateAdminGenreCommand>
{
    public CreateAdminGenreValidator()
    {
        RuleFor(x => x.Slug).NotEmpty().MaximumLength(160)
            .Matches("^[a-z0-9]+(-[a-z0-9]+)*$").WithMessage("Slug must be lowercase words separated by single hyphens.");
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
    }
}
public sealed class CreateAdminGenreHandler(IAdminStore store) : IRequestHandler<CreateAdminGenreCommand, ErrorOr<AdminGenreSummary>>
{
    public async Task<ErrorOr<AdminGenreSummary>> Handle(CreateAdminGenreCommand request, CancellationToken ct) =>
        await store.CreateGenreAsync(request.Slug.Trim().ToLowerInvariant(), request.Name.Trim(), ct) is { } genre
            ? genre
            : Error.Conflict("admin.genre.duplicate_slug", "A genre with that slug already exists.");
}

public sealed record UpdateAdminGenreCommand(Guid Id, string Name) : ICommand<AdminGenreSummary>;
public sealed class UpdateAdminGenreValidator : AbstractValidator<UpdateAdminGenreCommand>
{
    public UpdateAdminGenreValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
    }
}
public sealed class UpdateAdminGenreHandler(IAdminStore store) : IRequestHandler<UpdateAdminGenreCommand, ErrorOr<AdminGenreSummary>>
{
    public async Task<ErrorOr<AdminGenreSummary>> Handle(UpdateAdminGenreCommand request, CancellationToken ct) =>
        await store.UpdateGenreAsync(request.Id, request.Name.Trim(), ct) is { } genre
            ? genre
            : Error.NotFound("admin.genre.not_found", "Genre not found.");
}

public sealed record DeleteAdminGenreCommand(Guid Id) : ICommand<bool>;
public sealed class DeleteAdminGenreValidator : AbstractValidator<DeleteAdminGenreCommand>
{
    public DeleteAdminGenreValidator() => RuleFor(x => x.Id).NotEmpty();
}
public sealed class DeleteAdminGenreHandler(IAdminStore store) : IRequestHandler<DeleteAdminGenreCommand, ErrorOr<bool>>
{
    public async Task<ErrorOr<bool>> Handle(DeleteAdminGenreCommand request, CancellationToken ct) =>
        await store.DeleteGenreAsync(request.Id, ct)
            ? true
            : Error.NotFound("admin.genre.not_found", "Genre not found.");
}
