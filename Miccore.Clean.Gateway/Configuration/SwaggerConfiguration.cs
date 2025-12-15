using Microsoft.OpenApi;

namespace Miccore.Clean.Gateway.Configuration;

/// <summary>
/// Extension methods for configuring Swagger/OpenAPI documentation.
/// Follows Single Responsibility Principle - handles only Swagger configuration.
/// </summary>
public static class SwaggerConfiguration
{
    /// <summary>
    /// Adds Swagger services with Ocelot aggregation support.
    /// </summary>
    public static IServiceCollection AddSwaggerServices(this IServiceCollection services, Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        // Configure Swagger for Ocelot aggregation
        services.AddSwaggerForOcelot(configuration, null, options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Miccore.Clean.Gateway API",
                Version = "v1",
                Description = "API Gateway for Miccore.Clean microservices architecture",
                Contact = new OpenApiContact
                {
                    Name = "Miccore Team"
                }
            });
        });

        services.AddOpenApi();

        return services;
    }

    /// <summary>
    /// Configures Swagger UI middleware pipeline.
    /// </summary>
    public static WebApplication UseSwaggerServices(this WebApplication app)
    {
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

        // Swagger aggregation for downstream services
        app.UseSwaggerForOcelotUI(opt =>
        {
            opt.PathToSwaggerGenerator = "/swagger/docs";
        });

        return app;
    }
}
