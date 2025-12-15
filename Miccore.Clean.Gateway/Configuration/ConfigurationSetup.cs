namespace Miccore.Clean.Gateway.Configuration;

/// <summary>
/// Extension methods for configuring application configuration sources.
/// Follows Single Responsibility Principle - handles only configuration loading.
/// </summary>
public static class ConfigurationSetup
{
    /// <summary>
    /// Configures all configuration sources including environment-specific overrides.
    /// </summary>
    public static IConfigurationBuilder AddGatewayConfiguration(this IConfigurationBuilder configuration, IHostEnvironment environment)
    {
        configuration
            .SetBasePath(environment.ContentRootPath)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
            .AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: true)
            .AddJsonFile("ocelot.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"ocelot.{environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables();

        return configuration;
    }
}
