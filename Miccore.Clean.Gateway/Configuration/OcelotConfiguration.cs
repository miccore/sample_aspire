using MMLib.Ocelot.Provider.AppConfiguration;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;

namespace Miccore.Clean.Gateway.Configuration;

/// <summary>
/// Extension methods for configuring Ocelot API Gateway.
/// Follows Single Responsibility Principle - handles only Ocelot configuration.
/// </summary>
public static class OcelotConfiguration
{
    /// <summary>
    /// Adds Ocelot services with AppConfiguration provider for dynamic service discovery.
    /// </summary>
    public static IServiceCollection AddOcelotServices(this IServiceCollection services)
    {
        services.AddOcelot()
            .AddAppConfiguration();

        return services;
    }

    /// <summary>
    /// Configures Ocelot middleware in the request pipeline.
    /// </summary>
    public static async Task UseOcelotMiddleware(this WebApplication app)
    {
        await app.UseOcelot();
    }
}
