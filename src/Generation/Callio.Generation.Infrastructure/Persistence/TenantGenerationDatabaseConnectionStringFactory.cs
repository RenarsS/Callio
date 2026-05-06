using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace Callio.Generation.Infrastructure.Persistence;

public class TenantGenerationDatabaseConnectionStringFactory(IConfiguration configuration)
    : ITenantGenerationDatabaseConnectionStringFactory
{
    private readonly string _baseConnectionString = configuration.GetConnectionString("CallioTenantsDb")
        ?? throw new InvalidOperationException("A CallioTenantsDb connection string is required for tenant generation storage.");

    public string CreateTenantConnectionString()
        => new SqlConnectionStringBuilder(_baseConnectionString).ConnectionString;
}
