using Carter;
using Callio.Admin.Infrastructure;
using Callio.Admin.Infrastructure.Persistence;
using Callio.Identity.Infrastructure;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddCarter();

builder.Services.AddAdminModule(builder.Configuration);
builder.Services.AddIdentityModule(builder.Configuration);

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