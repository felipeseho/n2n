using Microsoft.Extensions.DependencyInjection;
using n2n.Commands;
using n2n.Services;

namespace n2n;

public static class TestDI
{
    public static void Run()
    {
        Console.WriteLine("=== TESTE DE DI ===");
        
        var services = new ServiceCollection();
        services.AddN2NServices();
        services.AddScoped<MainCommand>();
        
        var provider = services.BuildServiceProvider();
        
        Console.WriteLine("Services registrados:");
        foreach (var service in services)
        {
            Console.WriteLine($"  - {service.ServiceType.Name} ({service.Lifetime})");
        }
        
        Console.WriteLine("\nTentando resolver MainCommand...");
        
        try
        {
            using var scope = provider.CreateScope();
            var command = scope.ServiceProvider.GetRequiredService<MainCommand>();
            Console.WriteLine("✓ MainCommand resolvido com sucesso!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ ERRO: {ex.Message}");
            
            if (ex.InnerException != null)
            {
                Console.WriteLine($"  Inner: {ex.InnerException.Message}");
                
                if (ex.InnerException.InnerException != null)
                {
                    Console.WriteLine($"  Inner Inner: {ex.InnerException.InnerException.Message}");
                }
            }
        }
    }
}

