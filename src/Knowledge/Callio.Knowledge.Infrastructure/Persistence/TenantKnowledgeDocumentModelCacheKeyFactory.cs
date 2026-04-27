using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Callio.Knowledge.Infrastructure.Persistence;

public class TenantKnowledgeDocumentModelCacheKeyFactory : IModelCacheKeyFactory
{
    public object Create(DbContext context)
        => Create(context, false);

    public object Create(DbContext context, bool designTime)
    {
        var tenantContext = (TenantKnowledgeDocumentDbContext)context;
        return (context.GetType(), tenantContext.SchemaName, designTime);
    }
}
