using Callio.Admin.Infrastructure;
using Callio.Identity.Infrastructure;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAdminModule(builder.Configuration);
builder.Services.AddIdentityModule(builder.Configuration);

builder.Services.AddOpenApi();

var app = builder.Build();

app.MapIdentityModule();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.Run();