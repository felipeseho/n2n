using n2n.Models;
using n2n.Services;
using Spectre.Console;
using Spectre.Console.Cli;

namespace n2n.Commands;

public class MainCommand : AsyncCommand<MainCommandSettings>
{
    private readonly ConfigurationService _configurationService;
    private readonly AppExecutionContext _executionContext;
    private readonly DashboardService _dashboardService;
    private readonly CsvProcessorService _csvProcessorService;

    public MainCommand(
        ConfigurationService configurationService,
        AppExecutionContext executionContext,
        DashboardService dashboardService,
        CsvProcessorService csvProcessorService)
    {
        _configurationService = configurationService;
        _executionContext = executionContext;
        _dashboardService = dashboardService;
        _csvProcessorService = csvProcessorService;
    }

    public override async Task<int> ExecuteAsync(
        CommandContext context,
        MainCommandSettings settings,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!File.Exists(settings.ConfigPath))
            {
                AnsiConsole.MarkupLine(
                    $"[red]✗[/] Arquivo de configuração não encontrado: [yellow]{settings.ConfigPath}[/]");
                AnsiConsole.MarkupLine("[grey]💡 Use: n2n --config caminho/do/arquivo.yaml[/]");

                return 1;
            }

            // Gerar ou usar executionId existente
            var currentExecutionId = settings.ExecutionId ?? Guid.NewGuid().ToString();

            // Criar opções de linha de comando
            var cmdOptions = new CommandLineOptions
            {
                ConfigPath = settings.ConfigPath,
                InputPath = settings.InputPath,
                BatchLines = settings.BatchLines,
                LogDirectory = settings.LogDirectory,
                CsvDelimiter = settings.Delimiter,
                StartLine = settings.StartLine,
                MaxLines = settings.MaxLines,
                ExecutionId = currentExecutionId,
                EndpointName = settings.EndpointName,
                Verbose = settings.Verbose,
                DryRun = settings.DryRun
            };
            
            // Carregar e validar configuração
            _dashboardService.AddLogMessage("Carregando configuração...", "INFO");
            var config = _configurationService.LoadConfiguration(settings.ConfigPath);
            config = _configurationService.MergeWithCommandLineOptions(config, cmdOptions);
            _dashboardService.AddLogMessage("Configuração carregada e mesclada com sucesso", "SUCCESS");
            
            // Validar configuração final
            _dashboardService.AddLogMessage("Validando configuração", "INFO");
            if (!_configurationService.ValidateConfiguration(config))
            {
                _dashboardService.AddLogMessage("Configuração inválida", "ERROR");
                AnsiConsole.MarkupLine("[red]✗ Configuração inválida[/]");
                return 1;
            }
            _dashboardService.AddLogMessage("Configuração validada com sucesso", "SUCCESS");

            // Criar diretórios necessários
            _dashboardService.AddLogMessage("Criando diretórios necessários", "INFO");
            _configurationService.EnsureDirectoriesExist(config);

            // Gerar caminhos de execução iniciais (será atualizado durante o processamento para cada arquivo)
            var executionPaths = _configurationService.GenerateExecutionPaths(config, currentExecutionId);

            // Obter endpoint ativo
            var activeEndpoint = _configurationService.GetEndpointConfiguration(config, cmdOptions.EndpointName);

            // Preencher ExecutionContext (agora todas as dependências têm a configuração)
            _executionContext.Configuration = config;
            _executionContext.CommandLineOptions = cmdOptions;
            _executionContext.ExecutionPaths = executionPaths;
            _executionContext.ActiveEndpoint = activeEndpoint;

            if (settings.DryRun)
            {
                _dashboardService.AddLogMessage("MODO DRY RUN ATIVADO - Nenhuma requisição será enviada", "WARNING");
            }

            _dashboardService.AddLogMessage("Iniciando processamento do arquivo CSV", "INFO");

            // Processar arquivo CSV (passando dashboardService como parâmetro)
            await _csvProcessorService.ProcessCsvFileAsync(_dashboardService);

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