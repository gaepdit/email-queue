using System.Security.Claims;

namespace EmailQueue.API.Platform;

internal static class ClaimsExtensions
{
    extension(ClaimsPrincipal user)
    {
        public string ApiClientName() =>
            user.FindFirstValue(nameof(ApiClient.ClientName)) ?? throw new InvalidOperationException();

        public Guid ApiClientId() =>
            Guid.Parse(user.FindFirstValue(nameof(ApiClient.ClientId)) ?? throw new InvalidOperationException());
    }
}
