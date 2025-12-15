using Miccore.Clean.ServiceDefaults.Extensions;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Main entry point for adding common .NET Aspire services.
/// This class orchestrates calls to specialized extension classes following the Single Responsibility Principle.
/// This project should be referenced by each service project in your solution.
/// To learn more about using this project, see https://aka.ms/dotnet/aspire/service-defaults
/// </summary>
public static class Extensions
{
    /// <summary>
    /// Adds common .NET Aspire services: service discovery, resilience, health checks, and OpenTelemetry.
    /// </summary>
    /// <remarks>
    /// This method orchestrates calls to:
    /// <list type="bullet">
    ///   <item><description><see cref="OpenTelemetryExtensions.ConfigureOpenTelemetry{TBuilder}"/> - Observability</description></item>
    ///   <item><description><see cref="HealthChecksExtensions.AddDefaultHealthChecks{TBuilder}"/> - Health checks</description></item>
    ///   <item><description><see cref="ResilienceExtensions.ConfigureHttpClientDefaults{TBuilder}"/> - Resilience and service discovery</description></item>
    /// </list>
    /// </remarks>
    public static TBuilder AddServiceDefaults<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        // Configure OpenTelemetry observability (logging, metrics, tracing)
        builder.ConfigureOpenTelemetry();

        // Add health checks for Aspire dashboard integration
        builder.AddDefaultHealthChecks();

        // Configure HTTP client defaults with resilience and service discovery
        builder.ConfigureHttpClientDefaults();

        return builder;
    }

    /// <summary>
    /// Adds gateway-specific defaults including memory cache and response compression.
    /// Call this method in addition to AddServiceDefaults for API Gateway projects.
    /// </summary>
    /// <remarks>
    /// Delegates to <see cref="GatewayExtensions.AddGatewayDefaults{TBuilder}"/>.
    /// </remarks>
    public static TBuilder AddGatewayDefaults<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        return GatewayExtensions.AddGatewayDefaults(builder);
    }

    /// <summary>
    /// Maps default health check endpoints for Aspire integration.
    /// </summary>
    /// <remarks>
    /// Delegates to <see cref="HealthChecksExtensions.MapDefaultEndpoints"/>.
    /// </remarks>
    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        return HealthChecksExtensions.MapDefaultEndpoints(app);
    }

    /// <summary>
    /// Maps gateway-specific endpoints including response compression middleware.
    /// </summary>
    /// <remarks>
    /// Delegates to <see cref="GatewayExtensions.MapGatewayEndpoints"/>.
    /// </remarks>
    public static WebApplication MapGatewayEndpoints(this WebApplication app)
    {
        return GatewayExtensions.MapGatewayEndpoints(app);
    }
}
