using Microsoft.Extensions.Hosting;

namespace n2n.Infrastructure.Configuration;

public static class HostingExtensions
{
    public static IHostApplicationBuilder AddConfiguration(this IHostApplicationBuilder builder)
    {
        var configPath = Path.Combine(AppContext.BaseDirectory, "config.yaml");
        builder.Configuration.AddYamlFile(configPath, optional: false, reloadOnChange: true);
        
        builder.Services.AddOptions<RootConfig>()
            .Bind(builder.Configuration);
            
        return builder;
    }
}
