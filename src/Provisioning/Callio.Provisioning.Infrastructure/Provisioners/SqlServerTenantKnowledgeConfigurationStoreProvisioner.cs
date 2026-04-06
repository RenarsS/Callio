using Callio.Provisioning.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace Callio.Provisioning.Infrastructure.Provisioners;

public class SqlServerTenantKnowledgeConfigurationStoreProvisioner(
    IConfiguration configuration,
    ITenantDatabaseSchemaProvisioner tenantDatabaseSchemaProvisioner) : ITenantKnowledgeConfigurationStoreProvisioner
{
    private readonly string _connectionString = configuration.GetConnectionString("CallioTenantsDb")
        ?? throw new InvalidOperationException("A CallioTenantsDb connection string is required for tenant knowledge configuration storage.");

    public async Task EnsureCreatedAsync(string schemaName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(schemaName))
            throw new ArgumentException("Schema name is required.", nameof(schemaName));

        await tenantDatabaseSchemaProvisioner.EnsureCreatedAsync(schemaName, cancellationToken);

        var escapedSchemaName = schemaName.Replace("]", "]]", StringComparison.Ordinal);
        var escapedTableName = TenantKnowledgeConfigurationDbContext.TableName.Replace("]", "]]", StringComparison.Ordinal);
        var activeIndexName = $"IX_{escapedSchemaName}_{escapedTableName}_Active";

        var commandText = $"""
IF OBJECT_ID(N'[{escapedSchemaName}].[{escapedTableName}]', N'U') IS NULL
BEGIN
    CREATE TABLE [{escapedSchemaName}].[{escapedTableName}]
    (
        [Id] INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_{escapedSchemaName}_{escapedTableName}] PRIMARY KEY,
        [TenantId] INT NOT NULL,
        [SystemPrompt] NVARCHAR(MAX) NOT NULL,
        [AssistantInstructionPrompt] NVARCHAR(MAX) NOT NULL,
        [ChunkSize] INT NOT NULL,
        [ChunkOverlap] INT NOT NULL,
        [TopKRetrievalCount] INT NOT NULL,
        [MaximumChunksInFinalContext] INT NOT NULL,
        [MinimumSimilarityThreshold] DECIMAL(5,4) NOT NULL,
        [AllowedFileTypes] NVARCHAR(1000) NOT NULL,
        [MaximumFileSizeBytes] BIGINT NOT NULL,
        [AutoProcessOnUpload] BIT NOT NULL,
        [ManualApprovalRequiredBeforeIndexing] BIT NOT NULL,
        [VersioningEnabled] BIT NOT NULL,
        [IsActive] BIT NOT NULL,
        [CreatedAtUtc] DATETIME2 NOT NULL,
        [UpdatedAtUtc] DATETIME2 NOT NULL
    );
END

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = @activeIndexName
      AND object_id = OBJECT_ID(N'[{escapedSchemaName}].[{escapedTableName}]', N'U')
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [{activeIndexName}] ON [{escapedSchemaName}].[{escapedTableName}] ([IsActive]) WHERE [IsActive] = 1');
END
""";

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(commandText, connection);
        command.Parameters.AddWithValue("@activeIndexName", activeIndexName);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
