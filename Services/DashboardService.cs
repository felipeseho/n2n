using n2n.Models;
using Spectre.Console;

namespace n2n.Services;

public class DashboardService
{
    private readonly MetricsService _metricsService;
    private readonly AppExecutionContext _context;
    private readonly List<string> _logMessages = new();
    private readonly int _maxLogMessages = 10;
    
    // Application info
    private string _appName = "n2n - No Name to Nibble";
    private string _appVersion = "1.0.0";
    private string _appDescription = "CSV to API Data Processor";
    
    // State
    private bool _isRunning;

    public DashboardService(MetricsService metricsService, AppExecutionContext context)
    {
        _metricsService = metricsService;
        _context = context;
    }

    /// <summary>
    ///     Configura informações da aplicação
    /// </summary>
    public void SetApplicationInfo(string name, string version, string description)
    {
        _appName = name;
        _appVersion = version;
        _appDescription = description;
    }


    /// <summary>
    ///     Adiciona mensagem de log ao footer
    /// </summary>
    public void AddLogMessage(string message, string level = "INFO")
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        var color = level.ToUpper() switch
        {
            "ERROR" => "red",
            "WARNING" => "yellow",
            "SUCCESS" => "green",
            "INFO" => "cyan1",
            _ => "grey"
        };
        
        var formattedMessage = $"[grey]{timestamp}[/] [{color}]{level}[/] {message}";
        _logMessages.Add(formattedMessage);
        
