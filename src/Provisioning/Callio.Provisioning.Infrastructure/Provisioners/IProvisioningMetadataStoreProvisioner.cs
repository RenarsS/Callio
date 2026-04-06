namespace Callio.Provisioning.Infrastructure.Provisioners;

public interface IProvisioningMetadataStoreProvisioner
{
    Task EnsureCreatedAsync(CancellationToken cancellationToken = default);
}
