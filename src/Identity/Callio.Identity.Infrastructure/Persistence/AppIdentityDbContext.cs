using Callio.Identity.Domain;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Callio.Identity.Infrastructure.Persistence;

public class AppIdentityDbContext(DbContextOptions<AppIdentityDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public new DbSet<ApplicationUser> Users => Set<ApplicationUser>();
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("identity");
        base.OnModelCreating(modelBuilder);
    }
}
