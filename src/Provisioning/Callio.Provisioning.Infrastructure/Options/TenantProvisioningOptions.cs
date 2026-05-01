namespace Callio.Provisioning.Infrastructure.Options;

public sealed class TenantProvisioningOptions
{
    public const string SectionName = "TenantProvisioning";

    public string SchemaPrefix { get; set; } = "tenant_";

    public string VectorNamespacePrefix { get; set; } = "tenant-";

    public string VectorStoreProvider { get; set; } = "Local";

    public string AzureCosmosConnectionString { get; set; } = string.Empty;

    public string AzureCosmosDatabaseName { get; set; } = "callio-vectors";

    public int AzureCosmosVectorDimensions { get; set; } = 1536;

    public string AzureCosmosVectorIndexType { get; set; } = "QuantizedFlat";
}
