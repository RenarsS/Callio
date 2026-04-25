using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace Callio.Knowledge.Infrastructure.Provisioners;

public class SqlServerKnowledgeMetadataStoreProvisioner(IConfiguration configuration) : IKnowledgeMetadataStoreProvisioner
{
    private readonly string _connectionString = configuration.GetConnectionString("CallioDb")
        ?? throw new InvalidOperationException("A CallioDb connection string is required for knowledge metadata storage.");

    public async Task EnsureCreatedAsync(CancellationToken cancellationToken = default)
    {
        var commandText = """
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = N'provisioning')
BEGIN
    EXEC(N'CREATE SCHEMA [provisioning] AUTHORIZATION [dbo]');
END

IF OBJECT_ID(N'[provisioning].[TenantKnowledgeConfigurationSetups]', N'U') IS NULL
BEGIN
    CREATE TABLE [provisioning].[TenantKnowledgeConfigurationSetups]
    (
        [Id] INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_TenantKnowledgeConfigurationSetups] PRIMARY KEY,
        [TenantId] INT NOT NULL,
        [Status] NVARCHAR(32) NOT NULL,
        [AttemptCount] INT NOT NULL,
        [ActiveConfigurationId] INT NULL,
        [LastError] NVARCHAR(4000) NULL,
        [CreatedAtUtc] DATETIME2 NOT NULL,
        [UpdatedAtUtc] DATETIME2 NOT NULL,
        [LastStartedAtUtc] DATETIME2 NULL,
        [LastCompletedAtUtc] DATETIME2 NULL
    );
END

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_TenantKnowledgeConfigurationSetups_TenantId'
      AND object_id = OBJECT_ID(N'[provisioning].[TenantKnowledgeConfigurationSetups]', N'U')
)
BEGIN
    CREATE UNIQUE INDEX [IX_TenantKnowledgeConfigurationSetups_TenantId]
        ON [provisioning].[TenantKnowledgeConfigurationSetups] ([TenantId]);
END
""";

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(commandText, connection);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
