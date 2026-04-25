namespace Callio.Provisioning.Infrastructure.Options;

public sealed class TenantProvisioningOptions
{
    public const string SectionName = "TenantProvisioning";

    public string SchemaPrefix { get; set; } = "tenant_";

    public string VectorNamespacePrefix { get; set; } = "tenant-";
}
