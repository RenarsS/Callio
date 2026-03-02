var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.Callio_API>("api");
builder.AddProject<Projects.Callio_Dashboard>("dashboard");

builder.Build().Run();