var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.ZMovie_Api>("api");


builder.Build().Run();
