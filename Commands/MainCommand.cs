using n2n.Models;
using n2n.Services;
using Spectre.Console;
using Spectre.Console.Cli;

namespace n2n;

public class MainCommand : AsyncCommand<CommandSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, CommandSettings commandSettings, CancellationToken cancellationToken)
    {
        try
        {
            // Verificar se o arquivo de configuração existe
            if (!File.Exists(commandSettings.ConfigPath))
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Arquivo de configuração não encontrado: [yellow]{commandSettings.ConfigPath}[/]");
                AnsiConsole.MarkupLine("[grey]💡 Use: csv-to-api --config caminho/do/arquivo.yaml[/]");
                return 1;
            }

            // Gerar ou usar executionId existente
            var currentExecutionId = commandSettings.ExecutionId ?? Guid.NewGuid().ToString();

            // Criar opções de linha de comando
            var cmdOptions = new CommandLineOptions
            {
                ConfigPath = commandSettings.ConfigPath,
                InputPath = commandSettings.InputPath,
                BatchLines = commandSettings.BatchLines,
                LogDirectory = commandSettings.LogDirectory,
                CsvDelimiter = commandSettings.Delimiter,
                StartLine = commandSettings.StartLine,
                MaxLines = commandSettings.MaxLines,
                ExecutionId = currentExecutionId,
                EndpointName = commandSettings.EndpointName,
                Verbose = commandSettings.Verbose,
                DryRun = commandSettings.DryRun
            };

            // Mostrar configuração se verbose
            if (commandSettings.Verbose)
            {
                var configTable = new Table()
                    .Border(TableBorder.Rounded)
                    .BorderColor(Color.Grey)
                    .AddColumn(new TableColumn("[cyan1]Configuração[/]").Centered())
                    .AddColumn(new TableColumn("[cyan1]Valor[/]"));

                configTable.AddRow("Config", commandSettings.ConfigPath);
                if (commandSettings.InputPath != null) configTable.AddRow("Input", commandSettings.InputPath);
                if (commandSettings.BatchLines != null) configTable.AddRow("Batch Lines", commandSettings.BatchLines.ToString()!);
                if (commandSettings.StartLine != null) configTable.AddRow("Start Line", commandSettings.StartLine.ToString()!);
                if (commandSettings.MaxLines != null) configTable.AddRow("Max Lines", commandSettings.MaxLines.ToString()!);
                if (commandSettings.EndpointName != null) configTable.AddRow("Endpoint Name", commandSettings.EndpointName);
                if (commandSettings.DryRun) configTable.AddRow("[yellow]Modo[/]", "[yellow]DRY RUN[/]");

                AnsiConsole.Write(configTable);
                AnsiConsole.WriteLine();
            }

            // Inicializar serviços
            var configService = new ConfigurationService();
            var validationService = new ValidationService();
            var loggingService = new LoggingService();
            var checkpointService = new CheckpointService();
            var metricsService = new MetricsService();

            // Carregar configuração do YAML
            Configuration config = AnsiConsole
                .Status()
                .Spinner(Spinner.Known.Dots)
                .SpinnerStyle(Style.Parse("cyan1"))
                .Start<Configuration>("[cyan1]Carregando configuração...[/]", ctx =>
                {
                    return configService.LoadConfiguration(commandSettings.ConfigPath);
                });

            config = configService.LoadConfiguration(commandSettings.ConfigPath);

            // Mesclar com opções de linha de comando
            config = configService.MergeWithCommandLineOptions(config, cmdOptions);

            // Validar configuração final
            if (!configService.ValidateConfiguration(config))
            {
                AnsiConsole.MarkupLine("[red]✗ Configuração inválida[/]");
                return 1;
            }

            // Criar diretórios necessários
            configService.EnsureDirectoriesExist(config);

            // Exibir UUID da execução
            var panel = new Panel(
                    new Markup(commandSettings.ExecutionId != null
                        ? $"[cyan1]🔄 Continuando execução[/]\n[yellow]{currentExecutionId}[/]"
                        : $"[cyan1]✨ Nova execução iniciada[/]\n[yellow]{currentExecutionId}[/]"))
                .Border(BoxBorder.Rounded)
                .BorderColor(Color.Cyan1)
                .Header("[cyan1]Execution ID[/]");

            AnsiConsole.Write(panel);
            AnsiConsole.WriteLine();

            // Gerar caminhos de execução
            var executionPaths = configService.GenerateExecutionPaths(config, currentExecutionId);

            // Usar primeiro endpoint ou default para inicializar ApiClientService
            var referenceEndpoint = config.Endpoints.FirstOrDefault();
            if (referenceEndpoint == null)
            {
                AnsiConsole.MarkupLine("[red]✗ Nenhum endpoint configurado[/]");
                return 1;
            }

            // Inicializar ApiClientService com o endpoint de referência e MetricsService
            var apiClientService = new ApiClientService(loggingService, referenceEndpoint, metricsService);
            var processorService = new CsvProcessorService(validationService, loggingService, apiClientService, checkpointService, metricsService);

            if (commandSettings.DryRun)
            {
                AnsiConsole.MarkupLine("[yellow]🔍 MODO DRY RUN: Nenhuma requisição será enviada à API[/]");
                AnsiConsole.WriteLine();
            }

            AnsiConsole.MarkupLine("[cyan1]🚀 Iniciando processamento do arquivo CSV...[/]");
            AnsiConsole.WriteLine();

            // Processar arquivo CSV
            await processorService.ProcessCsvFileAsync(config, executionPaths, commandSettings.DryRun, cmdOptions.EndpointName);

            // Sucesso
            var successRule = new Rule("[green]✓ Processamento concluído com sucesso![/]")
                .RuleStyle(Style.Parse("green"));
            AnsiConsole.Write(successRule);

            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[red]✗ Erro durante o processamento[/]");
            AnsiConsole.WriteException(ex, ExceptionFormats.ShortenPaths | ExceptionFormats.ShortenTypes);
            return 1;
        }
    }
}