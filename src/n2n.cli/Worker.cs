using Microsoft.Extensions.Options;
using n2n.Infrastructure.Configuration;

namespace n2n;

public class Worker(
    ILogger<Worker> logger,
    IOptions<RootConfig> options) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation(
            "Worker started with {sources} sources, {destinations} destinations, and {pipelines} pipelines",
            options.Value.Sources.Count,
            options.Value.Destinations.Count,
            options.Value.Pipelines.Count);

        while (!stoppingToken.IsCancellationRequested)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            }

            await Task.Delay(1000, stoppingToken);
        }
    }
}