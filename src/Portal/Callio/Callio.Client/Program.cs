using Callio.Client.Services;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.Services.AddMudServices();

var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "http://localhost:5043";

builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri(apiBaseUrl)
});

builder.Services.AddScoped<PortalSessionService>();
builder.Services.AddScoped<Callio.Client.Services.TenantRequestApi>();
builder.Services.AddScoped<Callio.Client.Services.TenantKnowledgeSettingsApi>();
builder.Services.AddScoped<Callio.Client.Services.TenantKnowledgeDocumentsApi>();
builder.Services.AddScoped<Callio.Client.Services.TenantProvisioningApi>();
builder.Services.AddScoped<Callio.Client.Services.TenantGenerationApi>();

await builder.Build().RunAsync();
