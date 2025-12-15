namespace Miccore.Clean.ServiceDefaults.Extensions;

/// <summary>
/// Extension methods for configuring HTTP client resilience and service discovery.
/// Follows Single Responsibility Principle - handles only resilience and discovery configuration.
/// </summary>
public static class ResilienceExtensions
{
    /// <summary>
    /// Configures HTTP client defaults with resilience handlers and service discovery.
    /// </summary>
    public static TBuilder ConfigureHttpClientDefaults<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.Services.AddServiceDiscovery();

        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            // Turn on resilience by default
            http.AddStandardResilienceHandler();

            // Turn on service discovery by default
            http.AddServiceDiscovery();
        });

        // Uncomment the following to restrict the allowed schemes for service discovery.
        // builder.Services.Configure<ServiceDiscoveryOptions>(options =>
        // {
        //     options.AllowedSchemes = ["https"];
        // });

        return builder;
    }
}
