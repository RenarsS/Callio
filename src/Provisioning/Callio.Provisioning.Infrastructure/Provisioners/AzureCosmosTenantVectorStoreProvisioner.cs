using Callio.Provisioning.Infrastructure.Services;
using Microsoft.Azure.Cosmos;
using System.Collections.ObjectModel;

namespace Callio.Provisioning.Infrastructure.Provisioners;

public class AzureCosmosTenantVectorStoreProvisioner(
    TenantVectorStoreCosmosContext cosmosContext) : ITenantVectorStoreProvisioner
{
    public async Task EnsureCreatedAsync(int tenantId, string namespaceName, CancellationToken cancellationToken = default)
    {
        if (tenantId <= 0)
            throw new ArgumentOutOfRangeException(nameof(tenantId));

        if (string.IsNullOrWhiteSpace(namespaceName))
            throw new ArgumentException("Namespace name is required.", nameof(namespaceName));

        var database = await cosmosContext.CreateDatabaseIfNotExistsAsync(cancellationToken);
        var containerName = namespaceName.Trim();

        var properties = new ContainerProperties(containerName, TenantVectorStoreCosmosContext.SectionKeyPath)
        {
            VectorEmbeddingPolicy = new VectorEmbeddingPolicy(
                new Collection<Embedding>
                {
                    new()
                    {
                        Path = TenantVectorStoreCosmosContext.VectorPath,
                        DataType = VectorDataType.Float32,
                        DistanceFunction = DistanceFunction.Cosine,
                        Dimensions = cosmosContext.VectorDimensions
                    }
                }),
            IndexingPolicy = new IndexingPolicy()
        };

        properties.IndexingPolicy.VectorIndexes.Add(new VectorIndexPath
        {
            Path = TenantVectorStoreCosmosContext.VectorPath,
            Type = cosmosContext.ResolveVectorIndexType()
        });

        await database.CreateContainerIfNotExistsAsync(
            properties,
            cancellationToken: cancellationToken);
    }
}
