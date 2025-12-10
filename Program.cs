using Azure.AI.OpenAI;
using Azure.Identity;
using Azure.Extensions.AspNetCore.Configuration.Secrets;
using meeting_copilot.Agents;
using meeting_copilot.Components;
using meeting_copilot.Data;
using meeting_copilot.Data.Repositories;
using meeting_copilot.Hubs;
using meeting_copilot.Services;
using MeetingCopilot.Agents;
using MeetingCopilot.Contracts.Interfaces;
using MeetingCopilot.Memory;
using MeetingCopilot.Memory.Repositories;
using Microsoft.Agents.AI.Hosting.AGUI.AspNetCore;
using Microsoft.Azure.Cosmos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System.Net.Http;

var builder = WebApplication.CreateBuilder(args);

// Add Aspire ServiceDefaults for OpenTelemetry and health checks
builder.AddServiceDefaults();

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

// Add SignalR
builder.Services.AddSignalR();

builder.Services.AddHttpClient();
builder.Services.AddAGUI();
builder.Services.AddSingleton<AgentCatalog>();

// Cosmos DB Configuration
// When running with Aspire, the connection string is named "cosmosdb" (the resource name from AppHost)
// When running standalone, it uses "CosmosDb" from appsettings.json
var cosmosConnectionString = builder.Configuration.GetConnectionString("cosmosdb") 
    ?? builder.Configuration.GetConnectionString("CosmosDb");
var cosmosDatabaseName = builder.Configuration["CosmosDb:DatabaseName"] ?? "meetingcopilot";
var cosmosAccountEndpoint = builder.Configuration["CosmosDb:AccountEndpoint"];
var cosmosEndpointUri = ResolveCosmosEndpoint(cosmosConnectionString, cosmosAccountEndpoint);
var isCosmosEmulator = IsCosmosEmulatorEndpoint(cosmosEndpointUri);

if (!string.IsNullOrEmpty(cosmosConnectionString))
{
    builder.Services.AddSingleton<CosmosClient>(sp =>
    {
        var options = CreateCosmosClientOptions(isCosmosEmulator);
        return new CosmosClient(cosmosConnectionString, options);
    });
}
else
{
    // Fallback to account endpoint and key for development
    var accountEndpoint = cosmosAccountEndpoint;
    var accountKey = builder.Configuration["CosmosDb:AccountKey"];
    
    if (!string.IsNullOrEmpty(accountEndpoint) && !string.IsNullOrEmpty(accountKey))
    {
        builder.Services.AddSingleton<CosmosClient>(sp =>
        {
            var options = CreateCosmosClientOptions(isCosmosEmulator);
            return new CosmosClient(accountEndpoint, accountKey, options);
        });
    }
}

// OpenAI Client Configuration
var openAIEndpoint = builder.Configuration["AzureAI:OpenAIEndpoint"];
var embeddingModel = builder.Configuration["AzureAI:EmbeddingModel"] ?? "text-embedding-3-large";

if (!string.IsNullOrEmpty(openAIEndpoint))
{
    builder.Services.AddSingleton<AzureOpenAIClient>(sp =>
    {
        return new AzureOpenAIClient(new Uri(openAIEndpoint), new DefaultAzureCredential());
    });
    
    builder.Services.AddSingleton<OpenAI.Embeddings.EmbeddingClient>(sp =>
    {
        var azureClient = sp.GetRequiredService<AzureOpenAIClient>();
        return azureClient.GetEmbeddingClient(embeddingModel);
    });
    
    // Meeting Context Extractor (LLM-based title/agenda extraction)
    builder.Services.AddScoped<MeetingContextExtractor>();
}

// Cosmos DB Services
// Disable vector indexing for emulator (vNext preview doesn't support it)
var useVectorIndexing = !isCosmosEmulator && builder.Configuration.GetValue<bool>("CosmosDb:UseVectorIndexing", true);
builder.Services.AddSingleton<CosmosContainerInitializer>(sp =>
{
    var cosmosClient = sp.GetRequiredService<CosmosClient>();
    var logger = sp.GetRequiredService<ILogger<CosmosContainerInitializer>>();
    return new CosmosContainerInitializer(cosmosClient, cosmosDatabaseName, logger, useVectorIndexing);
});

// Embedding and Memory Services
builder.Services.AddSingleton<IEmbeddingService>(sp =>
{
    var embeddingClient = sp.GetRequiredService<OpenAI.Embeddings.EmbeddingClient>();
    var logger = sp.GetRequiredService<ILogger<EmbeddingService>>();
    return new EmbeddingService(embeddingClient, logger);
});

builder.Services.AddScoped<IMemoryProvider>(sp =>
{
    var cosmosClient = sp.GetRequiredService<CosmosClient>();
    var embeddingService = sp.GetRequiredService<IEmbeddingService>();
    var logger = sp.GetRequiredService<ILogger<CosmosMemoryProvider>>();
    return new CosmosMemoryProvider(cosmosClient, cosmosDatabaseName, embeddingService, logger);
});

