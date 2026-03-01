var builder = DistributedApplication.CreateBuilder(args);

var sql = builder.AddProject<Projects.Callio_API>("api");

builder.Build().Run();