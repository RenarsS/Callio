using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace Callio.Provisioning.Infrastructure.Provisioners;

public class SqlServerTenantDatabaseSchemaProvisioner(IConfiguration configuration) : ITenantDatabaseSchemaProvisioner
{
    private readonly string _connectionString = configuration.GetConnectionString("CallioTenantsDb")
        ?? configuration.GetConnectionString("CallioDb")
        ?? throw new InvalidOperationException("A CallioTenantsDb or CallioDb connection string is required for tenant schema provisioning.");

    public async Task EnsureCreatedAsync(string schemaName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(schemaName))
            throw new ArgumentException("Schema name is required.", nameof(schemaName));

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
}
