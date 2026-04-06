namespace Callio.Provisioning.Infrastructure.Services;

public interface ITenantResourceNamingStrategy
{
    TenantProvisioningResourceNames Create(int tenantId);
}
