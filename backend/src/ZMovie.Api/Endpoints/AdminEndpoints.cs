using MediatR;
using System.Security.Claims;
using ZMovie.Api;
using ZMovie.Application.Administration;
using ZMovie.Domain.Identity;

namespace ZMovie.Api.Endpoints;

public static class AdminEndpoints
{
    public static IEndpointRouteBuilder MapAdminEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var admin = endpoints.MapGroup("/v1/admin")
            .WithTags("Admin")
            .RequireAuthorization(ZMovieRoles.AdminPolicy);

        admin.MapGet("/overview", async (ISender sender, CancellationToken ct) =>
                (await sender.Send(new GetAdminOverviewQuery(), ct)).ToApiResult())
            .Produces<AdminOverview>(StatusCodes.Status200OK)
            .ProducesApiErrors();

        MapTitles(admin);
        MapUsers(admin);
        MapReviews(admin);
        MapGenres(admin);

        return endpoints;
    }

    private static void MapTitles(RouteGroupBuilder admin)
    {
        admin.MapGet("/titles", async (ISender sender, string? q, string? genre, string? type, bool? featured, int? page, int? pageSize, CancellationToken ct) =>
                (await sender.Send(new ListAdminTitlesQuery(q, genre, type, featured, page, pageSize), ct)).ToApiResult())
            .Produces<PagedResult<AdminTitleSummary>>(StatusCodes.Status200OK)
            .ProducesApiErrors();
        admin.MapGet("/titles/{slug}", async (ISender sender, string slug, CancellationToken ct) =>
                (await sender.Send(new GetAdminTitleQuery(slug), ct)).ToApiResult())
            .Produces<AdminTitleDetail>(StatusCodes.Status200OK)
            .ProducesApiErrors();
        admin.MapPut("/titles/{slug}", async (ISender sender, string slug, AdminTitleEdit request, CancellationToken ct) =>
                (await sender.Send(new UpdateAdminTitleCommand(slug, request), ct)).ToApiResult())
            .Produces<AdminTitleDetail>(StatusCodes.Status200OK)
            .ProducesApiErrors();
        admin.MapPatch("/titles/{slug}/featured", async (ISender sender, string slug, SetFeaturedRequest request, CancellationToken ct) =>
                (await sender.Send(new SetAdminTitleFeaturedCommand(slug, request.Featured), ct)).ToApiResult())
            .Produces<AdminTitleDetail>(StatusCodes.Status200OK)
            .ProducesApiErrors();
        admin.MapDelete("/titles/{slug}", async (ISender sender, string slug, CancellationToken ct) =>
                (await sender.Send(new DeleteAdminTitleCommand(slug), ct)).ToApiResult())
            .Produces<bool>(StatusCodes.Status200OK)
            .ProducesApiErrors();
    }

    private static void MapUsers(RouteGroupBuilder admin)
    {
        admin.MapGet("/users", async (ISender sender, string? q, string? role, int? page, int? pageSize, CancellationToken ct) =>
                (await sender.Send(new ListAdminUsersQuery(q, role, page, pageSize), ct)).ToApiResult())
            .Produces<PagedResult<AdminUserSummary>>(StatusCodes.Status200OK)
            .ProducesApiErrors();
        admin.MapPatch("/users/{id:guid}/role", async (ISender sender, HttpContext context, Guid id, SetUserRoleRequest request, CancellationToken ct) =>
        {
            if (!TryGetUserId(context, out var actorId)) return Results.Unauthorized();
            return (await sender.Send(new SetUserRoleCommand(actorId, id, request.Role), ct)).ToApiResult();
        })
            .Produces<AdminUserSummary>(StatusCodes.Status200OK)
            .ProducesApiErrors();
    }

    private static void MapReviews(RouteGroupBuilder admin)
    {
        admin.MapGet("/reviews", async (ISender sender, string? q, int? maxRating, int? page, int? pageSize, CancellationToken ct) =>
                (await sender.Send(new ListAdminReviewsQuery(q, maxRating, page, pageSize), ct)).ToApiResult())
            .Produces<PagedResult<AdminReviewSummary>>(StatusCodes.Status200OK)
            .ProducesApiErrors();
        admin.MapDelete("/reviews/{id:guid}", async (ISender sender, Guid id, CancellationToken ct) =>
                (await sender.Send(new DeleteAdminReviewCommand(id), ct)).ToApiResult())
            .Produces<bool>(StatusCodes.Status200OK)
            .ProducesApiErrors();
    }

    private static void MapGenres(RouteGroupBuilder admin)
    {
        admin.MapGet("/genres", async (ISender sender, CancellationToken ct) =>
                (await sender.Send(new ListAdminGenresQuery(), ct)).ToApiResult())
            .Produces<List<AdminGenreSummary>>(StatusCodes.Status200OK)
            .ProducesApiErrors();
        admin.MapPost("/genres", async (ISender sender, CreateGenreRequest request, CancellationToken ct) =>
                (await sender.Send(new CreateAdminGenreCommand(request.Slug, request.Name), ct)).ToApiResult())
            .Produces<AdminGenreSummary>(StatusCodes.Status200OK)
            .ProducesApiErrors();
        admin.MapPut("/genres/{id:guid}", async (ISender sender, Guid id, UpdateGenreRequest request, CancellationToken ct) =>
                (await sender.Send(new UpdateAdminGenreCommand(id, request.Name), ct)).ToApiResult())
            .Produces<AdminGenreSummary>(StatusCodes.Status200OK)
            .ProducesApiErrors();
        admin.MapDelete("/genres/{id:guid}", async (ISender sender, Guid id, CancellationToken ct) =>
                (await sender.Send(new DeleteAdminGenreCommand(id), ct)).ToApiResult())
            .Produces<bool>(StatusCodes.Status200OK)
            .ProducesApiErrors();
    }

    private static bool TryGetUserId(HttpContext context, out Guid userId) =>
        Guid.TryParse(context.User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
}

public sealed record SetFeaturedRequest(bool Featured);
public sealed record SetUserRoleRequest(string Role);
public sealed record CreateGenreRequest(string Slug, string Name);
public sealed record UpdateGenreRequest(string Name);
