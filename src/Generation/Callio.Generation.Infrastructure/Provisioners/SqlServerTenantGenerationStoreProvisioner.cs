using Callio.Generation.Infrastructure.Persistence;
using Callio.Provisioning.Infrastructure.Provisioners;
using Callio.Provisioning.Infrastructure.Services;
using Microsoft.Data.SqlClient;

namespace Callio.Generation.Infrastructure.Provisioners;

public class SqlServerTenantGenerationStoreProvisioner(
    ITenantDatabaseConnectionStringFactory connectionStringFactory,
    ITenantDatabaseSchemaProvisioner tenantDatabaseSchemaProvisioner) : ITenantGenerationStoreProvisioner
{
    public async Task EnsureCreatedAsync(string schemaName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(schemaName))
            throw new ArgumentException("Schema name is required.", nameof(schemaName));

        await tenantDatabaseSchemaProvisioner.EnsureCreatedAsync(schemaName, cancellationToken);

        var escapedSchemaName = schemaName.Trim().Replace("]", "]]", StringComparison.Ordinal);
        var commandText = $"""
IF OBJECT_ID(N'[{escapedSchemaName}].[{TenantGenerationDbContext.PromptTemplatesTableName}]', N'U') IS NULL
BEGIN
    CREATE TABLE [{escapedSchemaName}].[{TenantGenerationDbContext.PromptTemplatesTableName}]
    (
        [Id] INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_{TenantGenerationDbContext.PromptTemplatesTableName}] PRIMARY KEY,
        [TenantId] INT NOT NULL,
        [PromptKey] NVARCHAR(120) NOT NULL,
        [PromptName] NVARCHAR(200) NOT NULL,
        [Description] NVARCHAR(1000) NULL,
        [SystemPrompt] NVARCHAR(MAX) NOT NULL,
        [UserPromptTemplate] NVARCHAR(MAX) NOT NULL,
        [DataSourcesJson] NVARCHAR(MAX) NOT NULL,
        [CreatedAtUtc] DATETIME2 NOT NULL,
        [UpdatedAtUtc] DATETIME2 NOT NULL
    );
END

IF OBJECT_ID(N'[{escapedSchemaName}].[{TenantGenerationDbContext.ResponsesTableName}]', N'U') IS NULL
BEGIN
    CREATE TABLE [{escapedSchemaName}].[{TenantGenerationDbContext.ResponsesTableName}]
    (
        [Id] INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_{TenantGenerationDbContext.ResponsesTableName}] PRIMARY KEY,
        [ResponseKey] UNIQUEIDENTIFIER NOT NULL,
        [TenantId] INT NOT NULL,
        [PromptKey] NVARCHAR(120) NOT NULL,
        [PromptName] NVARCHAR(200) NOT NULL,
        [Input] NVARCHAR(MAX) NOT NULL,
        [SystemPrompt] NVARCHAR(MAX) NOT NULL,
        [UserPrompt] NVARCHAR(MAX) NOT NULL,
        [FinalPrompt] NVARCHAR(MAX) NOT NULL,
        [ResponseText] NVARCHAR(MAX) NOT NULL,
        [GenerationModel] NVARCHAR(256) NOT NULL,
        [Status] NVARCHAR(32) NOT NULL,
        [ErrorMessage] NVARCHAR(4000) NULL,
        [RequestedByUserId] NVARCHAR(128) NULL,
        [RequestedByDisplayName] NVARCHAR(200) NULL,
        [SourceCount] INT NOT NULL,
        [EstimatedInputTokens] INT NOT NULL,
        [EstimatedOutputTokens] INT NOT NULL,
        [CreatedAtUtc] DATETIME2 NOT NULL,
        [CompletedAtUtc] DATETIME2 NULL
    );
END

IF OBJECT_ID(N'[{escapedSchemaName}].[{TenantGenerationDbContext.ResponseSourcesTableName}]', N'U') IS NULL
BEGIN
    CREATE TABLE [{escapedSchemaName}].[{TenantGenerationDbContext.ResponseSourcesTableName}]
    (
        [Id] INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_{TenantGenerationDbContext.ResponseSourcesTableName}] PRIMARY KEY,
        [TenantGenerationResponseId] INT NOT NULL,
        [SourceKind] NVARCHAR(32) NOT NULL,
        [KnowledgeDocumentId] INT NULL,
        [DocumentTitle] NVARCHAR(256) NULL,
        [CategoryId] INT NULL,
        [CategoryName] NVARCHAR(120) NULL,
        [ChunkId] INT NULL,
        [ChunkIndex] INT NULL,
        [Score] DECIMAL(18,6) NULL,
        [BlobContainerName] NVARCHAR(128) NULL,
        [BlobName] NVARCHAR(512) NULL,
        [BlobUri] NVARCHAR(2000) NULL,
        [ContentExcerpt] NVARCHAR(MAX) NOT NULL,
        [CreatedAtUtc] DATETIME2 NOT NULL,
        CONSTRAINT [FK_{TenantGenerationDbContext.ResponseSourcesTableName}_{TenantGenerationDbContext.ResponsesTableName}]
            FOREIGN KEY ([TenantGenerationResponseId]) REFERENCES [{escapedSchemaName}].[{TenantGenerationDbContext.ResponsesTableName}]([Id]) ON DELETE CASCADE
    );
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_{TenantGenerationDbContext.PromptTemplatesTableName}_PromptKey' AND object_id = OBJECT_ID(N'[{escapedSchemaName}].[{TenantGenerationDbContext.PromptTemplatesTableName}]', N'U'))
    CREATE UNIQUE INDEX [IX_{TenantGenerationDbContext.PromptTemplatesTableName}_PromptKey] ON [{escapedSchemaName}].[{TenantGenerationDbContext.PromptTemplatesTableName}]([PromptKey]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_{TenantGenerationDbContext.PromptTemplatesTableName}_UpdatedAtUtc' AND object_id = OBJECT_ID(N'[{escapedSchemaName}].[{TenantGenerationDbContext.PromptTemplatesTableName}]', N'U'))
    CREATE INDEX [IX_{TenantGenerationDbContext.PromptTemplatesTableName}_UpdatedAtUtc] ON [{escapedSchemaName}].[{TenantGenerationDbContext.PromptTemplatesTableName}]([UpdatedAtUtc]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_{TenantGenerationDbContext.ResponsesTableName}_ResponseKey' AND object_id = OBJECT_ID(N'[{escapedSchemaName}].[{TenantGenerationDbContext.ResponsesTableName}]', N'U'))
    CREATE UNIQUE INDEX [IX_{TenantGenerationDbContext.ResponsesTableName}_ResponseKey] ON [{escapedSchemaName}].[{TenantGenerationDbContext.ResponsesTableName}]([ResponseKey]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_{TenantGenerationDbContext.ResponsesTableName}_TenantId' AND object_id = OBJECT_ID(N'[{escapedSchemaName}].[{TenantGenerationDbContext.ResponsesTableName}]', N'U'))
    CREATE INDEX [IX_{TenantGenerationDbContext.ResponsesTableName}_TenantId] ON [{escapedSchemaName}].[{TenantGenerationDbContext.ResponsesTableName}]([TenantId]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_{TenantGenerationDbContext.ResponsesTableName}_CreatedAtUtc' AND object_id = OBJECT_ID(N'[{escapedSchemaName}].[{TenantGenerationDbContext.ResponsesTableName}]', N'U'))
    CREATE INDEX [IX_{TenantGenerationDbContext.ResponsesTableName}_CreatedAtUtc] ON [{escapedSchemaName}].[{TenantGenerationDbContext.ResponsesTableName}]([CreatedAtUtc]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_{TenantGenerationDbContext.ResponseSourcesTableName}_KnowledgeDocumentId' AND object_id = OBJECT_ID(N'[{escapedSchemaName}].[{TenantGenerationDbContext.ResponseSourcesTableName}]', N'U'))
    CREATE INDEX [IX_{TenantGenerationDbContext.ResponseSourcesTableName}_KnowledgeDocumentId] ON [{escapedSchemaName}].[{TenantGenerationDbContext.ResponseSourcesTableName}]([KnowledgeDocumentId]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_{TenantGenerationDbContext.ResponseSourcesTableName}_CategoryId' AND object_id = OBJECT_ID(N'[{escapedSchemaName}].[{TenantGenerationDbContext.ResponseSourcesTableName}]', N'U'))
    CREATE INDEX [IX_{TenantGenerationDbContext.ResponseSourcesTableName}_CategoryId] ON [{escapedSchemaName}].[{TenantGenerationDbContext.ResponseSourcesTableName}]([CategoryId]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_{TenantGenerationDbContext.ResponseSourcesTableName}_ChunkId' AND object_id = OBJECT_ID(N'[{escapedSchemaName}].[{TenantGenerationDbContext.ResponseSourcesTableName}]', N'U'))
    CREATE INDEX [IX_{TenantGenerationDbContext.ResponseSourcesTableName}_ChunkId] ON [{escapedSchemaName}].[{TenantGenerationDbContext.ResponseSourcesTableName}]([ChunkId]);
""";

        await SqlServerTransientRetry.ExecuteAsync(async token =>
        {
            await using var connection = new SqlConnection(connectionStringFactory.CreateTenantConnectionString());
            await connection.OpenAsync(token);

            await using var command = new SqlCommand(commandText, connection);
            await command.ExecuteNonQueryAsync(token);
        }, cancellationToken);
    }
}
