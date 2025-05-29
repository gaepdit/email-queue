using EmailQueue.API.AuthHandlers;
using EmailQueue.API.Settings;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace EmailQueue.API.Tests.AuthHandlers;

public class ApiKeyAuthenticationHandlerTests
{
    private ApiKeyAuthenticationHandler _sut;
    private ILoggerFactory _loggerFactory;
    private HttpContext _httpContext;
    private const string ValidApiKey = "valid-api-key";

    private readonly ApiClient _testKey = new()
    {
        ClientName = "Test Client",
        ClientId = Guid.NewGuid(),
        ApiKey = ValidApiKey,
    };

    [SetUp]
    public void Setup()
    {
        // Set up mocks
        const string testScheme = "TestApiKey";

        var options = Substitute.For<IOptionsMonitor<AuthenticationSchemeOptions>>();
        options.Get(testScheme).Returns(new AuthenticationSchemeOptions());

        var apiKeys = Substitute.For<IOptionsSnapshot<List<ApiClient>>>();
        apiKeys.Value.Returns([_testKey]);

        _loggerFactory = Substitute.For<ILoggerFactory>();
        var urlEncoder = Substitute.For<UrlEncoder>();

        // Create HTTP context
        _httpContext = new DefaultHttpContext();

        // Create handler
        _sut = new ApiKeyAuthenticationHandler(options, apiKeys, _loggerFactory, urlEncoder);

        // Initialize handler with HTTP context
        var scheme = new AuthenticationScheme(testScheme, testScheme, typeof(ApiKeyAuthenticationHandler));
        _sut.InitializeAsync(scheme, _httpContext).GetAwaiter().GetResult();
    }

    [TearDown]
    public void Cleanup()
    {
        // Dispose of ILoggerFactory
        _loggerFactory.Dispose();
    }

    [Test]
    public async Task HandleAuthenticateAsync_WithMissingClientId_ReturnsFailure()
    {
        // Arrange
        _httpContext.Request.Headers[ApiKeyAuthenticationHandler.ApiKeyHeaderName] = ValidApiKey;

        // Act
        var result = await _sut.AuthenticateAsync();

        // Assert
        using var scope = new AssertionScope();
        result.Succeeded.Should().BeFalse();
        result.Failure!.Message.Should().Be("Client ID header is missing");
    }

    [Test]
    public async Task HandleAuthenticateAsync_WithMisformattedClientId_ReturnsFailure()
    {
        // Arrange
        _httpContext.Request.Headers[ApiKeyAuthenticationHandler.ClientIdHeaderName] = string.Empty;
        _httpContext.Request.Headers[ApiKeyAuthenticationHandler.ApiKeyHeaderName] = ValidApiKey;

        // Act
        var result = await _sut.AuthenticateAsync();

        // Assert
        using var scope = new AssertionScope();
        result.Succeeded.Should().BeFalse();
        result.Failure!.Message.Should().Be("Client ID header format is invalid");
    }

    [Test]
    public async Task HandleAuthenticateAsync_WithMissingApiKey_ReturnsFailure()
    {
        // Arrange
        _httpContext.Request.Headers[ApiKeyAuthenticationHandler.ClientIdHeaderName] = _testKey.ClientId.ToString();

        // Act
        var result = await _sut.AuthenticateAsync();

        // Assert
        using var scope = new AssertionScope();
        result.Succeeded.Should().BeFalse();
        result.Failure!.Message.Should().Be("API Key header is missing");
    }

    [Test]
    public async Task HandleAuthenticateAsync_WithEmptyApiKey_ReturnsFailure()
    {
        // Arrange
        _httpContext.Request.Headers[ApiKeyAuthenticationHandler.ClientIdHeaderName] = _testKey.ClientId.ToString();
        _httpContext.Request.Headers[ApiKeyAuthenticationHandler.ApiKeyHeaderName] = string.Empty;

        // Act
        var result = await _sut.AuthenticateAsync();

        // Assert
        using var scope = new AssertionScope();
        result.Succeeded.Should().BeFalse();
        result.Failure!.Message.Should().Be("API Key is empty");
    }

    [Test]
    public async Task HandleAuthenticateAsync_WithInvalidClientId_ReturnsFailure()
    {
        // Arrange
        _httpContext.Request.Headers[ApiKeyAuthenticationHandler.ClientIdHeaderName] = Guid.NewGuid().ToString();
        _httpContext.Request.Headers[ApiKeyAuthenticationHandler.ApiKeyHeaderName] = ValidApiKey;

        // Act
        var result = await _sut.AuthenticateAsync();

        // Assert
        using var scope = new AssertionScope();
        result.Succeeded.Should().BeFalse();
        result.Failure!.Message.Should().Be("Invalid Client ID");
    }

    [Test]
    public async Task HandleAuthenticateAsync_WithInvalidApiKey_ReturnsFailure()
    {
        // Arrange
        _httpContext.Request.Headers[ApiKeyAuthenticationHandler.ClientIdHeaderName] = _testKey.ClientId.ToString();
        _httpContext.Request.Headers[ApiKeyAuthenticationHandler.ApiKeyHeaderName] = "invalid-key";

        // Act
        var result = await _sut.AuthenticateAsync();

        // Assert
        using var scope = new AssertionScope();
        result.Succeeded.Should().BeFalse();
        result.Failure!.Message.Should().Be("Invalid API Key");
    }

    [Test]
    public async Task HandleAuthenticateAsync_WithValidApiKey_ReturnsSuccess()
    {
        // Arrange
        _httpContext.Request.Headers[ApiKeyAuthenticationHandler.ClientIdHeaderName] = _testKey.ClientId.ToString();
        _httpContext.Request.Headers[ApiKeyAuthenticationHandler.ApiKeyHeaderName] = ValidApiKey;

        // Act
        var result = await _sut.AuthenticateAsync();

        // Assert
        using var scope = new AssertionScope();
        result.Succeeded.Should().BeTrue();
        var identity = result.Principal?.Identity as ClaimsIdentity;
        identity.Should().NotBeNull();
        Debug.Assert(identity != null);
        identity.Claims.Should().Contain(c => c.Type == nameof(ApiClient.ClientName) && c.Value == _testKey.ClientName);
        identity.Claims.Should()
            .Contain(c => c.Type == nameof(ApiClient.ClientId) && c.Value == _testKey.ClientId.ToString());
    }
}
