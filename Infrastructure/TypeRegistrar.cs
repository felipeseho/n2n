using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;

namespace n2n.Infrastructure;

/// <summary>
///     Registrador de tipos para integrar Microsoft.Extensions.DependencyInjection com Spectre.Console.Cli
/// </summary>
public sealed class TypeRegistrar : ITypeRegistrar
{
    private readonly IServiceCollection _services;

    public TypeRegistrar(IServiceCollection services)
    {
        _services = services;
    }

    public ITypeResolver Build()
    {
        return new TypeResolver(_services.BuildServiceProvider());
    }

    public void Register(Type service, Type implementation)
    {
        _services.AddScoped(service, implementation);
    }

    public void RegisterInstance(Type service, object implementation)
    {
        _services.AddSingleton(service, implementation);
    }

    public void RegisterLazy(Type service, Func<object> factory)
    {
        _services.AddScoped(service, _ => factory());
    }
}

/// <summary>
///     Resolvedor de tipos para integrar Microsoft.Extensions.DependencyInjection com Spectre.Console.Cli
/// </summary>
public sealed class TypeResolver : ITypeResolver, IDisposable
{
    private readonly IServiceProvider _provider;
    private IServiceScope? _scope;

    public TypeResolver(IServiceProvider provider)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
    }

    public object? Resolve(Type? type)
    {
        if (type == null) return null;

        try
        {
            // Criar um scope se ainda não existe (para resolver serviços scoped)
            _scope ??= _provider.CreateScope();
            
            // Tentar obter o serviço
            var service = _scope.ServiceProvider.GetService(type);
            
            if (service == null)
            {
                throw new InvalidOperationException(
                    $"Could not resolve type '{type.FullName}'. " +
                    $"Verifique se o tipo está registrado no container DI.");
            }
            
            return service;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Erro ao resolver tipo '{type.FullName}': {ex.Message}", ex);
        }
    }

    public void Dispose()
    {
        _scope?.Dispose();
    }
}

