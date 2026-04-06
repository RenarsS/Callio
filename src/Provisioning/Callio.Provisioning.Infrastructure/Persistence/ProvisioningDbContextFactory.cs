using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Callio.Provisioning.Infrastructure.Persistence;

public class ProvisioningDbContextFactory : IDesignTimeDbContextFactory<ProvisioningDbContext>
{
    public ProvisioningDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ProvisioningDbContext>();
        optionsBuilder.UseSqlServer("Server=Renars\\SQLEXPRESS;Database=Callio;Trusted_Connection=True;TrustServerCertificate=True;");

        return new ProvisioningDbContext(optionsBuilder.Options);
    }
}
