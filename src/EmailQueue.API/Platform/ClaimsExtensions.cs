using System.Security.Claims;

namespace EmailQueue.API.Platform;

internal static class ClaimsExtensions
{
    public static string ApiClientName(this ClaimsPrincipal user) =>
        user.FindFirstValue(nameof(ApiClient.ClientName)) ?? throw new InvalidOperationException();

    public static Guid ApiClientId(this ClaimsPrincipal user) =>
        Guid.Parse(user.FindFirstValue(nameof(ApiClient.ClientId)) ?? throw new InvalidOperationException());
}
