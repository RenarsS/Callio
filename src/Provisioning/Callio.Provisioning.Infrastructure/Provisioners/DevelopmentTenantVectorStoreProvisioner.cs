namespace Callio.Provisioning.Infrastructure.Provisioners;

public class DevelopmentTenantVectorStoreProvisioner : ITenantVectorStoreProvisioner
{
    public Task EnsureCreatedAsync(int tenantId, string namespaceName, CancellationToken cancellationToken = default)
    {
        if (tenantId <= 0)
            throw new ArgumentOutOfRangeException(nameof(tenantId));

        if (string.IsNullOrWhiteSpace(namespaceName))
            throw new ArgumentException("Namespace name is required.", nameof(namespaceName));

        return Task.CompletedTask;
    }
}
