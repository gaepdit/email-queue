using Microsoft.Extensions.Options;
using Sample.WebApp.Settings;
using System.Text;
using System.Text.Json;

namespace Sample.WebApp.Services;

public class EmailQueueApiService(
    IHttpClientFactory httpClientFactory,
    IOptionsSnapshot<EmailQueueApi> apiSettings,
    ILogger<EmailQueueApiService> logger)
{
    public async Task<IEnumerable<EmailTaskViewModel>> GetBatchDetailsAsync(Guid batchId)
    {
        logger.LogInformation("Getting batch {BatchId}", batchId);
        using var httpClient = httpClientFactory.CreateClient(nameof(EmailQueueApiService));
        httpClient.DefaultRequestHeaders.Add("X-Client-ID", apiSettings.Value.ClientId.ToString());
        httpClient.DefaultRequestHeaders.Add("X-API-Key", apiSettings.Value.ApiKey);
        var requestPayload = new { BatchId = batchId };
        using var response = await httpClient.PostAsync(UriCombine(apiSettings.Value.BaseUrl, "batch-details"),
            new StringContent(JsonSerializer.Serialize(requestPayload), Encoding.UTF8, "application/json"));
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<EmailTaskViewModel>>().ConfigureAwait(false) ?? [];
    }

    public async Task<IEnumerable<BatchStatusViewModel>> GetAllBatchesAsync()
    {
        logger.LogInformation("Getting all batches");
        using var httpClient = httpClientFactory.CreateClient(nameof(EmailQueueApiService));
        httpClient.DefaultRequestHeaders.Add("X-Client-ID", apiSettings.Value.ClientId.ToString());
        httpClient.DefaultRequestHeaders.Add("X-API-Key", apiSettings.Value.ApiKey);
        using var response = await httpClient.GetAsync(UriCombine(apiSettings.Value.BaseUrl, "all-batches"));
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<BatchStatusViewModel>>().ConfigureAwait(false) ?? [];
    }

    public async Task<BatchStatusViewModel?> GetBatchStatusAsync(Guid batchId)
    {
        logger.LogInformation("Getting batch {BatchId}", batchId);
        using var httpClient = httpClientFactory.CreateClient(nameof(EmailQueueApiService));
        httpClient.DefaultRequestHeaders.Add("X-Client-ID", apiSettings.Value.ClientId.ToString());
        httpClient.DefaultRequestHeaders.Add("X-API-Key", apiSettings.Value.ApiKey);
        var requestPayload = new { BatchId = batchId };
        using var response = await httpClient.PostAsync(UriCombine(apiSettings.Value.BaseUrl, "batch-status"),
            new StringContent(JsonSerializer.Serialize(requestPayload), Encoding.UTF8, "application/json"));
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<BatchStatusViewModel>().ConfigureAwait(false);
    }

    private static Uri UriCombine(string baseUrl, string? endpoint)
    {
        var baseUri = new Uri(baseUrl);
        if (!baseUri.IsAbsoluteUri) throw new ArgumentOutOfRangeException(nameof(baseUrl));

        if (string.IsNullOrEmpty(endpoint)) return baseUri;

        const char separator = '/';
        return new Uri(baseUri.ToString().TrimEnd(separator) + separator + endpoint.TrimStart(separator));
    }
}

[UsedImplicitly(ImplicitUseTargetFlags.Members)]
public record EmailTaskViewModel
{
    public int Counter { get; init; }
    public required string Status { get; init; }
    public required string ClientName { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? AttemptedAt { get; init; }
    public List<string> Recipients { get; init; } = [];
    public required string From { get; init; }
    public required string Subject { get; init; }
}

[UsedImplicitly(ImplicitUseTargetFlags.Members)]
public record BatchStatusViewModel
{
    public Guid BatchId { get; init; }
    public int Count { get; init; }
    public int Queued { get; init; }
    public int Sent { get; init; }
    public int Failed { get; init; }
    public int Skipped { get; init; }
    public DateTime CreatedAt { get; init; }
}
