using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace Callio.Provisioning.Infrastructure.Services;

public class TenantDatabaseConnectionStringFactory(IConfiguration configuration) : ITenantDatabaseConnectionStringFactory
{
    private readonly string _baseConnectionString = configuration.GetConnectionString("CallioTenantsDb")
        ?? throw new InvalidOperationException("A CallioTenantsDb connection string is required for tenant schema storage.");

    public string CreateTenantConnectionString()
    {
        var builder = new SqlConnectionStringBuilder(_baseConnectionString);
        builder.ConnectTimeout = Math.Max(60, builder.ConnectTimeout);
        builder.ConnectRetryCount = 3;
        builder.ConnectRetryInterval = 10;
        builder.MultipleActiveResultSets = true;

        return builder.ConnectionString;
    }
}
