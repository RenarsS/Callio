using Callio.Knowledge.Infrastructure.Persistence;
using Callio.Provisioning.Infrastructure.Provisioners;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace Callio.Knowledge.Infrastructure.Provisioners;

public class SqlServerTenantKnowledgeDocumentStoreProvisioner(
    IConfiguration configuration,
    ITenantDatabaseSchemaProvisioner tenantDatabaseSchemaProvisioner) : ITenantKnowledgeDocumentStoreProvisioner
{
    private readonly string _connectionString = configuration.GetConnectionString("CallioTenantsDb")
        ?? throw new InvalidOperationException("A CallioTenantsDb connection string is required for tenant knowledge document storage.");

    public async Task EnsureCreatedAsync(string schemaName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(schemaName))
            throw new ArgumentException("Schema name is required.", nameof(schemaName));

        await tenantDatabaseSchemaProvisioner.EnsureCreatedAsync(schemaName, cancellationToken);

        var escapedSchemaName = schemaName.Replace("]", "]]", StringComparison.Ordinal);

        var commandText = $"""
IF OBJECT_ID(N'[{escapedSchemaName}].[{TenantKnowledgeDocumentDbContext.CategoriesTableName}]', N'U') IS NULL
BEGIN
    CREATE TABLE [{escapedSchemaName}].[{TenantKnowledgeDocumentDbContext.CategoriesTableName}]
    (
        [Id] INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_{escapedSchemaName}_{TenantKnowledgeDocumentDbContext.CategoriesTableName}] PRIMARY KEY,
        [TenantId] INT NOT NULL,
        [Name] NVARCHAR(120) NOT NULL,
        [NormalizedName] NVARCHAR(120) NOT NULL,
        [Description] NVARCHAR(500) NULL,
        [CreatedAtUtc] DATETIME2 NOT NULL,
        [UpdatedAtUtc] DATETIME2 NOT NULL
    );
END

IF OBJECT_ID(N'[{escapedSchemaName}].[{TenantKnowledgeDocumentDbContext.TagsTableName}]', N'U') IS NULL
BEGIN
    CREATE TABLE [{escapedSchemaName}].[{TenantKnowledgeDocumentDbContext.TagsTableName}]
    (
        [Id] INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_{escapedSchemaName}_{TenantKnowledgeDocumentDbContext.TagsTableName}] PRIMARY KEY,
        [TenantId] INT NOT NULL,
        [Name] NVARCHAR(80) NOT NULL,
        [NormalizedName] NVARCHAR(80) NOT NULL,
        [CreatedAtUtc] DATETIME2 NOT NULL,
        [UpdatedAtUtc] DATETIME2 NOT NULL
    );
END

IF OBJECT_ID(N'[{escapedSchemaName}].[{TenantKnowledgeDocumentDbContext.DocumentsTableName}]', N'U') IS NULL
BEGIN
    CREATE TABLE [{escapedSchemaName}].[{TenantKnowledgeDocumentDbContext.DocumentsTableName}]
    (
        [Id] INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_{escapedSchemaName}_{TenantKnowledgeDocumentDbContext.DocumentsTableName}] PRIMARY KEY,
        [DocumentKey] UNIQUEIDENTIFIER NOT NULL,
        [TenantId] INT NOT NULL,
        [KnowledgeConfigurationId] INT NOT NULL,
        [CategoryId] INT NULL,
        [Title] NVARCHAR(256) NOT NULL,
        [OriginalFileName] NVARCHAR(260) NOT NULL,
        [ContentType] NVARCHAR(256) NOT NULL,
        [FileExtension] NVARCHAR(32) NOT NULL,
        [SizeBytes] BIGINT NOT NULL,
        [BlobContainerName] NVARCHAR(128) NOT NULL,
        [BlobName] NVARCHAR(512) NOT NULL,
        [BlobUri] NVARCHAR(2000) NOT NULL,
        [ContentHash] NVARCHAR(64) NOT NULL,
        [VectorStoreNamespace] NVARCHAR(256) NOT NULL,
        [SourceType] NVARCHAR(32) NOT NULL,
        [ProcessingStatus] NVARCHAR(32) NOT NULL,
        [UploadedByUserId] NVARCHAR(128) NULL,
        [UploadedByDisplayName] NVARCHAR(200) NULL,
        [ChunkCount] INT NOT NULL,
        [LastError] NVARCHAR(4000) NULL,
        [CreatedAtUtc] DATETIME2 NOT NULL,
        [UpdatedAtUtc] DATETIME2 NOT NULL,
        [IndexedAtUtc] DATETIME2 NULL,
        CONSTRAINT [FK_{escapedSchemaName}_{TenantKnowledgeDocumentDbContext.DocumentsTableName}_{TenantKnowledgeDocumentDbContext.CategoriesTableName}]
            FOREIGN KEY ([CategoryId]) REFERENCES [{escapedSchemaName}].[{TenantKnowledgeDocumentDbContext.CategoriesTableName}]([Id])
    );
END

IF OBJECT_ID(N'[{escapedSchemaName}].[{TenantKnowledgeDocumentDbContext.DocumentTagsTableName}]', N'U') IS NULL
BEGIN
    CREATE TABLE [{escapedSchemaName}].[{TenantKnowledgeDocumentDbContext.DocumentTagsTableName}]
    (
        [Id] INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_{escapedSchemaName}_{TenantKnowledgeDocumentDbContext.DocumentTagsTableName}] PRIMARY KEY,
        [TenantKnowledgeDocumentId] INT NOT NULL,
        [TenantKnowledgeTagId] INT NOT NULL,
        [CreatedAtUtc] DATETIME2 NOT NULL,
        CONSTRAINT [FK_{escapedSchemaName}_{TenantKnowledgeDocumentDbContext.DocumentTagsTableName}_{TenantKnowledgeDocumentDbContext.DocumentsTableName}]
            FOREIGN KEY ([TenantKnowledgeDocumentId]) REFERENCES [{escapedSchemaName}].[{TenantKnowledgeDocumentDbContext.DocumentsTableName}]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_{escapedSchemaName}_{TenantKnowledgeDocumentDbContext.DocumentTagsTableName}_{TenantKnowledgeDocumentDbContext.TagsTableName}]
            FOREIGN KEY ([TenantKnowledgeTagId]) REFERENCES [{escapedSchemaName}].[{TenantKnowledgeDocumentDbContext.TagsTableName}]([Id]) ON DELETE CASCADE
    );
END

IF OBJECT_ID(N'[{escapedSchemaName}].[{TenantKnowledgeDocumentDbContext.DocumentChunksTableName}]', N'U') IS NULL
BEGIN
    CREATE TABLE [{escapedSchemaName}].[{TenantKnowledgeDocumentDbContext.DocumentChunksTableName}]
    (
        [Id] INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_{escapedSchemaName}_{TenantKnowledgeDocumentDbContext.DocumentChunksTableName}] PRIMARY KEY,
        [TenantKnowledgeDocumentId] INT NOT NULL,
        [ChunkIndex] INT NOT NULL,
        [VectorRecordId] NVARCHAR(256) NOT NULL,
        [VectorStoreNamespace] NVARCHAR(256) NOT NULL,
        [EmbeddingModel] NVARCHAR(256) NOT NULL,
        [EmbeddingDimensions] INT NOT NULL,
        [Content] NVARCHAR(MAX) NOT NULL,
        [CharacterCount] INT NOT NULL,
        [TokenCountEstimate] INT NOT NULL,
        [EmbeddingJson] NVARCHAR(MAX) NOT NULL,
        [CreatedAtUtc] DATETIME2 NOT NULL,
        CONSTRAINT [FK_{escapedSchemaName}_{TenantKnowledgeDocumentDbContext.DocumentChunksTableName}_{TenantKnowledgeDocumentDbContext.DocumentsTableName}]
            FOREIGN KEY ([TenantKnowledgeDocumentId]) REFERENCES [{escapedSchemaName}].[{TenantKnowledgeDocumentDbContext.DocumentsTableName}]([Id]) ON DELETE CASCADE
    );
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_{escapedSchemaName}_{TenantKnowledgeDocumentDbContext.CategoriesTableName}_NormalizedName' AND object_id = OBJECT_ID(N'[{escapedSchemaName}].[{TenantKnowledgeDocumentDbContext.CategoriesTableName}]', N'U'))
    CREATE UNIQUE INDEX [IX_{escapedSchemaName}_{TenantKnowledgeDocumentDbContext.CategoriesTableName}_NormalizedName] ON [{escapedSchemaName}].[{TenantKnowledgeDocumentDbContext.CategoriesTableName}]([NormalizedName]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_{escapedSchemaName}_{TenantKnowledgeDocumentDbContext.TagsTableName}_NormalizedName' AND object_id = OBJECT_ID(N'[{escapedSchemaName}].[{TenantKnowledgeDocumentDbContext.TagsTableName}]', N'U'))
    CREATE UNIQUE INDEX [IX_{escapedSchemaName}_{TenantKnowledgeDocumentDbContext.TagsTableName}_NormalizedName] ON [{escapedSchemaName}].[{TenantKnowledgeDocumentDbContext.TagsTableName}]([NormalizedName]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_{escapedSchemaName}_{TenantKnowledgeDocumentDbContext.DocumentsTableName}_DocumentKey' AND object_id = OBJECT_ID(N'[{escapedSchemaName}].[{TenantKnowledgeDocumentDbContext.DocumentsTableName}]', N'U'))
    CREATE UNIQUE INDEX [IX_{escapedSchemaName}_{TenantKnowledgeDocumentDbContext.DocumentsTableName}_DocumentKey] ON [{escapedSchemaName}].[{TenantKnowledgeDocumentDbContext.DocumentsTableName}]([DocumentKey]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_{escapedSchemaName}_{TenantKnowledgeDocumentDbContext.DocumentsTableName}_ProcessingStatus' AND object_id = OBJECT_ID(N'[{escapedSchemaName}].[{TenantKnowledgeDocumentDbContext.DocumentsTableName}]', N'U'))
    CREATE INDEX [IX_{escapedSchemaName}_{TenantKnowledgeDocumentDbContext.DocumentsTableName}_ProcessingStatus] ON [{escapedSchemaName}].[{TenantKnowledgeDocumentDbContext.DocumentsTableName}]([ProcessingStatus]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_{escapedSchemaName}_{TenantKnowledgeDocumentDbContext.DocumentsTableName}_CategoryId' AND object_id = OBJECT_ID(N'[{escapedSchemaName}].[{TenantKnowledgeDocumentDbContext.DocumentsTableName}]', N'U'))
    CREATE INDEX [IX_{escapedSchemaName}_{TenantKnowledgeDocumentDbContext.DocumentsTableName}_CategoryId] ON [{escapedSchemaName}].[{TenantKnowledgeDocumentDbContext.DocumentsTableName}]([CategoryId]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_{escapedSchemaName}_{TenantKnowledgeDocumentDbContext.DocumentTagsTableName}_DocumentTag' AND object_id = OBJECT_ID(N'[{escapedSchemaName}].[{TenantKnowledgeDocumentDbContext.DocumentTagsTableName}]', N'U'))
    CREATE UNIQUE INDEX [IX_{escapedSchemaName}_{TenantKnowledgeDocumentDbContext.DocumentTagsTableName}_DocumentTag] ON [{escapedSchemaName}].[{TenantKnowledgeDocumentDbContext.DocumentTagsTableName}]([TenantKnowledgeDocumentId], [TenantKnowledgeTagId]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_{escapedSchemaName}_{TenantKnowledgeDocumentDbContext.DocumentChunksTableName}_DocumentChunk' AND object_id = OBJECT_ID(N'[{escapedSchemaName}].[{TenantKnowledgeDocumentDbContext.DocumentChunksTableName}]', N'U'))
    CREATE UNIQUE INDEX [IX_{escapedSchemaName}_{TenantKnowledgeDocumentDbContext.DocumentChunksTableName}_DocumentChunk] ON [{escapedSchemaName}].[{TenantKnowledgeDocumentDbContext.DocumentChunksTableName}]([TenantKnowledgeDocumentId], [ChunkIndex]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_{escapedSchemaName}_{TenantKnowledgeDocumentDbContext.DocumentChunksTableName}_VectorRecordId' AND object_id = OBJECT_ID(N'[{escapedSchemaName}].[{TenantKnowledgeDocumentDbContext.DocumentChunksTableName}]', N'U'))
    CREATE UNIQUE INDEX [IX_{escapedSchemaName}_{TenantKnowledgeDocumentDbContext.DocumentChunksTableName}_VectorRecordId] ON [{escapedSchemaName}].[{TenantKnowledgeDocumentDbContext.DocumentChunksTableName}]([VectorRecordId]);
""";

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(commandText, connection);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
