using Callio.Provisioning.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace Callio.Provisioning.Infrastructure.Provisioners;

public class TenantBlobStorageProvisioner(
    IOptions<TenantProvisioningOptions> options,
    LocalTenantBlobStorageProvisioner localProvisioner,
    AzureBlobTenantBlobStorageProvisioner azureBlobProvisioner) : ITenantBlobStorageProvisioner
{
    private readonly TenantProvisioningOptions _options = options.Value;

    public Task EnsureCreatedAsync(string containerName, CancellationToken cancellationToken = default)
        => string.Equals(_options.ResolveBlobStorageProvider(), TenantProvisioningOptions.AzureBlobProvider, StringComparison.OrdinalIgnoreCase)
            ? azureBlobProvisioner.EnsureCreatedAsync(containerName, cancellationToken)
            : localProvisioner.EnsureCreatedAsync(containerName, cancellationToken);
}
