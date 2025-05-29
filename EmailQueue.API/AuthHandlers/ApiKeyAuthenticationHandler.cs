using EmailQueue.API.Settings;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace EmailQueue.API.AuthHandlers;

public class ApiKeyAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    IOptionsSnapshot<List<ApiClient>> apiClients,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    internal const string ClientIdHeaderName = "X-Client-ID";
    internal const string ApiKeyHeaderName = "X-API-Key";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(ClientIdHeaderName, out var clientIdHeaderValues))
            return Task.FromResult(AuthenticateResult.Fail("Client ID header is missing"));
        if (!Guid.TryParse(clientIdHeaderValues.FirstOrDefault(), out var providedClientId))
            return Task.FromResult(AuthenticateResult.Fail("Client ID header format is invalid"));

        if (!Request.Headers.TryGetValue(ApiKeyHeaderName, out var apiKeyHeaderValues))
            return Task.FromResult(AuthenticateResult.Fail("API Key header is missing"));
        var providedApiKey = apiKeyHeaderValues.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(providedApiKey))
            return Task.FromResult(AuthenticateResult.Fail("API Key is empty"));

        var matchingKey = apiClients.Value.FirstOrDefault(key => key.ClientId == providedClientId);

        if (matchingKey == null)
            return Task.FromResult(AuthenticateResult.Fail("Invalid Client ID"));
        if (!matchingKey.ApiKey.Equals(providedApiKey))
            return Task.FromResult(AuthenticateResult.Fail("Invalid API Key"));

        var claims = new List<Claim>
        {
            new(nameof(ApiClient.ClientName), matchingKey.ClientName),
            new(nameof(ApiClient.ClientId), matchingKey.ClientId.ToString()),
        };

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
