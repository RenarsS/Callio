using Azure.Storage.Blobs;
using Callio.Provisioning.Infrastructure.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Callio.Provisioning.Infrastructure.Provisioners;

public class AzureBlobTenantBlobStorageProvisioner(
    IOptions<TenantProvisioningOptions> options,
    IConfiguration configuration) : ITenantBlobStorageProvisioner
{
    private const string KnowledgeIngestionAzureBlobConnectionStringKey = "TenantKnowledgeIngestion:AzureBlobConnectionString";

    private readonly TenantProvisioningOptions _options = options.Value;

    public async Task EnsureCreatedAsync(string containerName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(containerName))
            throw new ArgumentException("Blob container name is required.", nameof(containerName));

        var connectionString = ResolveConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException(
                "Azure Blob Storage connection string is required when Azure blob storage is enabled.");

        var containerClient = new BlobContainerClient(connectionString, containerName.Trim());
        await containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
    }

    private string ResolveConnectionString()
    {
        var ingestionConnectionString = configuration[KnowledgeIngestionAzureBlobConnectionStringKey];

        return string.IsNullOrWhiteSpace(ingestionConnectionString)
            ? _options.AzureBlobConnectionString
            : ingestionConnectionString;
    }
}
