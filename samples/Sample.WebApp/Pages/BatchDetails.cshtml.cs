using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Sample.WebApp.Services;
using System.ComponentModel.DataAnnotations;

namespace Sample.WebApp.Pages;

public class BatchDetailsModel(EmailQueueApiService apiService, ILogger<BatchDetailsModel> logger) : PageModel
{
    [BindProperty]
    [Display(Name = "Batch ID")]
    public Guid? BatchId { get; set; }

    public IEnumerable<EmailTaskViewModel> EmailTasks { get; private set; } = [];
    public BatchStatusViewModel? BatchStatus { get; private set; }
    public string? ErrorMessage { get; private set; }
    public bool ShowResults { get; private set; }

    [TempData]
    public string? NotificationMessage { get; set; }

    public void OnGet()
    {
        // Method intentionally left empty.
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!BatchId.HasValue)
        {
            NotificationMessage = "Please enter a valid Batch ID to continue.";
            return RedirectToPage();
        }

        try
        {
            EmailTasks = await apiService.GetBatchDetailsAsync(BatchId.Value);
            BatchStatus = await apiService.GetBatchStatusAsync(BatchId.Value);
            ShowResults = true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching emails for batch {BatchId}", BatchId);
            ErrorMessage = "Error fetching emails. Please try again later.";
        }

        return Page();
    }
}
