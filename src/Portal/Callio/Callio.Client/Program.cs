using Callio.Client.Services;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.Services.AddMudServices();

builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri("http://localhost:5043")
});

builder.Services.AddScoped<Callio.Client.Services.TenantRequestApi>();
builder.Services.AddScoped<Callio.Client.Services.TenantKnowledgeSettingsApi>();
builder.Services.AddScoped<Callio.Client.Services.TenantKnowledgeDocumentsApi>();

await builder.Build().RunAsync();
