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
            return builder;
        }
    }
}
