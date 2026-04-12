var builder = DistributedApplication.CreateBuilder(args);

var server = builder.AddProject<Projects.PointerStar_Server>("server")
    .WithExternalHttpEndpoints()
    .WithEnvironment("EnableClientAppBuild", "false");

builder.AddViteApp("frontend", "..\\ClientApp")
    .WithReference(server)
    .WithEnvironment("VITE_BACKEND_URL", server.GetEndpoint("https"));

builder.Build().Run();
