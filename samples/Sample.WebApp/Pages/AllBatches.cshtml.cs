using Microsoft.AspNetCore.Mvc.RazorPages;
using Sample.WebApp.Services;

namespace Sample.WebApp.Pages;

public class AllBatchesModel(EmailQueueApiService apiService, ILogger<AllBatchesModel> logger) : PageModel
{
    public IEnumerable<BatchStatusViewModel> AllBatches { get; private set; } = [];
    public string? ErrorMessage { get; private set; }

    public async Task OnGetAsync()
    {
        try
        {
            AllBatches = await apiService.GetAllBatchesAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching batches");
            ErrorMessage = "Error fetching data. Please try again later.";
        }
    }
}
