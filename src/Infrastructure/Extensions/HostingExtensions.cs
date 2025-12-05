using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace n2n.Infrastructure.Extensions;

public static class HostingExtensions
{
    extension(IHostApplicationBuilder builder)
    {
        public IHostApplicationBuilder AddInfrastructure()
        {
            // This will register Bus, Transformer, and PluginLoader in the future.
            return builder;
        }

        public IHostApplicationBuilder AddTelemetry()
        {
            // This will configure OTel, Metrics, and Logging providers.
            builder.Logging.ClearProviders();
            builder.Logging.AddOpenTelemetry();

            builder.Services.ConfigureOpenTelemetryLoggerProvider(options => options.AddConsoleExporter());
            builder.Services.ConfigureOpenTelemetryMeterProvider(options => options.AddConsoleExporter());
            builder.Services.ConfigureOpenTelemetryTracerProvider(options => options.AddConsoleExporter());
            
            return builder;
        }
    }
}
