using EmailQueue.WebApp.Services;
using EmailQueue.WebApp.Settings;

var builder = WebApplication.CreateBuilder(args);

// Populate application settings.
builder.BindSettings();

// Persist data protection keys.
builder.Services.AddDataProtection();

// Configure the EmailQueue API.
builder.Services.AddHttpClient<EmailQueueApiService>();
builder.Services.AddScoped<EmailQueueApiService>();

// Configure UI services.
builder.Services.AddRazorPages();

var app = builder.Build();

// Configure error handling.
if (app.Environment.IsDevelopment()) app.UseDeveloperExceptionPage();
else app.UseExceptionHandler("/Error");

// Configure the application pipeline.
app.UseHsts();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.MapStaticAssets();
app.MapRazorPages().WithStaticAssets();

await app.RunAsync();
