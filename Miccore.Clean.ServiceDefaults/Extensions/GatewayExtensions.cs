namespace Miccore.Clean.ServiceDefaults.Extensions;

/// <summary>
/// Extension methods specific to API Gateway projects.
/// Follows Single Responsibility Principle - handles only gateway-specific configuration.
/// </summary>
public static class GatewayExtensions
{
    /// <summary>
    /// Adds gateway-specific defaults including memory cache and response compression.
    /// Call this method in addition to AddServiceDefaults for API Gateway projects.
    /// </summary>
    public static TBuilder AddGatewayDefaults<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        // Add service defaults first
        builder.AddServiceDefaults();

        // Add memory cache (required for Ocelot rate limiting and caching)
        builder.Services.AddMemoryCache();

        // Add response compression
        builder.Services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true;
        });

        // Optional: Add JWT authentication if configured
        // To enable JWT, add JWT section to appsettings.json:
        // "Jwt": { "Authority": "https://your-identity-server", "Audience": "your-api" }
        // And install Microsoft.AspNetCore.Authentication.JwtBearer package

        return builder;
    }

    /// <summary>
    /// Maps gateway-specific endpoints including response compression middleware.
    /// Call this method instead of MapDefaultEndpoints for API Gateway projects.
    /// </summary>
    public static WebApplication MapGatewayEndpoints(this WebApplication app)
    {
        // Map default health check endpoints
        app.MapDefaultEndpoints();

        // Enable response compression
        app.UseResponseCompression();

        return app;
    }
}
