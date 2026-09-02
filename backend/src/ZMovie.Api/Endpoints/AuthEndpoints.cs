using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using ZMovie.Api;
using ZMovie.Application.Identity;
using ZMovie.Application.Engagement;
using ZMovie.Domain.Identity;

namespace ZMovie.Api.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/v1/auth/google", async (ISender sender, HttpContext context, GoogleCredentialRequest request, CancellationToken ct) =>
        {
            var result = await sender.Send(new SignInWithGoogleCommand(request.Credential), ct);
            if (result.IsError) return result.ToApiResult();

            var user = result.Value;
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.DisplayName),
                new Claim("picture", user.AvatarUrl ?? string.Empty),
                new Claim(ClaimTypes.Role, user.Role),
            };
            await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)));
            return Results.Ok(user);
        }).Produces<AuthenticatedUser>(StatusCodes.Status200OK).ProducesApiErrors();

        endpoints.MapGet("/v1/auth/me", (HttpContext context) =>
        {
            var authenticatedUser = UserIdentityAdapter.ToAuthenticatedUser(context.User);
            if (authenticatedUser is null) return Results.Unauthorized();

            return Results.Ok(authenticatedUser);
        }).RequireAuthorization().Produces<AuthenticatedUser>(StatusCodes.Status200OK).ProducesApiErrors();

        endpoints.MapPost("/v1/auth/logout", async (HttpContext context) =>
        {
            await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Results.NoContent();
        }).RequireAuthorization().Produces(StatusCodes.Status204NoContent).ProducesApiErrors();

        endpoints.MapGet("/v1/me/library", async (ISender sender, HttpContext context, string? locale, CancellationToken ct) =>
                (await sender.Send(new GetUserLibraryQuery(UserIdentityAdapter.GetRequiredUserId(context.User), locale?.StartsWith("en", StringComparison.OrdinalIgnoreCase) is true ? "en" : "vi"), ct)).ToApiResult())
            .RequireAuthorization().Produces<UserLibraryResponse>(StatusCodes.Status200OK).ProducesApiErrors();
        endpoints.MapPut("/v1/me/saved/{slug}", async (ISender sender, HttpContext context, string slug, CancellationToken ct) =>
                (await sender.Send(new SaveTitleCommand(UserIdentityAdapter.GetRequiredUserId(context.User), slug), ct)).ToApiResult())
            .RequireAuthorization().Produces<bool>(StatusCodes.Status200OK).ProducesApiErrors();
        endpoints.MapDelete("/v1/me/saved/{slug}", async (ISender sender, HttpContext context, string slug, CancellationToken ct) =>
                (await sender.Send(new RemoveSavedTitleCommand(UserIdentityAdapter.GetRequiredUserId(context.User), slug), ct)).ToApiResult())
            .RequireAuthorization().Produces<bool>(StatusCodes.Status200OK).ProducesApiErrors();
        endpoints.MapPost("/v1/me/history/{slug}", async (ISender sender, HttpContext context, string slug, WatchProgressRequest request, CancellationToken ct) =>
                (await sender.Send(new RecordWatchProgressCommand(UserIdentityAdapter.GetRequiredUserId(context.User), slug, request.EpisodeNumber, request.ProgressSeconds), ct)).ToApiResult())
            .RequireAuthorization().Produces<bool>(StatusCodes.Status200OK).ProducesApiErrors();
        endpoints.MapDelete("/v1/me/history/{slug}", async (ISender sender, HttpContext context, string slug, CancellationToken ct) =>
                (await sender.Send(new RemoveWatchHistoryCommand(UserIdentityAdapter.GetRequiredUserId(context.User), slug), ct)).ToApiResult())
            .RequireAuthorization().Produces<bool>(StatusCodes.Status200OK).ProducesApiErrors();
        endpoints.MapDelete("/v1/me/history", async (ISender sender, HttpContext context, CancellationToken ct) =>
                (await sender.Send(new ClearWatchHistoryCommand(UserIdentityAdapter.GetRequiredUserId(context.User)), ct)).ToApiResult())
            .RequireAuthorization().Produces<bool>(StatusCodes.Status200OK).ProducesApiErrors();
        endpoints.MapPut("/v1/me/titles/{slug}/review", async (ISender sender, HttpContext context, string slug, SubmitTitleReviewRequest request, CancellationToken ct) =>
                (await sender.Send(new SubmitTitleReviewCommand(UserIdentityAdapter.GetRequiredUserId(context.User), UserIdentityAdapter.GetAuthorName(context.User), slug, request.Rating, request.Comment), ct)).ToApiResult())
            .RequireAuthorization().Produces<bool>(StatusCodes.Status200OK).ProducesApiErrors();
        endpoints.MapDelete("/v1/me/titles/{slug}/review", async (ISender sender, HttpContext context, string slug, CancellationToken ct) =>
                (await sender.Send(new RemoveTitleReviewCommand(UserIdentityAdapter.GetRequiredUserId(context.User), slug), ct)).ToApiResult())
            .RequireAuthorization().Produces<bool>(StatusCodes.Status200OK).ProducesApiErrors();

        return endpoints;
    }
}

public sealed record GoogleCredentialRequest(string Credential);
public sealed record WatchProgressRequest(int? EpisodeNumber, double ProgressSeconds);
public sealed record SubmitTitleReviewRequest(int Rating, string? Comment);
