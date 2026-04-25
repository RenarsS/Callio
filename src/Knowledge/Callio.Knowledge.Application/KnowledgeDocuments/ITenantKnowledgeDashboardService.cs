namespace Callio.Knowledge.Application.KnowledgeDocuments;

public interface ITenantKnowledgeDashboardService
{
    Task<TenantKnowledgeDashboardOverviewDto> GetOverviewAsync(CancellationToken cancellationToken = default);
}
