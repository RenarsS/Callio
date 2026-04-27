using Callio.Knowledge.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

public class KnowledgeDbContextFactory : IDesignTimeDbContextFactory<KnowledgeDbContext>
{
    public KnowledgeDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<KnowledgeDbContext>();
        // Replace with your actual provider and connection string
        optionsBuilder.UseSqlServer("Server=Renars\\SQLEXPRESS;Database=Callio;Trusted_Connection=True;TrustServerCertificate=True;"); 

        return new KnowledgeDbContext(optionsBuilder.Options);
    }
}