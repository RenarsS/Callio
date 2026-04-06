using Callio.Provisioning.Domain;
using Callio.Provisioning.Infrastructure.Options;
using Callio.Provisioning.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Callio.Provisioning.Infrastructure.Provisioners;

public class TenantKnowledgeBaseSettingsProvisioner(
    ProvisioningDbContext provisioningDbContext,
    IOptions<TenantProvisioningOptions> options) : ITenantKnowledgeBaseSettingsProvisioner
{
    private readonly TenantProvisioningOptions _options = options.Value;

    public async Task EnsureCreatedAsync(
        int tenantId,
        string databaseSchema,
        string vectorStoreNamespace,
        CancellationToken cancellationToken = default)
    {
        var settings = await provisioningDbContext.TenantKnowledgeBaseSettings
            .FirstOrDefaultAsync(x => x.TenantId == tenantId, cancellationToken);

        if (settings is null)
        {
            provisioningDbContext.TenantKnowledgeBaseSettings.Add(
                new TenantKnowledgeBaseSettings(
                    tenantId,
                    databaseSchema,
                    vectorStoreNamespace,
                    _options.EmbeddingProvider,
                    _options.EmbeddingModel,
                    _options.ChunkSize,
                    _options.ChunkOverlap,
                    _options.RetrievalTopK,
                    _options.EnableKnowledgeIngestion,
                    _options.EnableKnowledgeRetrieval,
                    DateTime.UtcNow));

            return;
        }

        settings.UpdateDefaults(
            databaseSchema,
            vectorStoreNamespace,
            _options.EmbeddingProvider,
            _options.EmbeddingModel,
            _options.ChunkSize,
            _options.ChunkOverlap,
            _options.RetrievalTopK,
            _options.EnableKnowledgeIngestion,
            _options.EnableKnowledgeRetrieval,
            DateTime.UtcNow);
    }
}
