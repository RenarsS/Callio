using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace Callio.Provisioning.Infrastructure.Provisioners;

public class SqlServerTenantDatabaseSchemaProvisioner(IConfiguration configuration) : ITenantDatabaseSchemaProvisioner
{
    private readonly string _connectionString = configuration.GetConnectionString("CallioTenantsDb")
        ?? throw new InvalidOperationException("A CallioTenantsDb connection string is required for tenant schema provisioning.");

    public async Task EnsureCreatedAsync(string schemaName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(schemaName))
            throw new ArgumentException("Schema name is required.", nameof(schemaName));

        await EnsureDatabaseExistsAsync(cancellationToken);

        var escapedSchemaName = schemaName.Replace("]", "]]", StringComparison.Ordinal);
        var commandText = $"""
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = @schemaName)
BEGIN
    EXEC(N'CREATE SCHEMA [{escapedSchemaName}] AUTHORIZATION [dbo]');
END
""";

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(commandText, connection);
        command.Parameters.AddWithValue("@schemaName", schemaName);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task EnsureDatabaseExistsAsync(CancellationToken cancellationToken)
    {
        var builder = new SqlConnectionStringBuilder(_connectionString);
        if (string.IsNullOrWhiteSpace(builder.InitialCatalog))
            throw new InvalidOperationException("The tenant database connection string must include a database name.");

        var databaseName = builder.InitialCatalog;
        builder.InitialCatalog = "master";

        var escapedDatabaseName = databaseName.Replace("]", "]]", StringComparison.Ordinal);
        var commandText = $"""
IF DB_ID(@databaseName) IS NULL
BEGIN
    EXEC(N'CREATE DATABASE [{escapedDatabaseName}]');
END
""";

        await using var masterConnection = new SqlConnection(builder.ConnectionString);
        await masterConnection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(commandText, masterConnection);
        command.Parameters.AddWithValue("@databaseName", databaseName);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