builder.Services.AddScoped<VectorSearchService>(sp =>
{
    var cosmosClient = sp.GetRequiredService<CosmosClient>();
    var embeddingService = sp.GetRequiredService<IEmbeddingService>();
    var logger = sp.GetRequiredService<ILogger<VectorSearchService>>();
    return new VectorSearchService(cosmosClient, cosmosDatabaseName, embeddingService, logger);
});

// Repositories
builder.Services.AddScoped<IMeetingRepository>(sp =>
{
    var cosmosClient = sp.GetRequiredService<CosmosClient>();
    var logger = sp.GetRequiredService<ILogger<CosmosMeetingRepository>>();
    return new CosmosMeetingRepository(cosmosClient, cosmosDatabaseName, logger);
});

builder.Services.AddScoped<ISpeakerRepository>(sp =>
{
    var cosmosClient = sp.GetRequiredService<CosmosClient>();
    var logger = sp.GetRequiredService<ILogger<CosmosSpeakerRepository>>();
    return new CosmosSpeakerRepository(cosmosClient, cosmosDatabaseName, logger);
});

builder.Services.AddScoped<IInteractionRepository>(sp =>
{
    var cosmosClient = sp.GetRequiredService<CosmosClient>();
    var logger = sp.GetRequiredService<ILogger<CosmosInteractionRepository>>();
    return new CosmosInteractionRepository(cosmosClient, cosmosDatabaseName, logger);
});

builder.Services.AddScoped<IInsightRepository>(sp =>
{
    var cosmosClient = sp.GetRequiredService<CosmosClient>();
    var logger = sp.GetRequiredService<ILogger<CosmosInsightRepository>>();
    return new CosmosInsightRepository(cosmosClient, cosmosDatabaseName, logger);
});

// Agent Infrastructure
builder.Services.AddSingleton<AgentPriorityQueue>();
builder.Services.AddHostedService<MeetingCopilot.Agents.AgentOrchestrator>();

// Legacy services (will be migrated)
builder.Services.AddDbContext<MeetingCopilotDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("MeetingCopilot") ?? "Data Source=meetingcopilot.db"));

builder.Services.AddScoped<KeypointRepository>();
builder.Services.AddScoped<GuestInfoRepository>();
builder.Services.AddScoped<meeting_copilot.Agents.AgentOrchestrator>();

// Add Azure Speech Recognition Service
builder.Services.AddScoped<SpeechRecognitionService>();

// Add Meeting Services
builder.Services.AddScoped<MeetingService>();
builder.Services.AddScoped<MicrophoneService>();

// Add API Controllers
builder.Services.AddControllers();

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

app.UseStatusCodePagesWithReExecute("/not-found");
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseAntiforgery();

// Map Aspire health check endpoints
app.MapDefaultEndpoints();

// Map API Controllers
app.MapControllers();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Map SignalR hub
app.MapHub<MeetingHub>("/hubs/meeting");

// Initialize Cosmos DB containers
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<MeetingCopilotDbContext>();
    await dbContext.Database.EnsureCreatedAsync();
    
    // Initialize Cosmos DB if configured
    if (scope.ServiceProvider.GetService<CosmosClient>() != null)
    {
        var containerInitializer = scope.ServiceProvider.GetRequiredService<CosmosContainerInitializer>();
        try
        {
            await containerInitializer.InitializeAsync();
        }
        catch (Exception ex)
        {
            var startupLogger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
            startupLogger.LogWarning(ex,
                "Skipping Cosmos DB initialization. Ensure the emulator or configured account is reachable. {Message}",
                ex.Message);
        }
    }
}

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

var agentCatalog = app.Services.GetRequiredService<AgentCatalog>();
app.MapAGUI("/agents/keypoints", agentCatalog.KeypointAgent);
app.MapAGUI("/agents/speakers", agentCatalog.SpeakerAgent);

app.Run();

CosmosClientOptions CreateCosmosClientOptions(bool isEmulator)
{
    var options = new CosmosClientOptions
    {
        SerializerOptions = new CosmosSerializationOptions
        {
            PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase,
            IgnoreNullValues = true // Prevent serializing null TTL values which Cosmos DB rejects
        },
        ConnectionMode = ConnectionMode.Gateway
    };

    if (isEmulator)
    {
        options.LimitToEndpoint = true;
        options.HttpClientFactory = () =>
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            };
            return new HttpClient(handler);
        };
    }

    return options;
}

static Uri? ResolveCosmosEndpoint(string? connectionString, string? accountEndpoint)
{
    if (!string.IsNullOrWhiteSpace(accountEndpoint) && Uri.TryCreate(accountEndpoint, UriKind.Absolute, out var explicitUri))
    {
        return explicitUri;
    }

    if (!string.IsNullOrEmpty(connectionString))
    {
        var endpointEntry = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault(part => part.StartsWith("AccountEndpoint=", StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrEmpty(endpointEntry))
        {
            var endpointValue = endpointEntry.Substring("AccountEndpoint=".Length).Trim();
            if (Uri.TryCreate(endpointValue, UriKind.Absolute, out var uriFromConnectionString))
            {
                return uriFromConnectionString;
            }
        }
    }

    return null;
}

static bool IsCosmosEmulatorEndpoint(Uri? endpoint)
    => endpoint?.IsLoopback ?? false;
