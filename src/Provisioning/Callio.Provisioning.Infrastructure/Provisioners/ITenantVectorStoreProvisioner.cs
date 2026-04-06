namespace Callio.Provisioning.Infrastructure.Provisioners;

public interface ITenantVectorStoreProvisioner
{
    Task EnsureCreatedAsync(int tenantId, string namespaceName, CancellationToken cancellationToken = default);
}
