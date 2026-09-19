using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Npgsql;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using RIN.Core.Config;
using Serilog;
using Serilog.Sinks.OpenTelemetry;

namespace RIN.Core.Hosting
{
    public static class ObservabilityExtensions
    {
        public const string HealthPath = "/health";

        public static bool IsOtlpEnabled(IConfiguration config) => !string.IsNullOrWhiteSpace(config["OTEL_EXPORTER_OTLP_ENDPOINT"]);

        public static WebApplicationBuilder AddRinObservability(this WebApplicationBuilder builder)
        {
            builder.Services.AddHealthChecks().AddCheck<DatabaseHealthCheck>("postgres");

            if (!IsOtlpEnabled(builder.Configuration))
            {
                return builder;
            }

            builder.Services.AddOpenTelemetry()
                   .WithTracing(tracing => tracing.AddAspNetCoreInstrumentation(o => o.Filter = ctx => ctx.Request.Path != HealthPath)
                                                  .AddHttpClientInstrumentation()
                                                  .AddNpgsql())
                   .WithMetrics(metrics => metrics.AddAspNetCoreInstrumentation()
                                                  .AddHttpClientInstrumentation()
                                                  .AddNpgsqlInstrumentation())
                   .UseOtlpExporter();

            return builder;
        }

        public static WebApplication MapRinHealthChecks(this WebApplication app)
        {
            app.MapHealthChecks(HealthPath);
            return app;
        }

        public static LoggerConfiguration WriteToOpenTelemetryIfEnabled(this LoggerConfiguration logger, IConfiguration config)
        {
            if (!IsOtlpEnabled(config))
            {
                return logger;
            }

            return logger.WriteTo.OpenTelemetry(options =>
            {
                if (config["OTEL_SERVICE_NAME"] is { } serviceName)
                {
                    options.ResourceAttributes["service.name"] = serviceName;
                }

                options.IncludedData |= IncludedData.TraceIdField | IncludedData.SpanIdField;
            });
        }

        private sealed class DatabaseHealthCheck(IOptions<DbConnectionSettings> settings) : IHealthCheck
        {
            public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
            {
                try
                {
                    await using var conn = new NpgsqlConnection(settings.Value.DBConnStr);
                    await conn.OpenAsync(cancellationToken);
                    await using var cmd = new NpgsqlCommand("SELECT 1", conn);
                    await cmd.ExecuteScalarAsync(cancellationToken);
                    return HealthCheckResult.Healthy();
                }
                catch (Exception e)
                {
                    return HealthCheckResult.Unhealthy("Can't reach the database", e);
                }
            }
        }
    }
}
