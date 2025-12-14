var builder = DistributedApplication.CreateBuilder(args);

#region Orchestration

#region Local Projects (same repo)
// Add projects from this repository using AddProject<>
// Example:
// var localService = builder.AddProject<Projects.LocalService>("localservice");
#endregion

#region External Services (different repos)
// Option 1: Container-based services (RECOMMENDED for multi-repo)
// Build and publish your services as Docker images, then reference them here
// Example:
// var catalogService = builder.AddContainer("catalog", "your-registry/catalog-api", "latest")
//     .WithHttpEndpoint(port: 5001, targetPort: 8080, name: "http")
//     .WithEnvironment("ASPNETCORE_ENVIRONMENT", builder.Environment.EnvironmentName);

// Option 2: External project references (requires local checkout of other repos)
// Example:
// var catalogService = builder.AddProject("catalog", "../../CatalogRepo/src/CatalogService/CatalogService.csproj");

// Option 3: Executable/Published binaries
// Example:
// var catalogService = builder.AddExecutable("catalog",
//     "../../CatalogRepo/publish/CatalogService",
//     "../../CatalogRepo/publish")
//     .WithHttpEndpoint(port: 5001);
#endregion

#region Infrastructure Resources
// Add external resources (databases, message queues, caches)
// Example:
// var postgres = builder.AddPostgres("postgres")
//     .WithPgAdmin();
// var catalogDb = postgres.AddDatabase("catalogdb");
// 
// var redis = builder.AddRedis("redis");
// var rabbitmq = builder.AddRabbitMQ("messaging");
#endregion

#endregion

builder.AddProject<Projects.Miccore_Clean_Gateway>("Gateway")
#region Gateway Configuration
    // Reference your services here to enable Aspire service discovery
    // The gateway will automatically resolve service names to their endpoints
    // Example:
    // .WithReference(catalogService)
    // .WithReference(ordersService)
    // .WithReference(identityService)
    // .WithReference(redis)
    
    // For each referenced service, add an entry in Gateway's appsettings.json:
    // "Services": {
    //   "catalog": { "DownstreamPath": "http://catalog" },
    //   "orders": { "DownstreamPath": "http://orders" }
    // }
    // 
    // Then configure routes in ocelot.json using ServiceName (not DownstreamHostAndPorts)
    // See ocelot.routes.example.json for complete route configuration examples
    // 
    // IMPORTANT: Service names must match between:
    // - The name used in AddContainer/AddProject/AddExecutable (e.g., "catalog")
    // - The ServiceName in ocelot.json routes
    // - The key in appsettings.json Services section
#endregion
;

builder.Build().Run();
