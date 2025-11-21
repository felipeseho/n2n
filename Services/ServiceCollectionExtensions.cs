using Microsoft.Extensions.DependencyInjection;
using n2n.Commands;
using n2n.Models;

namespace n2n.Services;

/// <summary>
///     Extensões para registrar todos os serviços da aplicação no container de DI
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddN2NServices(this IServiceCollection services)
    {
        // Contexto de execução (Scoped - um por execução)
        services.AddScoped<AppExecutionContext>();

        // Serviços de infraestrutura (Singleton - stateless)
        services.AddSingleton<ConfigurationService>();
        services.AddSingleton<ValidationService>();
        services.AddSingleton<CheckpointService>();

        // Serviços de processamento (Scoped - um por execução para isolar estado)
        services.AddScoped<MetricsService>();
        services.AddScoped<LoggingService>();
        services.AddScoped<DashboardService>();

        // Serviços de API (Scoped)
        services.AddScoped<ApiClientService>();
        services.AddScoped<CsvProcessorService>();

        return services;
    }
}

