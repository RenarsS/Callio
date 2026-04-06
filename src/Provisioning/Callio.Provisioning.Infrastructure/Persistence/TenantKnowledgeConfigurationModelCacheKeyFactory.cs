using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Callio.Provisioning.Infrastructure.Persistence;

public class TenantKnowledgeConfigurationModelCacheKeyFactory : IModelCacheKeyFactory
{
    public object Create(DbContext context, bool designTime)
    {
        if (context is TenantKnowledgeConfigurationDbContext tenantContext)
            return (context.GetType(), tenantContext.SchemaName, designTime);

        return (context.GetType(), designTime);
    }
}
