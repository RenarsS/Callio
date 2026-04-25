using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Callio.Knowledge.Infrastructure.Persistence;

public class TenantKnowledgeDocumentModelCacheKeyFactory : IModelCacheKeyFactory
{
    public object Create(DbContext context, bool designTime)
    {
        if (context is TenantKnowledgeDocumentDbContext tenantContext)
            return (context.GetType(), tenantContext.SchemaName, designTime);

        return (context.GetType(), designTime);
    }
}
