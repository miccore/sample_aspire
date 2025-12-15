using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Miccore.Clean.ServiceDefaults.Extensions;

/// <summary>
/// Extension methods for configuring health checks.
/// Follows Single Responsibility Principle - handles only health check configuration.
/// </summary>
public static class HealthChecksExtensions
{
    /// <summary>
    /// Adds default health checks including a liveness probe.
    /// </summary>
    public static TBuilder AddDefaultHealthChecks<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.Services.AddHealthChecks()
            // Add a default liveness check to ensure app is responsive
            .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

        return builder;
    }

    /// <summary>
    /// Maps default health check endpoints for Aspire integration.
    /// Only enabled in development environment for security.
    /// </summary>
    /// <remarks>
    /// Adding health checks endpoints to applications in non-development environments has security implications.
    /// See https://aka.ms/dotnet/aspire/healthchecks for details before enabling these endpoints in non-development environments.
    /// </remarks>
    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            // All health checks must pass for app to be considered ready to accept traffic after starting
            app.MapHealthChecks("/health");

            // Only health checks tagged with the "live" tag must pass for app to be considered alive
            app.MapHealthChecks("/alive", new HealthCheckOptions
            {
                Predicate = r => r.Tags.Contains("live")
            });
        }

        return app;
    }
}
