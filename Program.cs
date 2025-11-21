using Microsoft.Extensions.DependencyInjection;
using n2n.Commands;
using n2n.Services;
using Spectre.Console;
using Spectre.Console.Cli;
using Spectre.Console.Cli.Extensions.DependencyInjection;

// Configurar container de DI
var services = new ServiceCollection();
services.AddN2NServices();

// Configurar aplicação com DI usando a extensão oficial
using var registrar = new DependencyInjectionRegistrar(services);

var app = new CommandApp<MainCommand>(registrar);
app.Configure(config =>
{
    config.SetApplicationName("n2n");
    config.ValidateExamples();
});

try
{
    return await app.RunAsync(args);
}
catch (Exception ex)
{
    AnsiConsole.WriteException(ex, ExceptionFormats.ShortenEverything);
    return 1;
}