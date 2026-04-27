using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace Callio.Provisioning.Infrastructure.Services;

public class TenantDatabaseConnectionStringFactory(IConfiguration configuration) : ITenantDatabaseConnectionStringFactory
{
    private readonly string _baseConnectionString = configuration.GetConnectionString("CallioTenantsDb")
        ?? throw new InvalidOperationException("A CallioTenantsDb connection string is required for tenant schema storage.");

    public string CreateTenantConnectionString()
        => new SqlConnectionStringBuilder(_baseConnectionString).ConnectionString;
}
