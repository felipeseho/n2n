using System.Reflection;
using n2n.Models;
using Spectre.Console;

namespace n2n.Services;

public class DashboardService(
    MetricsService metricsService, 
    AppExecutionContext context)
{
    private readonly List<string> _logMessages = new();
    private readonly int _maxLogMessages = 10;
    
    // Application info
    // Get assembly title and version dynamically if needed
    
    private readonly string _appName = Assembly.GetExecutingAssembly().GetName().Name!;
    private readonly string _appVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString()!;
    private readonly string _appDescription = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyDescriptionAttribute>()?.Description!;
    
    // State
    private bool _isRunning;
    
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
        
        var levelIcon = level.ToUpper() switch
        {
            "ERROR" => "❌",
            "WARNING" => "⚠️",
            "SUCCESS" => "✅",
            "INFO" => "ℹ️ ",
            _ => "."
        };
        
        var formattedMessage = $"[grey]{timestamp}[/] [{color}]{levelIcon}[/] {message}";
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

        try
        {
            // Validar dimensões mínimas do terminal
            if (!ValidateTerminalSize())
            {
                AnsiConsole.MarkupLine("[yellow]⚠️  Terminal muito pequeno para dashboard visual. Usando modo texto.[/]");
                AnsiConsole.MarkupLine("[grey]Para dashboard completo, use terminal de no mínimo 80x25[/]");
                AnsiConsole.WriteLine();
                
                // Modo texto simples para terminais pequenos
                while (_isRunning && !cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        ShowSimpleSummary();
                        await Task.Delay(2000, cancellationToken); // Atualizar a cada 2 segundos
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch
                    {
                        // Ignorar erros e continuar
                    }
                }
                return;
            }

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
                    try
                    {
                        var layout = CreateLayout();
                        ctx.UpdateTarget(layout);
                    }
                    catch (ArgumentOutOfRangeException)
                    {
                        // Terminal redimensionado durante execução - ignorar este frame
                    }
                    catch (Exception ex)
                    {
                        // Log erro mas continua
                        _logMessages.Add($"[red]Erro ao atualizar dashboard: {ex.Message}[/]");
                    }
                    
                    await Task.Delay(500, cancellationToken); // Atualizar a cada 500ms
                }
                });
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Erro ao iniciar dashboard: {ex.Message}[/]");
            AnsiConsole.MarkupLine("[grey]Continuando processamento sem dashboard...[/]");
            
            // Continuar sem dashboard
            while (_isRunning && !cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(1000, cancellationToken);
            }
        }
    }

    /// <summary>
    ///     Valida se o terminal tem dimensões mínimas adequadas
    /// </summary>
    private bool ValidateTerminalSize()
    {
        try
        {
            var width = Console.WindowWidth;
            var height = Console.WindowHeight;
            
            // Mínimo recomendado: 80x25 (padrão de terminal)
            return width >= 80 && height >= 25;
        }
        catch
        {
            // Se não conseguir obter dimensões, assume que não pode usar dashboard
            return false;
        }
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
        try
        {
            // Validar dimensões mínimas do terminal
            if (!ValidateTerminalSize())
            {
                // Exibir resumo simples ao invés do dashboard completo
                ShowSimpleSummary();
                return;
            }

            AnsiConsole.Clear();
            AnsiConsole.Write(CreateLayout());
        }
        catch (ArgumentOutOfRangeException)
        {
            // Fallback para modo simples
            ShowSimpleSummary();
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Erro ao atualizar dashboard: {ex.Message}[/]");
            ShowSimpleSummary();
        }
    }
    
    /// <summary>
    ///     Exibe resumo simples para terminais pequenos
    /// </summary>
    private void ShowSimpleSummary()
    {
        var metrics = metricsService.GetMetrics();
        
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[cyan1]═══ Processamento CSV ═══[/]");
        AnsiConsole.MarkupLine($"[grey]Progresso:[/] {metrics.ProgressPercentage:F1}% ([yellow]{metrics.ProcessedLines:N0}[/] / [grey]{metrics.TotalLines:N0}[/])");
        AnsiConsole.MarkupLine($"[green]✅ Sucessos:[/] {metrics.SuccessCount:N0} ({metrics.SuccessRate:F1}%)");
        AnsiConsole.MarkupLine($"[red]❌ Erros:[/] {metrics.ErrorCount:N0} ({metrics.ErrorRate:F1}%)");
        AnsiConsole.MarkupLine($"[cyan1]⏱️  Tempo:[/] {FormatTimeSpan(metrics.ElapsedTime)} | [green]🚀 {metrics.LinesPerSecond:F1}[/] linhas/s");
        
        if (_logMessages.Count > 0)
        {
            AnsiConsole.MarkupLine($"[grey]Último log:[/] {_logMessages.Last()}");
        }
        
        AnsiConsole.WriteLine();
    }

    /// <summary>
    ///     Cria o layout do dashboard
    /// </summary>
    private Layout CreateLayout()
    {
        var metrics = metricsService.GetMetrics();

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
        
        layout["Body"]["Row2"]["API"].Update(CreateEndpointPanel());
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
            .AddColumn(new GridColumn().LeftAligned());

        grid.AddRow(new Markup($"[bold cyan1]{_appName}[/]"));
        grid.AddRow(new Markup($"[grey]{_appDescription}[/]"));
        grid.AddRow(new Markup($"[grey]Versão {_appVersion}[/]"));

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
        grid.AddRow("  Delimitador CSV:", $"[yellow]'{context.Configuration.File.CsvDelimiter}'[/]");
        grid.AddRow("  Linhas por Lote:", $"[yellow]{context.Configuration.File.BatchLines:N0}[/]");
        grid.AddRow("  Linha Inicial:", $"[yellow]{context.Configuration.File.StartLine:N0}[/]");
        
        if (context.Configuration.File.MaxLines.HasValue)
            grid.AddRow("  Linhas Máximas:", $"[yellow]{context.Configuration.File.MaxLines.Value:N0}[/]");
        
        grid.AddEmptyRow();
        
        // Parâmetros de checkpoint
        grid.AddRow(new Markup("[underline cyan1]Checkpoint:[/]"), new Markup(""));
        grid.AddRow("  Diretório:", $"[yellow]{context.Configuration.File.CheckpointDirectory}[/]");
        grid.AddRow("  ID da Execução:", $"[yellow]{context.ExecutionPaths.ExecutionId}[/]");
        
        // Verificar se há checkpoint existente
        var hasCheckpoint = !string.IsNullOrEmpty(context.ExecutionPaths.CheckpointPath) && 
                           File.Exists(context.ExecutionPaths.CheckpointPath);
        grid.AddRow("  Status:", hasCheckpoint 
            ? "[green]Continuando execução[/]" 
            : "[cyan1]Nova execução[/]");
        
        grid.AddEmptyRow();
        
        // Parâmetros de log
        grid.AddRow(new Markup("[underline cyan1]Logs:[/]"), new Markup(""));
        grid.AddRow("  Diretório:", $"[yellow]{context.Configuration.File.LogDirectory}[/]");
        grid.AddRow("  Detalhado:", context.IsVerbose ? "[green]Sim[/]" : "[grey]Não[/]");
        
        if (context.IsDryRun)
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

        var filePath = context.Configuration.File.InputPath;
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
        var metrics = metricsService.GetMetrics();
        grid.AddRow("[cyan1]Total Linhas:[/]", $"[yellow]{metrics.TotalLines:N0}[/]");
        
        // Obter linhas filtradas do MetricsService
        if (metrics.FilteredLines > 0)
        {
            grid.AddRow("[cyan1]Filtradas:[/]", $"[grey]{metrics.FilteredLines:N0} linhas[/]");
        }

        // Obter informações de filtros das colunas configuradas
        var columnsWithFilters = context.Configuration.File.Columns.Where(c => c.Filters != null && c.Filters.Count > 0).ToList();
        var totalFilters = columnsWithFilters.Sum(c => c.Filters?.Count ?? 0);
        
        if (totalFilters > 0)
        {
            grid.AddEmptyRow();
            grid.AddRow("[cyan1]Filtros:[/]", $"[blue]{totalFilters} filtro(s) em {columnsWithFilters.Count} coluna(s)[/]");
            grid.AddEmptyRow();
            
            var displayedFilters = 0;
            foreach (var column in columnsWithFilters)
            {
                if (column.Filters == null) continue;
                
                foreach (var filter in column.Filters)
                {
                    if (displayedFilters >= 5) break; // Limitar a 5 filtros na exibição
                    grid.AddRow($"  🔍 {column.Column} [yellow]{filter.Operator}[/]", $"[grey]{filter.Value}[/]");
                    displayedFilters++;
                }
                
                if (displayedFilters >= 5) break;
            }
            
            if (totalFilters > 5)
            {
                grid.AddRow($"  [dim]... e mais {totalFilters - 5} filtro(s)[/]", "");
            }
        }
        
        return new Panel(grid)
            .Header("[bold cyan1]📄 ARQUIVO[/]", Justify.Center)
            .BorderColor(Color.Blue)
            .Padding(0, 0)
            .Expand();
    }

    /// <summary>
    ///     Cria o painel de informações do Endpoint
    /// </summary>
    private Panel CreateEndpointPanel()
    {
        var grid = new Grid()
            .AddColumn(new GridColumn().NoWrap().PadRight(2))
            .AddColumn();

        var endpoint = context.ActiveEndpoint;

        grid.AddRow("[cyan1]Url:[/]", $"[yellow]{ShortenUrl(endpoint.EndpointUrl)}[/]");
        grid.AddRow("[cyan1]Método:[/]", $"[green]{endpoint.Method}[/]");
        grid.AddRow("[cyan1]Timeout:[/]", $"[yellow]{endpoint.RequestTimeout}s[/]");
        grid.AddRow("[cyan1]Tentativas:[/]", $"[yellow]{endpoint.RetryAttempts}x[/]");
        grid.AddRow("[cyan1]Atraso entre Tentativas:[/]", $"[yellow]{endpoint.RetryDelaySeconds}s[/]");

        if (endpoint.Headers.Count > 0)
        {
            grid.AddEmptyRow();
            grid.AddRow(new Markup("[underline cyan1]Headers:[/]"), new Markup(""));
            
            foreach (var header in endpoint.Headers.Take(3))
            {
                var value = header.Value.Length > 30 ? header.Value.Substring(0, 27) + "..." : header.Value;
                grid.AddRow($"  {header.Key}:", $"[grey]{value}[/]");
            }
            
            if (endpoint.Headers.Count > 3)
                grid.AddRow("", $"[grey]... +{endpoint.Headers.Count - 3} headers[/]");
        }

        return new Panel(grid)
            .Header("[bold cyan1]🌐 ENDPOINT[/]", Justify.Center)
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
        var filledWidth = Math.Max(0, Math.Min(barWidth, (int)(barWidth * percentage / 100)));
        var emptyWidth = Math.Max(0, barWidth - filledWidth);
        
        var filledBar = filledWidth > 0 ? new string('█', filledWidth) : "";
        var emptyBar = emptyWidth > 0 ? new string('░', emptyWidth) : "";
        var bar = $"[green]{filledBar}[/][grey]{emptyBar}[/]";

        grid.AddRow(new Markup($"{bar} [yellow]{percentage:F1}%[/]").Centered());
        grid.AddEmptyRow();

        // Estatísticas de processamento
        var statsGrid = new Grid()
            .AddColumn(new GridColumn().NoWrap().PadRight(2))
            .AddColumn();

        statsGrid.AddRow("[cyan1]Processadas:[/]",
            $"[yellow]{metrics.ProcessedLines:N0}[/] / [grey]{metrics.TotalLines:N0}[/]");
        statsGrid.AddRow("[green]✅ Sucessos:[/]",
            $"[green]{metrics.SuccessCount:N0}[/] [grey]({metrics.SuccessRate:F1}%)[/]");
        statsGrid.AddRow("[red]❌ Erros:[/]", $"[red]{metrics.ErrorCount:N0}[/] [grey]({metrics.ErrorRate:F1}%)[/]");

        if (metrics.ValidationErrors > 0)
            statsGrid.AddRow("[yellow]⚠️ Validação:[/]", $"[yellow]{metrics.ValidationErrors:N0}[/]");

        if (metrics.SkippedLines > 0)
            statsGrid.AddRow("[grey]⏭️  Puladas:[/]", $"[grey]{metrics.SkippedLines:N0}[/]");

        grid.AddRow(statsGrid);
        grid.AddEmptyRow();

        // Tempo e velocidade
        var timeGrid = new Grid()
            .AddColumn(new GridColumn().NoWrap().PadRight(2))
            .AddColumn();

        var elapsed = metrics.ElapsedTime;
        timeGrid.AddRow("[cyan1]⏱️  Decorrido:[/]", $"[yellow]{FormatTimeSpan(elapsed)}[/]");

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
                httpGrid.AddRow("[cyan1]Mín / Máx:[/]",
                    $"[green]{metrics.MinResponseTimeMs}[/] / [red]{metrics.MaxResponseTimeMs}[/] ms");

            if (metrics.TotalRetries > 0)
                httpGrid.AddRow("[cyan1]Tentativas:[/]", $"[yellow]{metrics.TotalRetries}[/]");

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