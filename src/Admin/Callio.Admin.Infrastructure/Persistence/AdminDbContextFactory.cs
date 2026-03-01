using System.ComponentModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Callio.Admin.Infrastructure.Persistence;

public class AdminDbContextFactory : IDesignTimeDbContextFactory<AdminDbContext>
{
    public AdminDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AdminDbContext>();
        optionsBuilder.UseSqlServer("Server=Renars\\SQLEXPRESS;Database=Callio;Trusted_Connection=True;TrustServerCertificate=True;");

        return new AdminDbContext(optionsBuilder.Options);
    }
}