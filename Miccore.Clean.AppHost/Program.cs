var builder = DistributedApplication.CreateBuilder(args);

#region Orchestration
#endregion

builder.AddProject<Projects.Miccore_Clean_Gateway>("Gateway")
#region Gateway configuration
#endregion
;

builder.Build().Run();
