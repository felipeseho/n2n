using Microsoft.Extensions.DependencyInjection;
using n2n.Commands;
using n2n.Services;
using Spectre.Console.Cli;
using Spectre.Console.Cli.Extensions.DependencyInjection;

// Configurar container de DI
var services = new ServiceCollection();
services.AddN2NServices();

// Configurar aplicação com DI usando a extensão oficial
using var registrar = new DependencyInjectionRegistrar(services);

var app = new CommandApp<PipelineCommand>(registrar);
app.Configure(config =>
{
    config.SetApplicationName("n2n");
    config.SetApplicationVersion("2.0.0");
    config.ValidateExamples();
    
    // Adicionar comando para listar checkpoints
    config.AddCommand<ListCheckpointsCommand>("checkpoints")
        .WithDescription("Lista checkpoints disponíveis para retomada de execução")
        .WithExample("checkpoints")
        .WithExample("checkpoints", "--directory", "checkpoints");
});

try
{
    return await app.RunAsync(args);
}
catch (Exception ex)
{
    Console.WriteLine($"Erro fatal: {ex.Message}");
    return 1;
}