namespace Callio.Knowledge.Infrastructure.Provisioners;

public interface ITenantKnowledgeConfigurationStoreProvisioner
{
    Task EnsureCreatedAsync(string schemaName, CancellationToken cancellationToken = default);
}
