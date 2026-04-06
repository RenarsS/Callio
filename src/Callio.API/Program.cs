using Carter;
using Callio.Admin.Infrastructure;
using Callio.Admin.Infrastructure.Persistence;
using Callio.Identity.Infrastructure;
using Callio.Identity.Infrastructure.Consumers;
using Callio.Provisioning.Infrastructure;
using Callio.Provisioning.Infrastructure.Consumers;
using Callio.Provisioning.Infrastructure.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddCarter();
builder.Services.AddMassTransit(x =>
{
    x.SetKebabCaseEndpointNameFormatter();
    x.AddConsumer<TenantApprovedConsumer>();
    x.AddConsumer<TenantApprovedProvisioningConsumer>();
    x.AddConsumer<TenantInfrastructureProvisioningSucceededConsumer>();

    var transport = builder.Configuration["MassTransit:Transport"];
    if (string.Equals(transport, "RabbitMq", StringComparison.OrdinalIgnoreCase))
    {
        x.UsingRabbitMq((context, cfg) =>
        {
            var host = builder.Configuration["MassTransit:RabbitMq:Host"] ?? "localhost";
            var virtualHost = builder.Configuration["MassTransit:RabbitMq:VirtualHost"] ?? "/";

            cfg.Host(host, virtualHost, h =>
            {
                var username = builder.Configuration["MassTransit:RabbitMq:Username"];
                var password = builder.Configuration["MassTransit:RabbitMq:Password"];

                if (!string.IsNullOrWhiteSpace(username))
                    h.Username(username);

                if (!string.IsNullOrWhiteSpace(password))
                    h.Password(password);
            });

            cfg.ConfigureEndpoints(context);
        });
    }
    else if (string.Equals(transport, "AzureServiceBus", StringComparison.OrdinalIgnoreCase))
    {
        x.UsingAzureServiceBus((context, cfg) =>
        {
            var connectionString = builder.Configuration["MassTransit:AzureServiceBus:ConnectionString"];
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new InvalidOperationException("MassTransit Azure Service Bus transport requires a connection string.");

            cfg.Host(connectionString);
            cfg.ConfigureEndpoints(context);
        });
    }
    else
    {
        x.UsingInMemory((context, cfg) =>
        {
            cfg.ConfigureEndpoints(context);
        });
    }
});

builder.Services.AddAdminModule(builder.Configuration);
builder.Services.AddIdentityModule(builder.Configuration);
builder.Services.AddProvisioningModule(builder.Configuration);

builder.Services.AddCors(options =>
{
    options.AddPolicy("LocalDevCors", policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AdminDbContext>();
    var provisioningDb = scope.ServiceProvider.GetRequiredService<ProvisioningDbContext>();
    var provisioningMetadataStoreProvisioner = scope.ServiceProvider.GetRequiredService<Callio.Provisioning.Infrastructure.Provisioners.IProvisioningMetadataStoreProvisioner>();

    await provisioningDb.Database.MigrateAsync();
    await provisioningMetadataStoreProvisioner.EnsureCreatedAsync();
    await AdminDataSeeder.SeedAsync(db);
}

app.MapIdentityModule();
app.MapCarter();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseCors("LocalDevCors");

app.Run();
