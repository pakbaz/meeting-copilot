using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;

namespace MeetingCopilot.Memory;

public abstract class CosmosDbService
{
    protected readonly Container Container;
    protected readonly ILogger Logger;

    protected CosmosDbService(
        CosmosClient cosmosClient,
        string databaseName,
        string containerName,
        ILogger logger)
    {
        Container = cosmosClient.GetContainer(databaseName, containerName);
        Logger = logger;
    }

    protected async Task<T?> GetItemAsync<T>(string id, string partitionKey, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await Container.ReadItemAsync<T>(
                id,
                new PartitionKey(partitionKey),
                cancellationToken: cancellationToken);

            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return default;
        }
    }

    protected async Task<T> UpsertItemAsync<T>(T item, string partitionKey, CancellationToken cancellationToken = default)
    {
        var response = await Container.UpsertItemAsync(
            item,
            new PartitionKey(partitionKey),
            cancellationToken: cancellationToken);

        return response.Resource;
    }

    protected async Task DeleteItemAsync(string id, string partitionKey, CancellationToken cancellationToken = default)
    {
        await Container.DeleteItemAsync<object>(
            id,
            new PartitionKey(partitionKey),
            cancellationToken: cancellationToken);
    }

    protected async Task<List<T>> QueryItemsAsync<T>(
        string query,
        Dictionary<string, object>? parameters = null,
        CancellationToken cancellationToken = default)
    {
        var queryDefinition = new QueryDefinition(query);
        if (parameters != null)
        {
            foreach (var param in parameters)
            {
                queryDefinition.WithParameter($"@{param.Key}", param.Value);
            }
        }

        var results = new List<T>();
        using var iterator = Container.GetItemQueryIterator<T>(queryDefinition);

        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync(cancellationToken);
            results.AddRange(response);
        }

        return results;
    }

    protected async Task<List<T>> QueryItemsAsync<T>(
        QueryDefinition queryDefinition,
        CancellationToken cancellationToken = default)
    {
        var results = new List<T>();
        using var iterator = Container.GetItemQueryIterator<T>(queryDefinition);

        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync(cancellationToken);
            results.AddRange(response);
        }

        return results;
    }
}
