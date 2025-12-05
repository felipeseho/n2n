using n2n.Infrastructure.Configuration;

namespace n2n.Extensions;

public static class HostingExtensions
{
    extension(IHostApplicationBuilder builder)
    {
        public IHostApplicationBuilder AddConfiguration(string[] args)
        {
            var basePath = builder.Environment.ContentRootPath;
            var effectivePath = Path.Combine(basePath, "config.yaml");

            builder.Configuration.AddYamlFile(effectivePath, optional: false, reloadOnChange: true);
            
            builder.Services.Configure<RootConfig>(options =>
            {
                var boundConfig = builder.Configuration.BindRootConfig();
                options.Sources = boundConfig.Sources;
                options.Destinations = boundConfig.Destinations;
                options.Pipelines = boundConfig.Pipelines;
            });

            return builder;
        }

        public IHostApplicationBuilder AddWorkers()
        {
            builder.Services.AddHostedService<Worker>();
            return builder;
        }
    }
}