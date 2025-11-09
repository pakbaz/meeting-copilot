using meeting_copilot.Components;
using meeting_copilot.Services;
using Azure.Identity;
using Azure.Extensions.AspNetCore.Configuration.Secrets;

var builder = WebApplication.CreateBuilder(args);

// Configure secure configuration sources following Azure best practices
if (builder.Environment.IsProduction())
{
    // In production: Use Azure Key Vault for secrets management
    var keyVaultName = builder.Configuration["KeyVaultName"];
    if (!string.IsNullOrEmpty(keyVaultName))
    {
        var keyVaultUri = new Uri($"https://{keyVaultName}.vault.azure.net/");
        builder.Configuration.AddAzureKeyVault(keyVaultUri, new DefaultAzureCredential());
    }
}
// In development: User Secrets are automatically loaded by ASP.NET Core

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add Azure Speech Recognition Service
builder.Services.AddScoped<SpeechRecognitionService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// Add security headers for microphone access
app.Use(async (context, next) =>
{
    // Security headers for microphone access
    context.Response.Headers.Append("Permissions-Policy", "microphone=*");
    context.Response.Headers.Append("Feature-Policy", "microphone *");
    
    // Additional security headers
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
    
    await next();
});

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Add diagnostic endpoint for debugging authentication
app.MapGet("/api/diagnostics", (SpeechRecognitionService speechService) =>
{
    return Results.Ok(new 
    { 
        message = "Check console for authentication diagnostics",
        timestamp = DateTime.UtcNow
    });
});

// Add microphone diagnostics endpoint
app.MapGet("/api/microphone-check", () =>
{
    var diagnostics = new
    {
        serverInfo = new
        {
            httpsConfigured = true,
            timestamp = DateTime.UtcNow,
            environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown"
        },
        requirements = new
        {
            httpsRequired = "Microphone access requires HTTPS connection",
            browserSupport = "Chrome 47+, Firefox 36+, Safari 11+, Edge 12+",
            permissionsRequired = "User must grant microphone access when prompted"
        },
        troubleshooting = new
        {
            checkBrowserConsole = "Look for permission denied errors in browser console",
            checkUrl = "Ensure you're accessing via https:// (not http://)",
            checkMicrophone = "Verify microphone is working in other applications",
            checkBrowserSettings = "Check browser microphone permissions for this site"
        }
    };
    
    return Results.Ok(diagnostics);
});

// Add test endpoint to trigger speech recognition and capture errors
app.MapPost("/api/test-auth", async (SpeechRecognitionService speechService) =>
{
    var errors = new List<string>();
    var messages = new List<string>();
    
    // Capture error and info messages
    speechService.OnError += (sender, error) => 
    {
        if (error.Contains("❌") || error.Contains("💥"))
            errors.Add(error);
        else 
            messages.Add(error);
    };
    
    try
    {
        var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromSeconds(10)); // Cancel after 10 seconds
        
        // This will trigger the authentication process
        await speechService.RecognizeFromMicrophoneAsync(cts.Token);
        
        return Results.Ok(new { 
            success = true, 
            errors = errors,
            messages = messages,
            timestamp = DateTime.UtcNow 
        });
    }
    catch (Exception ex)
    {
        errors.Add($"💥 Exception: {ex.Message}");
        return Results.Ok(new { 
            success = false, 
            errors = errors,
            messages = messages,
            exception = ex.Message,
            timestamp = DateTime.UtcNow 
        });
    }
});

app.Run();
