using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Callio.Generation.Infrastructure.Persistence;

public class TenantGenerationModelCacheKeyFactory : IModelCacheKeyFactory
{
    public object Create(DbContext context)
        => Create(context, false);

    public object Create(DbContext context, bool designTime)
    {
        var tenantContext = (TenantGenerationDbContext)context;
        return (context.GetType(), tenantContext.SchemaName, designTime);
    }
}
