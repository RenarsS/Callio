using Callio.Knowledge.Infrastructure.Persistence;
using Callio.Provisioning.Infrastructure.Provisioners;
using Callio.Provisioning.Infrastructure.Services;
using Microsoft.Data.SqlClient;

namespace Callio.Knowledge.Infrastructure.Provisioners;

public class SqlServerTenantKnowledgeConfigurationStoreProvisioner(
    ITenantDatabaseConnectionStringFactory connectionStringFactory,
    ITenantDatabaseSchemaProvisioner tenantDatabaseSchemaProvisioner) : ITenantKnowledgeConfigurationStoreProvisioner
{
    public async Task EnsureCreatedAsync(string schemaName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(schemaName))
            throw new ArgumentException("Schema name is required.", nameof(schemaName));

        await tenantDatabaseSchemaProvisioner.EnsureCreatedAsync(schemaName, cancellationToken);

        var escapedSchemaName = schemaName.Trim().Replace("]", "]]", StringComparison.Ordinal);
        var escapedTableName = TenantKnowledgeConfigurationDbContext.TableName.Replace("]", "]]", StringComparison.Ordinal);
        const string activeIndexName = "IX_KnowledgeConfigurations_Active";

        var commandText = $"""
IF OBJECT_ID(N'[{escapedSchemaName}].[{escapedTableName}]', N'U') IS NULL
BEGIN
    CREATE TABLE [{escapedSchemaName}].[{escapedTableName}]
    (
        [Id] INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_{escapedTableName}] PRIMARY KEY,
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

        await SqlServerTransientRetry.ExecuteAsync(async token =>
        {
            await using var connection = new SqlConnection(connectionStringFactory.CreateTenantConnectionString());
            await connection.OpenAsync(token);

            await using var command = new SqlCommand(commandText, connection);
            command.Parameters.AddWithValue("@activeIndexName", activeIndexName);

            await command.ExecuteNonQueryAsync(token);
        }, cancellationToken);
    }
}
