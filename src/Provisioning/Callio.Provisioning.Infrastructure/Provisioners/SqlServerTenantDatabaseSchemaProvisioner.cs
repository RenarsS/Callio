using Callio.Provisioning.Infrastructure.Services;
using Microsoft.Data.SqlClient;

namespace Callio.Provisioning.Infrastructure.Provisioners;

public class SqlServerTenantDatabaseSchemaProvisioner(
    ITenantDatabaseConnectionStringFactory connectionStringFactory) : ITenantDatabaseSchemaProvisioner
{
    public async Task EnsureCreatedAsync(string schemaName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(schemaName))
            throw new ArgumentException("Schema name is required.", nameof(schemaName));

        await EnsureSchemaExistsAsync(schemaName.Trim(), cancellationToken);
    }

    private async Task EnsureSchemaExistsAsync(string schemaName, CancellationToken cancellationToken)
    {
        var escapedSchemaName = schemaName.Replace("]", "]]", StringComparison.Ordinal);
        var commandText = $"""
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = @schemaName)
BEGIN
    EXEC(N'CREATE SCHEMA [{escapedSchemaName}] AUTHORIZATION [dbo]');
END
""";

        await SqlServerTransientRetry.ExecuteAsync(async token =>
        {
            await using var connection = new SqlConnection(connectionStringFactory.CreateTenantConnectionString());
            await connection.OpenAsync(token);

            await using var command = new SqlCommand(commandText, connection);
            command.Parameters.AddWithValue("@schemaName", schemaName);

            await command.ExecuteNonQueryAsync(token);
        }, cancellationToken);
    }
}
