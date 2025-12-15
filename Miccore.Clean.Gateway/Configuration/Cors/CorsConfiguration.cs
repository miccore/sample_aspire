namespace Miccore.Clean.Gateway.Configuration.Cors;

/// <summary>
/// Extension methods for configuring CORS policy.
/// Follows Single Responsibility Principle - handles only CORS configuration.
/// </summary>
public static class CorsConfiguration
{
    public const string PolicyName = "GatewayPolicy";

    /// <summary>
    /// Adds CORS policy with environment-specific configuration.
    /// Uses Options Pattern with validation for production settings.
    /// </summary>
    public static IServiceCollection AddCorsPolicy(this IServiceCollection services, IHostEnvironment environment, Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        // Bind and validate CORS options
        services.AddOptions<CorsOptions>()
            .Bind(configuration.GetSection(CorsOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddCors(options =>
        {
            options.AddPolicy(PolicyName, policy =>
            {
                if (environment.IsDevelopment())
                {
                    // Permissive CORS in development
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                }
                else
                {
                    // Restrictive CORS in production - configure allowed origins
                    var corsOptions = configuration.GetSection(CorsOptions.SectionName).Get<CorsOptions>();
                    var origins = corsOptions?.AllowedOrigins ?? [];

                    if (origins.Length > 0)
                    {
                        policy.WithOrigins(origins);
                    }

                    var methods = corsOptions?.AllowedMethods ?? [];
                    if (methods.Length > 0)
                    {
                        policy.WithMethods(methods);
                    }
                    else
                    {
                        policy.AllowAnyMethod();
                    }

                    var headers = corsOptions?.AllowedHeaders ?? [];
                    if (headers.Length > 0)
                    {
                        policy.WithHeaders(headers);
                    }
                    else
                    {
                        policy.AllowAnyHeader();
                    }

                    if (corsOptions?.AllowCredentials == true && origins.Length > 0)
                    {
                        policy.AllowCredentials();
                    }
                }
            });
        });

        return services;
    }

    /// <summary>
    /// Applies CORS middleware with the configured policy.
    /// </summary>
    public static WebApplication UseCorsPolicy(this WebApplication app)
    {
        app.UseCors(PolicyName);
        return app;
    }
}
