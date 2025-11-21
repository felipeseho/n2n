﻿﻿using Microsoft.Extensions.DependencyInjection;
using n2n;
using n2n.Commands;
using n2n.Infrastructure;
using n2n.Services;
using Spectre.Console;
using Spectre.Console.Cli;

// TESTE DE DI (remover depois)
if (args.Length > 0 && args[0] == "--test-di")
{
    TestDI.Run();
    return 0;
}

// Configurar container de DI
var services = new ServiceCollection();
services.AddN2NServices(); 

// Criar registrador de tipos para Spectre.Console.Cli
var registrar = new TypeRegistrar(services);

// Configurar aplicação com DI
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