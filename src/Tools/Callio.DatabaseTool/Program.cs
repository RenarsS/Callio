using Callio.Admin.Infrastructure.Persistence;
using Callio.DatabaseTool;
using Callio.Identity.Infrastructure.Persistence;
using Callio.Provisioning.Application.KnowledgeConfigurations;
using Callio.Provisioning.Infrastructure.Options;
using Callio.Provisioning.Infrastructure.Persistence;
using Callio.Provisioning.Infrastructure.Provisioners;
using Callio.Provisioning.Infrastructure.Repositories;
using Callio.Provisioning.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder();

builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(options =>
{
    options.SingleLine = true;
    options.TimestampFormat = "HH:mm:ss ";
});

var callioDbConnectionString = builder.Configuration.GetConnectionString("CallioDb")
    ?? throw new InvalidOperationException("Connection string 'CallioDb' is required.");

builder.Services.Configure<TenantProvisioningOptions>(
    builder.Configuration.GetSection(TenantProvisioningOptions.SectionName));

builder.Services.AddDbContext<AdminDbContext>(options => options.UseSqlServer(callioDbConnectionString));
builder.Services.AddDbContext<AppIdentityDbContext>(options => options.UseSqlServer(callioDbConnectionString));
builder.Services.AddDbContext<ProvisioningDbContext>(options => options.UseSqlServer(callioDbConnectionString));

builder.Services.AddScoped<IProvisioningMetadataStoreProvisioner, SqlServerProvisioningMetadataStoreProvisioner>();
builder.Services.AddScoped<ITenantDatabaseSchemaProvisioner, SqlServerTenantDatabaseSchemaProvisioner>();
builder.Services.AddScoped<ITenantKnowledgeConfigurationStoreProvisioner, SqlServerTenantKnowledgeConfigurationStoreProvisioner>();
builder.Services.AddSingleton<ITenantResourceNamingStrategy, DefaultTenantResourceNamingStrategy>();
builder.Services.AddSingleton<ITenantKnowledgeConfigurationDbContextFactory, TenantKnowledgeConfigurationDbContextFactory>();
builder.Services.AddScoped<ITenantKnowledgeConfigurationRepository, TenantKnowledgeConfigurationRepository>();
builder.Services.AddScoped<ITenantKnowledgeConfigurationService, TenantKnowledgeConfigurationService>();
builder.Services.AddScoped<ITenantKnowledgeConfigurationSetupService, TenantKnowledgeConfigurationSetupService>();

builder.Services.AddScoped<TenantSchemaMigrationRunner>();
builder.Services.AddScoped<TestTenantRequestSeeder>();
builder.Services.AddScoped<DatabaseCommandRunner>();

if (!DatabaseToolCommandParser.TryParse(args, out var command))
{
    Console.Error.WriteLine(DatabaseToolCommandParser.GetUsage());
    return 1;
}

using var cancellationSource = new CancellationTokenSource();
Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    cancellationSource.Cancel();
};

using var host = builder.Build();
await using var scope = host.Services.CreateAsyncScope();

var runner = scope.ServiceProvider.GetRequiredService<DatabaseCommandRunner>();
return await runner.RunAsync(command, cancellationSource.Token);
