using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Callio.Generation.Infrastructure.Persistence;

public class TenantGenerationModelCacheKeyFactory : IModelCacheKeyFactory
{
    public object Create(DbContext context, bool designTime)
    {
        if (context is TenantGenerationDbContext tenantContext)
            return (context.GetType(), tenantContext.SchemaName, designTime);

        return (context.GetType(), designTime);
    }
}
