using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Miccore.Clean.ServiceDefaults.Extensions;

/// <summary>
/// Extension methods for configuring OpenTelemetry observability.
/// Follows Single Responsibility Principle - handles only OpenTelemetry configuration.
/// </summary>
public static class OpenTelemetryExtensions
{
    /// <summary>
    /// Configures complete OpenTelemetry stack including logging, metrics, and tracing.
    /// </summary>
    public static TBuilder ConfigureOpenTelemetry<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.AddOpenTelemetryLogging();
        builder.AddOpenTelemetryMetrics();
        builder.AddOpenTelemetryTracing();
        builder.AddOpenTelemetryExporters();

        return builder;
    }

    /// <summary>
    /// Configures OpenTelemetry logging with formatted messages and scopes.
    /// </summary>
    public static TBuilder AddOpenTelemetryLogging<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        return builder;
    }

    /// <summary>
    /// Configures OpenTelemetry metrics for ASP.NET Core, HTTP clients, and runtime.
    /// </summary>
    public static TBuilder AddOpenTelemetryMetrics<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.Services.AddOpenTelemetry()
            .WithMetrics(metrics =>
            {
                metrics.AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation();
            });

        return builder;
    }

    /// <summary>
    /// Configures OpenTelemetry tracing for ASP.NET Core and HTTP clients.
    /// </summary>
    public static TBuilder AddOpenTelemetryTracing<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.Services.AddOpenTelemetry()
            .WithTracing(tracing =>
            {
                tracing.AddSource(builder.Environment.ApplicationName)
                    .AddAspNetCoreInstrumentation()
                    // Uncomment the following line to enable gRPC instrumentation (requires the OpenTelemetry.Instrumentation.GrpcNetClient package)
                    //.AddGrpcClientInstrumentation()
                    .AddHttpClientInstrumentation();
            });

        return builder;
    }

    /// <summary>
    /// Configures OpenTelemetry exporters based on environment configuration.
    /// Supports OTLP exporter when OTEL_EXPORTER_OTLP_ENDPOINT is configured.
    /// </summary>
    public static TBuilder AddOpenTelemetryExporters<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        var useOtlpExporter = !string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);

        if (useOtlpExporter)
        {
            builder.Services.AddOpenTelemetry().UseOtlpExporter();
        }

        // Uncomment the following lines to enable the Azure Monitor exporter (requires the Azure.Monitor.OpenTelemetry.AspNetCore package)
        //if (!string.IsNullOrEmpty(builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]))
        //{
        //    builder.Services.AddOpenTelemetry()
        //       .UseAzureMonitor();
        //}

        return builder;
    }
}
