using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;

namespace MeetingCopilot.Memory;

public class CosmosContainerInitializer
{
    private readonly CosmosClient _cosmosClient;
    private readonly string _databaseName;
    private readonly ILogger<CosmosContainerInitializer> _logger;
    private readonly bool _useVectorIndexing;

    public CosmosContainerInitializer(
        CosmosClient cosmosClient,
        string databaseName,
        ILogger<CosmosContainerInitializer> logger,
        bool useVectorIndexing = false)
    {
        _cosmosClient = cosmosClient;
        _databaseName = databaseName;
        _logger = logger;
        _useVectorIndexing = useVectorIndexing;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Initializing Cosmos DB containers (VectorIndexing: {UseVectorIndexing})", _useVectorIndexing);

        // Create database if not exists
        var database = await _cosmosClient.CreateDatabaseIfNotExistsAsync(_databaseName, cancellationToken: cancellationToken);
        
        // Create meetings container
        await CreateMeetingsContainerAsync(database.Database, cancellationToken);
        
        // Create speakers container
        await CreateSpeakersContainerAsync(database.Database, cancellationToken);
        
        // Create interactions container with vector indexing
        await CreateInteractionsContainerAsync(database.Database, cancellationToken);
        
        // Create insights container with vector indexing
        await CreateInsightsContainerAsync(database.Database, cancellationToken);
        
        // Create session_summaries container
        await CreateSessionSummariesContainerAsync(database.Database, cancellationToken);

        _logger.LogInformation("Cosmos DB containers initialized successfully");
    }

    private async Task CreateMeetingsContainerAsync(Database database, CancellationToken cancellationToken)
    {
        var containerProperties = new ContainerProperties
        {
            Id = "meetings",
            PartitionKeyPath = "/meetingId"
        };

        await database.CreateContainerIfNotExistsAsync(containerProperties, 400, cancellationToken: cancellationToken);
        _logger.LogInformation("Container 'meetings' ready");
    }

    private async Task CreateSpeakersContainerAsync(Database database, CancellationToken cancellationToken)
    {
        var containerProperties = new ContainerProperties
        {
            Id = "speakers",
            PartitionKeyPath = "/meetingId"
        };

        await database.CreateContainerIfNotExistsAsync(containerProperties, 400, cancellationToken: cancellationToken);
        _logger.LogInformation("Container 'speakers' ready");
    }

    private async Task CreateInteractionsContainerAsync(Database database, CancellationToken cancellationToken)
    {
        var containerProperties = new ContainerProperties
        {
            Id = "interactions",
            PartitionKeyPath = "/userId"
        };

        // Only add vector indexing if supported (not supported in vNext preview emulator)
        if (_useVectorIndexing)
        {
            containerProperties.VectorEmbeddingPolicy = new VectorEmbeddingPolicy(
                new System.Collections.ObjectModel.Collection<Embedding>
                {
                    new()
                    {
                        Path = "/contentVector",
                        DataType = VectorDataType.Float32,
                        DistanceFunction = DistanceFunction.Cosine,
                        Dimensions = 1536
                    }
                });

            // Configure indexing policy for vector search
            containerProperties.IndexingPolicy.VectorIndexes.Add(new VectorIndexPath
            {
                Path = "/contentVector",
                Type = VectorIndexType.QuantizedFlat
            });
            
            _logger.LogInformation("Container 'interactions' configured with vector indexing");
        }

        await database.CreateContainerIfNotExistsAsync(containerProperties, 400, cancellationToken: cancellationToken);
        _logger.LogInformation("Container 'interactions' ready");
    }

    private async Task CreateInsightsContainerAsync(Database database, CancellationToken cancellationToken)
    {
        var containerProperties = new ContainerProperties
        {
            Id = "insights",
            PartitionKeyPath = "/userId"
        };

        // Only add vector indexing if supported (not supported in vNext preview emulator)
        if (_useVectorIndexing)
        {
            containerProperties.VectorEmbeddingPolicy = new VectorEmbeddingPolicy(
                new System.Collections.ObjectModel.Collection<Embedding>
                {
                    new()
                    {
                        Path = "/embedding",
                        DataType = VectorDataType.Float32,
                        DistanceFunction = DistanceFunction.Cosine,
                        Dimensions = 1536
                    }
                });

            // Configure indexing policy for vector search
            containerProperties.IndexingPolicy.VectorIndexes.Add(new VectorIndexPath
            {
                Path = "/embedding",
                Type = VectorIndexType.QuantizedFlat
            });
            
            _logger.LogInformation("Container 'insights' configured with vector indexing");
        }

        await database.CreateContainerIfNotExistsAsync(containerProperties, 400, cancellationToken: cancellationToken);
        _logger.LogInformation("Container 'insights' ready");
    }

    private async Task CreateSessionSummariesContainerAsync(Database database, CancellationToken cancellationToken)
    {
        var containerProperties = new ContainerProperties
        {
            Id = "session_summaries",
            PartitionKeyPath = "/userId"
        };

        await database.CreateContainerIfNotExistsAsync(containerProperties, 400, cancellationToken: cancellationToken);
        _logger.LogInformation("Container 'session_summaries' ready");
    }
}
