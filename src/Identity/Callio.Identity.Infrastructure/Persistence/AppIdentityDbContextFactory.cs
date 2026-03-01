using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Callio.Identity.Infrastructure.Persistence;

public class AppIdentityDbContextFactory: IDesignTimeDbContextFactory<AppIdentityDbContext>
{
    public AppIdentityDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppIdentityDbContext>();
        optionsBuilder.UseSqlServer("Server=Renars\\SQLEXPRESS;Database=Callio;Trusted_Connection=True;TrustServerCertificate=True;");

        return new AppIdentityDbContext(optionsBuilder.Options);
    }
}