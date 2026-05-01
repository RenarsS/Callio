using Callio.Provisioning.Infrastructure.Options;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;

namespace Callio.Provisioning.Infrastructure.Services;

public sealed class TenantVectorStoreCosmosContext(IOptions<TenantProvisioningOptions> options) : IAsyncDisposable
{
    public const string AzureCosmosProvider = "AzureCosmos";
    public const string SectionKeyPath = "/sectionKey";
    public const string VectorPath = "/contentVector";

    private readonly TenantProvisioningOptions _options = options.Value;
    private readonly CosmosClient? _client = CreateClient(options.Value);

    public bool UsesAzureCosmos
        => string.Equals(_options.VectorStoreProvider, AzureCosmosProvider, StringComparison.OrdinalIgnoreCase);

    public int VectorDimensions
        => Math.Max(1, _options.AzureCosmosVectorDimensions);

    public string DatabaseName
    {
        get
        {
            var databaseName = _options.AzureCosmosDatabaseName?.Trim();
            return string.IsNullOrWhiteSpace(databaseName) ? "callio-vectors" : databaseName;
        }
    }

    public CosmosClient GetRequiredClient()
    {
        if (!UsesAzureCosmos)
            throw new InvalidOperationException("Azure Cosmos vector storage is not enabled for this environment.");

        return _client ?? throw new InvalidOperationException("Azure Cosmos DB connection string is required when the AzureCosmos vector store provider is enabled.");
    }

    public async Task<Database> CreateDatabaseIfNotExistsAsync(CancellationToken cancellationToken = default)
    {
        var response = await GetRequiredClient()
            .CreateDatabaseIfNotExistsAsync(DatabaseName, cancellationToken: cancellationToken);

        return response.Database;
    }

    public Container GetRequiredContainer(string containerName)
    {
        if (string.IsNullOrWhiteSpace(containerName))
            throw new ArgumentException("Container name is required.", nameof(containerName));

        return GetRequiredClient()
            .GetContainer(DatabaseName, containerName.Trim());
    }

    public VectorIndexType ResolveVectorIndexType()
        => _options.AzureCosmosVectorIndexType?.Trim().ToLowerInvariant() switch
        {
            "flat" => VectorIndexType.Flat,
            "diskann" => VectorIndexType.DiskANN,
            _ => VectorIndexType.QuantizedFlat
        };

    public static string BuildSectionKey(int? categoryId)
        => categoryId.HasValue && categoryId.Value > 0
            ? $"category:{categoryId.Value}"
            : "general";

    public static string BuildSectionName(string? categoryName)
        => string.IsNullOrWhiteSpace(categoryName)
            ? "General"
            : categoryName.Trim();

    public ValueTask DisposeAsync()
        => _client is IAsyncDisposable asyncDisposable
            ? asyncDisposable.DisposeAsync()
            : ValueTask.CompletedTask;

    private static CosmosClient? CreateClient(TenantProvisioningOptions options)
    {
        var connectionString = options.AzureCosmosConnectionString?.Trim();
        if (string.IsNullOrWhiteSpace(connectionString))
            return null;

        return new CosmosClient(
            connectionString,
            new CosmosClientOptions
            {
                ApplicationName = "Callio",
                AllowBulkExecution = true,
                SerializerOptions = new CosmosSerializationOptions
                {
                    PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
                }
            });
    }
}
