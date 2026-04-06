namespace Callio.Provisioning.Domain;

public static class TenantProvisioningSteps
{
    public const string DatabaseSchema = "database-schema";
    public const string VectorStore = "vector-store";
    public const string DefaultConfiguration = "default-configuration";

    public static IReadOnlyList<string> Ordered { get; } =
    [
        DatabaseSchema,
        VectorStore,
        DefaultConfiguration
    ];
}
