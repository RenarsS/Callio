using Azure.Storage.Blobs;
using Callio.Provisioning.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace Callio.Provisioning.Infrastructure.Provisioners;

public class AzureBlobTenantBlobStorageProvisioner(IOptions<TenantProvisioningOptions> options) : ITenantBlobStorageProvisioner
{
    private readonly TenantProvisioningOptions _options = options.Value;

    public async Task EnsureCreatedAsync(string containerName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(containerName))
            throw new ArgumentException("Blob container name is required.", nameof(containerName));

        if (string.IsNullOrWhiteSpace(_options.AzureBlobConnectionString))
            throw new InvalidOperationException("Azure Blob Storage connection string is required when Azure blob storage is enabled.");

        var containerClient = new BlobContainerClient(_options.AzureBlobConnectionString, containerName.Trim());
        await containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
    }
}
