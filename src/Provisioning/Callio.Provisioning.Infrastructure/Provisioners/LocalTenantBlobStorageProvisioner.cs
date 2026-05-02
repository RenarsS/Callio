using Callio.Provisioning.Infrastructure.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Callio.Provisioning.Infrastructure.Provisioners;

public class LocalTenantBlobStorageProvisioner(
    IOptions<TenantProvisioningOptions> options,
    IHostEnvironment hostEnvironment) : ITenantBlobStorageProvisioner
{
    private readonly TenantProvisioningOptions _options = options.Value;

    public Task EnsureCreatedAsync(string containerName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(containerName))
            throw new ArgumentException("Blob container name is required.", nameof(containerName));

        var rootPath = _options.LocalBlobStorageRootPath;
        if (!Path.IsPathRooted(rootPath))
            rootPath = Path.Combine(hostEnvironment.ContentRootPath, rootPath);

        Directory.CreateDirectory(Path.Combine(rootPath, containerName.Trim()));
        return Task.CompletedTask;
    }
}
