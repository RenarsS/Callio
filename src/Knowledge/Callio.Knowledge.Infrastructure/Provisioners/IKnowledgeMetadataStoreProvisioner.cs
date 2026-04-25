namespace Callio.Knowledge.Infrastructure.Provisioners;

public interface IKnowledgeMetadataStoreProvisioner
{
    Task EnsureCreatedAsync(CancellationToken cancellationToken = default);
}
