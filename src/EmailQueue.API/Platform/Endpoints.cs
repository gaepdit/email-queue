using EmailQueue.API.Database;
using Microsoft.AspNetCore.Authorization;

namespace EmailQueue.API.Platform;

internal static class Endpoints
{
    public static void MapEndpoints(this IEndpointRouteBuilder app)
    {
        // Status endpoints
        app.MapGet("/", () => Results.Ok());
        app.MapGet("/health", DbAvailable);
        app.MapGet("/version", () => Results.Ok(new { AppSettings.Version }));
        app.MapGet("/check-api-auth", CheckApiAuth);
    }

    [Authorize(AuthenticationSchemes = nameof(ApiKeyAuthenticationHandler))]
    private static IResult CheckApiAuth() => Results.Ok("Auth OK");

    private static async Task<IResult> DbAvailable(AppDbContext db) =>
        await db.Database.CanConnectAsync()
            ? TypedResults.Ok("OK")
            : TypedResults.InternalServerError("Database not available.");
}
