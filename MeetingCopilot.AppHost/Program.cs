var builder = DistributedApplication.CreateBuilder(args);

// Use Azure Cosmos DB connection string from configuration/user secrets
// The connection string should be set in user secrets as "ConnectionStrings:cosmosdb"
var cosmosDb = builder.AddConnectionString("cosmosdb");

// Add the main Blazor web application (using relative path since Projects namespace is not available without IsAspireHost)
var webApp = builder.AddProject("webapp", "../meeting-copilot.csproj")
    .WithReference(cosmosDb)
    .WithExternalHttpEndpoints();

builder.Build().Run();
