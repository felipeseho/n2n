using Microsoft.Extensions.FileProviders;

namespace n2n.Infrastructure.Configuration;

public static class YamlConfigurationExtensions
{
    public static IConfigurationBuilder AddYamlFile(this IConfigurationBuilder builder, string path, bool optional = false, bool reloadOnChange = false)
    {
        ArgumentNullException.ThrowIfNull(builder);

        if (string.IsNullOrEmpty(path))
        {
            throw new ArgumentException("File path must be a non-empty string.", nameof(path));
        }

        var source = new YamlConfigurationSource
        {
            Optional = optional,
            ReloadOnChange = reloadOnChange
        };

        if (Path.IsPathRooted(path))
        {
            var dir = Path.GetDirectoryName(path)!;
            var file = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
            {
                source.FileProvider = new PhysicalFileProvider(dir);
                source.Path = file;
            }
            else
            {
                source.Path = path;
            }
        }
        else
        {
            source.Path = path;
        }
        
        builder.Add(source);
        
        return builder;
    }
}
