using System.Security.Claims;
using ZMovie.Application.Identity;
using ZMovie.Domain.Identity;

namespace ZMovie.Api;

public static class UserIdentityAdapter
{
    public static bool TryGetUserId(ClaimsPrincipal? principal, out Guid userId)
    {
        userId = default;
        var value = principal?.FindFirstValue(ClaimTypes.NameIdentifier);
        return !string.IsNullOrWhiteSpace(value) && Guid.TryParse(value, out userId);
    }

    public static Guid GetRequiredUserId(ClaimsPrincipal? principal)
    {
        if (!TryGetUserId(principal, out var userId))
        {
            throw new InvalidOperationException("User is not authenticated or claims do not contain a valid NameIdentifier.");
        }

        return userId;
    }

    public static Guid? GetUserIdOrNull(ClaimsPrincipal? principal) =>
        TryGetUserId(principal, out var userId) ? userId : null;

    public static AuthenticatedUser? ToAuthenticatedUser(ClaimsPrincipal? principal)
    {
        if (principal is null || !TryGetUserId(principal, out var userId))
        {
            return null;
        }

        var email = principal.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
        var name = principal.FindFirstValue(ClaimTypes.Name) ?? string.Empty;
        var picture = principal.FindFirstValue("picture");
        var role = Role.Normalize(principal.FindFirstValue(ClaimTypes.Role)).Value;

        return new AuthenticatedUser(userId, email, name, picture, role);
    }

    public static string GetAuthorName(ClaimsPrincipal? principal, string fallback = "ZMovie user") =>
        principal?.FindFirstValue(ClaimTypes.Name) ?? fallback;

    public static string GetOrCreateAnalyticsSessionId(HttpContext context)
    {
        const string cookieName = "zmovie.analytics-session";
        if (context.Request.Cookies.TryGetValue(cookieName, out var sessionId) && Guid.TryParse(sessionId, out _))
        {
            return sessionId;
        }

        sessionId = Guid.CreateVersion7().ToString("N");
        context.Response.Cookies.Append(cookieName, sessionId, new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Lax,
            Secure = context.Request.IsHttps,
            MaxAge = TimeSpan.FromDays(30),
        });
        return sessionId;
    }
}
