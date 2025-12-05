using n2n.Infrastructure.Configuration;

namespace n2n.Extensions;

public static class HostingExtensions
{
    extension(IHostApplicationBuilder builder)
    {
        public IHostApplicationBuilder AddConfiguration()
        {
            var configPath = Path.Combine(AppContext.BaseDirectory, "config.yaml");
            builder.Configuration.AddYamlFile(configPath, optional: false, reloadOnChange: true);
        
            builder.Services.AddOptions<RootConfig>()
                .Bind(builder.Configuration);
            
            return builder;
        }

        public IHostApplicationBuilder AddWorkers()
        {
            builder.Services.AddHostedService<Worker>();
            return builder;
        }
    }
}
