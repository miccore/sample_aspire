namespace Miccore.Clean.Gateway.Configuration.Cors;

/// <summary>
/// Configuration options for CORS policy.
/// Supports validation via DataAnnotations for fail-fast configuration.
/// </summary>
public class CorsOptions
{
    public const string SectionName = "Cors";

    /// <summary>
    /// List of allowed origins for CORS requests in production.
    /// Required in production environment.
    /// </summary>
    public string[] AllowedOrigins { get; set; } = [];

    /// <summary>
    /// List of allowed HTTP methods. Defaults to all methods if empty.
    /// </summary>
    public string[] AllowedMethods { get; set; } = [];

    /// <summary>
    /// List of allowed headers. Defaults to all headers if empty.
    /// </summary>
    public string[] AllowedHeaders { get; set; } = [];

    /// <summary>
    /// Whether to allow credentials in CORS requests.
    /// </summary>
    public bool AllowCredentials { get; set; } = true;
}
