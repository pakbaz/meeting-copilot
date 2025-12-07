var builder = DistributedApplication.CreateBuilder(args);

// Add Cosmos DB emulator for local development
var cosmosDb = builder.AddAzureCosmosDB("cosmosdb")
    .RunAsEmulator();

// Add the main Blazor web application (using relative path since Projects namespace is not available without IsAspireHost)
var webApp = builder.AddProject("webapp", "../meeting-copilot.csproj")
    .WithReference(cosmosDb)
    .WithExternalHttpEndpoints();

builder.Build().Run();
