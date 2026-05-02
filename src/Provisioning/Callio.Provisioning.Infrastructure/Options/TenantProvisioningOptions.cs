namespace Callio.Provisioning.Infrastructure.Options;

public sealed class TenantProvisioningOptions
{
    public const string SectionName = "TenantProvisioning";
    public const string LocalProvider = "Local";
    public const string AzureProvider = "Azure";
    public const string AzureCosmosProvider = "AzureCosmos";
    public const string AzureBlobProvider = "AzureBlob";

    public string ResourceProvider { get; set; } = LocalProvider;

    public string SchemaPrefix { get; set; } = "tenant_";

    public string VectorNamespacePrefix { get; set; } = "tenant-";

    public string BlobContainerPrefix { get; set; } = "tenant-knowledge-";

    public string VectorStoreProvider { get; set; } = string.Empty;

    public string BlobStorageProvider { get; set; } = string.Empty;

    public string LocalBlobStorageRootPath { get; set; } = "App_Data\\tenant-knowledge";

    public string AzureCosmosConnectionString { get; set; } = string.Empty;

    public string AzureCosmosDatabaseName { get; set; } = "callio-vectors";

    public int AzureCosmosVectorDimensions { get; set; } = 1536;

    public string AzureCosmosVectorIndexType { get; set; } = "QuantizedFlat";

    public string AzureBlobConnectionString { get; set; } = string.Empty;

    public string ResolveVectorStoreProvider()
        => ResolveProvider(VectorStoreProvider, AzureCosmosProvider);

    public string ResolveBlobStorageProvider()
        => ResolveProvider(BlobStorageProvider, AzureBlobProvider);

    private string ResolveProvider(string provider, string azureProvider)
    {
        if (!string.IsNullOrWhiteSpace(provider))
            return provider.Trim();

        return string.Equals(ResourceProvider, AzureProvider, StringComparison.OrdinalIgnoreCase)
            ? azureProvider
            : LocalProvider;
    }
}
