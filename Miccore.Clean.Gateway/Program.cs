using Microsoft.OpenApi.Models;
using MMLib.Ocelot.Provider.AppConfiguration;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;

var builder = WebApplication.CreateBuilder(args);

#region Configuration
// Load configuration files with environment-specific overrides
builder.Configuration
    .SetBasePath(builder.Environment.ContentRootPath)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: true)
    .AddJsonFile("ocelot.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"ocelot.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();
#endregion

#region Services
// Add Aspire service defaults (OpenTelemetry, Health Checks, Service Discovery)
builder.AddServiceDefaults();

// Add Memory Cache (required by Ocelot AppConfiguration provider)
builder.Services.AddMemoryCache();

// Add Ocelot with AppConfiguration provider for dynamic service discovery
builder.Services.AddOcelot()
    .AddAppConfiguration();

// Configure Swagger for Ocelot aggregation
builder.Services.AddSwaggerForOcelot(builder.Configuration, null, options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Miccore.Clean.Gateway API",
        Version = "v1",
        Description = "API Gateway for Miccore.Clean microservices architecture"
    });
});

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("GatewayPolicy", policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            // Permissive CORS in development
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        }
        else
        {
            // Restrictive CORS in production - configure allowed origins
            policy.WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [])
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
        }
    });
});

builder.Services.AddOpenApi();
#endregion

var app = builder.Build();

#region Middleware Pipeline
// Map Aspire health check endpoints (/health, /alive)
app.MapDefaultEndpoints();

// Enable CORS
app.UseCors("GatewayPolicy");

// Configure Swagger UI
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Gateway API");
    c.RoutePrefix = string.Empty;
});

app.UseHttpsRedirection();

// Swagger aggregation for downstream services
app.UseSwaggerForOcelotUI(opt =>
{
    opt.PathToSwaggerGenerator = "/swagger/docs";
});

// Ocelot middleware (async pattern)
await app.UseOcelot();
#endregion

app.Run();                                                                  