        // Manter apenas as últimas N mensagens
        if (_logMessages.Count > _maxLogMessages)
        {
            _logMessages.RemoveAt(0);
        }
    }

    /// <summary>
    ///     Inicia o dashboard em modo live (atualização contínua)
    /// </summary>
    public async Task StartLiveDashboard(CancellationToken cancellationToken)
    {
        _isRunning = true;

        // Limpar console para fullscreen
        AnsiConsole.Clear();

        await AnsiConsole.Live(CreateLayout())
            .AutoClear(false)
            .Overflow(VerticalOverflow.Visible)
            .Cropping(VerticalOverflowCropping.Bottom)
            .StartAsync(async ctx =>
            {
                while (_isRunning && !cancellationToken.IsCancellationRequested)
                {
                    ctx.UpdateTarget(CreateLayout());
                    await Task.Delay(500, cancellationToken); // Atualizar a cada 500ms
                }
            });
    }

    /// <summary>
    ///     Para o dashboard
    /// </summary>
    public void Stop()
    {
        _isRunning = false;
    }

    /// <summary>
    ///     Atualiza o dashboard uma única vez (sem modo live)
    /// </summary>
    public void UpdateOnce()
    {
        AnsiConsole.Clear();
        AnsiConsole.Write(CreateLayout());
    }

    /// <summary>
    ///     Cria o layout do dashboard
    /// </summary>
    private Layout CreateLayout()
    {
        var metrics = _metricsService.GetMetrics();

        // Layout principal: Header, Body (2x2), Footer - proporções para fullscreen
        var layout = new Layout("Root")
            .SplitRows(
                new Layout("Header").Size(5),
                new Layout("Body"),
                new Layout("Footer").Size(13)
            );

        // Header com informações da aplicação
        layout["Header"].Update(CreateHeaderPanel());

        // Body dividido em 2 linhas (proporção automática)
        layout["Body"].SplitRows(
            new Layout("Row1"),
            new Layout("Row2")
        );

        // Linha 1: Parâmetros | Arquivo (50% cada coluna)
        layout["Body"]["Row1"].SplitColumns(
            new Layout("Parameters").Ratio(1),
            new Layout("File").Ratio(1)
        );
        
        layout["Body"]["Row1"]["Parameters"].Update(CreateParametersPanel());
        layout["Body"]["Row1"]["File"].Update(CreateFilePanel());

        // Linha 2: API | Progresso (50% cada coluna)
        layout["Body"]["Row2"].SplitColumns(
            new Layout("API").Ratio(1),
            new Layout("Progress").Ratio(1)
        );
        
        layout["Body"]["Row2"]["API"].Update(CreateApiPanel());
        layout["Body"]["Row2"]["Progress"].Update(CreateProgressPanel(metrics));

        // Footer com logs
        layout["Footer"].Update(CreateLogsPanel());


        return layout;
    }

    /// <summary>
    ///     Cria o painel de cabeçalho com informações da aplicação
    /// </summary>
    private Panel CreateHeaderPanel()
    {
        var grid = new Grid()
            .AddColumn(new GridColumn().Centered());

        grid.AddRow(new Markup($"[bold cyan1]{_appName}[/]").Centered());
        grid.AddRow(new Markup($"[grey]{_appDescription}[/]").Centered());
        grid.AddRow(new Markup($"[grey]Versão {_appVersion}[/]").Centered());

        return new Panel(grid)
            .BorderColor(Color.Cyan1)
            .Padding(0, 0)
            .Expand();
    }

    /// <summary>
    ///     Cria o painel de parâmetros (config + CLI args)
    /// </summary>
    private Panel CreateParametersPanel()
    {
        var grid = new Grid()
            .AddColumn(new GridColumn().NoWrap().PadRight(2))
            .AddColumn();

        // Parâmetros de arquivo
        grid.AddRow(new Markup("[underline cyan1]Arquivo:[/]"), new Markup(""));
        grid.AddRow("  CSV Delimiter:", $"[yellow]'{_context.Configuration.File.CsvDelimiter}'[/]");
        grid.AddRow("  Batch Lines:", $"[yellow]{_context.Configuration.File.BatchLines:N0}[/]");
        grid.AddRow("  Start Line:", $"[yellow]{_context.Configuration.File.StartLine:N0}[/]");
        
        if (_context.Configuration.File.MaxLines.HasValue)
            grid.AddRow("  Max Lines:", $"[yellow]{_context.Configuration.File.MaxLines.Value:N0}[/]");
        
        grid.AddEmptyRow();
        
        // Parâmetros de checkpoint
        grid.AddRow(new Markup("[underline cyan1]Checkpoint:[/]"), new Markup(""));
        grid.AddRow("  Directory:", $"[yellow]{_context.Configuration.File.CheckpointDirectory}[/]");
        grid.AddRow("  Execution ID:", $"[yellow]{_context.ExecutionPaths.ExecutionId}[/]");
        
        // Verificar se há checkpoint existente
        var hasCheckpoint = !string.IsNullOrEmpty(_context.ExecutionPaths.CheckpointPath) && 
                           File.Exists(_context.ExecutionPaths.CheckpointPath);
        grid.AddRow("  Status:", hasCheckpoint 
            ? "[green]Continuando execução[/]" 
            : "[cyan1]Nova execução[/]");
        
        grid.AddEmptyRow();
        
        // Parâmetros de log
        grid.AddRow(new Markup("[underline cyan1]Logs:[/]"), new Markup(""));
        grid.AddRow("  Directory:", $"[yellow]{_context.Configuration.File.LogDirectory}[/]");
        grid.AddRow("  Verbose:", _context.IsVerbose ? "[green]Sim[/]" : "[grey]Não[/]");
        
        if (_context.IsDryRun)
        {
            grid.AddEmptyRow();
            grid.AddRow(new Markup("[yellow]⚠  Modo:[/]"), new Markup("[yellow]DRY RUN[/]"));
        }

        return new Panel(grid)
            .Header("[bold cyan1]⚙️ PARÂMETROS[/]", Justify.Center)
            .BorderColor(Color.Cyan1)
            .Padding(0, 0)
            .Expand();
    }

    /// <summary>
    ///     Cria o painel de informações do arquivo
    /// </summary>
    private Panel CreateFilePanel()
    {
        var grid = new Grid()
            .AddColumn(new GridColumn().NoWrap().PadRight(2))
            .AddColumn();

        var filePath = _context.Configuration.File.InputPath;
        var fileName = Path.GetFileName(filePath);
        var fileDir = Path.GetDirectoryName(filePath);
        
        grid.AddRow("[cyan1]Nome:[/]", $"[yellow]{fileName}[/]");
        grid.AddRow("[cyan1]Diretório:[/]", $"[grey]{ShortenPath(fileDir ?? "")}[/]");
        
        // Obter tamanho do arquivo
        if (File.Exists(filePath))
        {
            var fileInfo = new FileInfo(filePath);
            grid.AddRow("[cyan1]Tamanho:[/]", $"[yellow]{FormatFileSize(fileInfo.Length)}[/]");
        }
        
        // Obter total de linhas do MetricsService
        var metrics = _metricsService.GetMetrics();
        grid.AddRow("[cyan1]Total Linhas:[/]", $"[yellow]{metrics.TotalLines:N0}[/]");

        // Obter informações de filtros das colunas configuradas
        var filtersCount = _context.Configuration.File.Columns.Count(c => c.Filter != null);
        if (filtersCount > 0)
        {
            grid.AddEmptyRow();
            grid.AddRow("[cyan1]Filtros:[/]", $"[blue]{filtersCount} coluna(s) com filtros[/]");
        }

        // Obter linhas filtradas do MetricsService
        if (metrics.FilteredLines > 0)
        {
            grid.AddRow("[cyan1]Filtradas:[/]", $"[grey]{metrics.FilteredLines:N0} linhas[/]");
        }

        return new Panel(grid)
            .Header("[bold cyan1]📄 ARQUIVO[/]", Justify.Center)
            .BorderColor(Color.Blue)
            .Padding(0, 0)
            .Expand();
    }

    /// <summary>
    ///     Cria o painel de informações da API
    /// </summary>
    private Panel CreateApiPanel()
    {
        var grid = new Grid()
            .AddColumn(new GridColumn().NoWrap().PadRight(2))
            .AddColumn();

        grid.AddRow("[cyan1]Endpoint:[/]", $"[yellow]{ShortenUrl(_context.ActiveEndpoint.EndpointUrl)}[/]");
        grid.AddRow("[cyan1]Método:[/]", $"[green]{_context.ActiveEndpoint.Method}[/]");
        grid.AddRow("[cyan1]Timeout:[/]", $"[yellow]{_context.ActiveEndpoint.RequestTimeout}s[/]");
        grid.AddRow("[cyan1]Retry:[/]", $"[yellow]{_context.ActiveEndpoint.RetryAttempts}x[/]");

        if (_context.ActiveEndpoint.Headers.Count > 0)
        {
            grid.AddEmptyRow();
            grid.AddRow(new Markup("[underline cyan1]Headers:[/]"), new Markup(""));
            
            foreach (var header in _context.ActiveEndpoint.Headers.Take(3))
            {
                var value = header.Value.Length > 30 ? header.Value.Substring(0, 27) + "..." : header.Value;
                grid.AddRow($"  {header.Key}:", $"[grey]{value}[/]");
            }
            
            if (_context.ActiveEndpoint.Headers.Count > 3)
                grid.AddRow("", $"[grey]... +{_context.ActiveEndpoint.Headers.Count - 3} headers[/]");
        }

        return new Panel(grid)
            .Header("[bold cyan1]🌐 API[/]", Justify.Center)
            .BorderColor(Color.Green)
            .Padding(0, 0)
            .Expand();
    }

    /// <summary>
    ///     Cria o painel de logs (footer)
    /// </summary>
    private Panel CreateLogsPanel()
    {
        var grid = new Grid()
            .AddColumn();

        if (_logMessages.Count == 0)
        {
            grid.AddRow(new Markup("[grey]Aguardando eventos...[/]"));
        }
        else
        {
            foreach (var log in _logMessages)
            {
                grid.AddRow(new Markup(log));
            }
        }

        return new Panel(grid)
            .Header("[bold cyan1]📋 LOGS DE EXECUÇÃO[/]", Justify.Center)
            .BorderColor(Color.Orange1)
            .Padding(0, 0)
            .Expand();
    }

    /// <summary>
    ///     Cria o painel de progresso
    /// </summary>
    private Panel CreateProgressPanel(ProcessingMetrics metrics)
    {
        var grid = new Grid()
            .AddColumn();

        // Barra de progresso visual
        var percentage = metrics.ProgressPercentage;
        var barWidth = 35;
        var filledWidth = (int)(barWidth * percentage / 100);
        var emptyWidth = barWidth - filledWidth;
        var bar = $"[green]{"█".PadRight(filledWidth, '█')}[/][grey]{"░".PadRight(emptyWidth, '░')}[/]";

        grid.AddRow(new Markup($"{bar} [yellow]{percentage:F1}%[/]").Centered());
        grid.AddEmptyRow();

        // Estatísticas de processamento
        var statsGrid = new Grid()
            .AddColumn(new GridColumn().NoWrap().PadRight(2))
            .AddColumn();

        statsGrid.AddRow("[cyan1]Processadas:[/]",
            $"[yellow]{metrics.ProcessedLines:N0}[/] / [grey]{metrics.TotalLines:N0}[/]");
        statsGrid.AddRow("[green]✓ Sucessos:[/]",
            $"[green]{metrics.SuccessCount:N0}[/] [grey]({metrics.SuccessRate:F1}%)[/]");
        statsGrid.AddRow("[red]✗ Erros:[/]", $"[red]{metrics.ErrorCount:N0}[/] [grey]({metrics.ErrorRate:F1}%)[/]");

        if (metrics.ValidationErrors > 0)
            statsGrid.AddRow("[yellow]⚠ Validação:[/]", $"[yellow]{metrics.ValidationErrors:N0}[/]");

        if (metrics.SkippedLines > 0)
            statsGrid.AddRow("[grey]⏭  Puladas:[/]", $"[grey]{metrics.SkippedLines:N0}[/]");

        grid.AddRow(statsGrid);
        grid.AddEmptyRow();

        // Tempo e velocidade
        var timeGrid = new Grid()
            .AddColumn(new GridColumn().NoWrap().PadRight(2))
            .AddColumn();

        var elapsed = metrics.ElapsedTime;
        timeGrid.AddRow("[cyan1]⏱  Decorrido:[/]", $"[yellow]{FormatTimeSpan(elapsed)}[/]");

        if (metrics.ProcessedLines > 0 && metrics.ProcessedLines < metrics.TotalLines)
            timeGrid.AddRow("[cyan1]⏳ Estimado:[/]", $"[yellow]{FormatTimeSpan(metrics.EstimatedTimeRemaining)}[/]");

        timeGrid.AddRow("[cyan1]🚀 Velocidade:[/]", $"[green]{metrics.LinesPerSecond:F1}[/] [grey]linhas/s[/]");

        grid.AddRow(timeGrid);

        // Performance HTTP compacta
        if (metrics.ProcessedLines > 0)
        {
            grid.AddEmptyRow();
            var httpGrid = new Grid()
                .AddColumn(new GridColumn().NoWrap().PadRight(2))
                .AddColumn();

            httpGrid.AddRow("[cyan1]Resp. Média:[/]", $"[yellow]{metrics.AverageResponseTimeMs:F0} ms[/]");

            if (metrics.MinResponseTimeMs != long.MaxValue)
                httpGrid.AddRow("[cyan1]Min / Max:[/]",
                    $"[green]{metrics.MinResponseTimeMs}[/] / [red]{metrics.MaxResponseTimeMs}[/] ms");

            if (metrics.TotalRetries > 0)
                httpGrid.AddRow("[cyan1]Retries:[/]", $"[yellow]{metrics.TotalRetries}[/]");

            grid.AddRow(httpGrid);
        }

        // Status codes resumido
        if (metrics.HttpStatusCodes.Count > 0)
        {
            grid.AddEmptyRow();
            var statusList = new List<string>();
            foreach (var kvp in metrics.HttpStatusCodes.OrderBy(x => x.Key).Take(4))
            {
                var color = GetStatusColor(kvp.Key);
                statusList.Add($"[{color}]{kvp.Key}[/]:[{color}]{kvp.Value}[/]");
            }
            
            var statusLine = string.Join(" ", statusList);
            if (metrics.HttpStatusCodes.Count > 4)
                statusLine += $" [grey]+{metrics.HttpStatusCodes.Count - 4}[/]";
                
            grid.AddRow(new Markup($"[grey]HTTP:[/] {statusLine}"));
        }

        return new Panel(grid)
            .Header("[bold cyan1]📊 PROGRESSO[/]", Justify.Center)
            .BorderColor(Color.Purple)
            .Padding(0, 0)
            .Expand();
    }

    /// <summary>
    ///     Formata tamanho de arquivo para exibição
    /// </summary>
    private string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        var order = 0;

        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }

        return $"{len:F2} {sizes[order]}";
    }

    /// <summary>
    ///     Formata TimeSpan para exibição
    /// </summary>
    private string FormatTimeSpan(TimeSpan ts)
    {
        if (ts.TotalSeconds < 60)
            return $"{ts.TotalSeconds:F0}s";
        if (ts.TotalMinutes < 60)
            return $"{(int)ts.TotalMinutes}min {ts.Seconds}s";
        return $"{(int)ts.TotalHours}h {ts.Minutes}min";
    }

    /// <summary>
    ///     Encurta URL para exibição
    /// </summary>
    private string ShortenUrl(string url)
    {
        if (string.IsNullOrEmpty(url)) return "";
        return url.Length > 50 ? url.Substring(0, 47) + "..." : url;
    }

    /// <summary>
    ///     Encurta path para exibição
    /// </summary>
    private string ShortenPath(string path)
    {
        if (string.IsNullOrEmpty(path)) return "";
        return path.Length > 35 ? "..." + path.Substring(path.Length - 32) : path;
    }

    /// <summary>
    ///     Retorna cor apropriada para código HTTP
    /// </summary>
    private string GetStatusColor(int statusCode)
    {
        return statusCode switch
        {
            >= 200 and < 300 => "green",
            >= 300 and < 400 => "blue",
            >= 400 and < 500 => "yellow",
            >= 500 => "red",
            _ => "grey"
        };
    }
}