namespace Callio.Knowledge.Application.KnowledgeDocuments;

public record TenantKnowledgeDashboardOverviewDto(
    int TenantCount,
    int TenantsWithDocuments,
    int TotalDocuments,
    long TotalStorageBytes,
    int ReadyDocuments,
    int FailedDocuments,
    int AwaitingApprovalDocuments);
