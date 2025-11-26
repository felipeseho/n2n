using System.Reflection;
using n2n.Core;
using n2n.Models;
using Spectre.Console;

namespace n2n.Services;

/// <summary>
///     Serviço de dashboard interativo usando Spectre.Console Layout
/// </summary>
public class PipelineDashboardService
{
    private readonly DashboardViewModel _viewModel;
    private bool _isRunning;

    public PipelineDashboardService(PipelineConfiguration configuration)
    {
        var appName = Assembly.GetExecutingAssembly().GetName().Name!;
        var appVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString()!;

        _viewModel = new DashboardViewModel
        {
            ApplicationName = appName,
            ApplicationVersion = appVersion,
            ExecutionName = configuration.Name,
            ExecutionDescription = configuration.Description ?? string.Empty,
            Configuration = configuration
        };
    }

    public void SetSource(IDataSource source) => _viewModel.Source = source;
    public void SetDestination(IDataDestination destination) => _viewModel.Destination = destination;
    public void SetEstimatedTotal(long? total) => _viewModel.EstimatedTotal = total;
    public void SetExecutionId(string executionId) => _viewModel.ExecutionId = executionId;

    public void AddLogMessage(string message, string level = "INFO")
    {
        var logLevel = level.ToUpper() switch
        {
            "ERROR" => LogLevel.Error,
            "WARNING" => LogLevel.Warning,
            "SUCCESS" => LogLevel.Success,
            "INFO" => LogLevel.Info,
            _ => LogLevel.Debug
        };

        _viewModel.AddGlobalLog(message, logLevel);
    }

    public void AddSourceLog(string message, string level = "INFO")
    {
        var logLevel = level.ToUpper() switch
        {
            "ERROR" => LogLevel.Error,
            "WARNING" => LogLevel.Warning,
            "SUCCESS" => LogLevel.Success,
            "INFO" => LogLevel.Info,
            _ => LogLevel.Debug
        };

        _viewModel.AddSourceLog(message, logLevel);
    }

    public void AddDestinationLog(string message, string level = "INFO")
    {
        var logLevel = level.ToUpper() switch
        {
            "ERROR" => LogLevel.Error,
            "WARNING" => LogLevel.Warning,
            "SUCCESS" => LogLevel.Success,
            "INFO" => LogLevel.Info,
            _ => LogLevel.Debug
        };

        _viewModel.AddDestinationLog(message, logLevel);
    }

    public void SetStatus(DashboardStatus status)
    {
        _viewModel.Status = status;
    }

    public async Task StartLiveDashboard(CancellationToken cancellationToken)
    {
        _isRunning = true;

        try
        {
            if (!ValidateTerminalSize())
            {
                AnsiConsole.MarkupLine(
                    "[yellow]⚠️  Terminal pequeno. Use terminal de no mínimo 100x30 para dashboard completo.[/]");
                AnsiConsole.WriteLine();

                while (_isRunning && !cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        ShowSimpleSummary();
                        await Task.Delay(2000, cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }

                return;
            }

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
                            ctx.UpdateTarget(CreateLayout());
                        }
                        catch (ArgumentOutOfRangeException)
                        {
                            // Terminal redimensionado
                        }

                        await Task.Delay(500, cancellationToken);
                    }
                });
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Erro ao iniciar dashboard: {ex.Message}[/]");

            while (_isRunning && !cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(1000, cancellationToken);
            }
        }
    }

    public void Stop() => _isRunning = false;

    public void UpdateOnce()
    {
        try
        {
            if (!ValidateTerminalSize())
            {
                ShowSimpleSummary();
                return;
            }

            AnsiConsole.Clear();
            AnsiConsole.Write(CreateLayout());
        }
        catch
        {
            ShowSimpleSummary();
        }
    }

    private bool ValidateTerminalSize()
    {
        try
        {
            return Console.WindowWidth >= 100 && Console.WindowHeight >= 30;
        }
        catch
        {
            return false;
        }
    }

    private void ShowSimpleSummary()
    {
        var sourceMetrics = _viewModel.Source?.GetMetrics();
        var destMetrics = _viewModel.Destination?.GetMetrics();

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[cyan1]═══ Pipeline: {_viewModel.ExecutionName} ═══[/]");

        if (sourceMetrics != null)
        {
            AnsiConsole.MarkupLine(
                $"[cyan1]Origem ({_viewModel.Configuration.Source.Type}):[/] {sourceMetrics.TotalRecordsRead:N0} lidos");
        }

        if (destMetrics != null)
        {
            AnsiConsole.MarkupLine(
                $"[cyan1]Destino ({_viewModel.Configuration.Destination.Type}):[/] {destMetrics.TotalRecordsWritten:N0} escritos");
            AnsiConsole.MarkupLine(
                $"[green]✅ Sucessos:[/] {destMetrics.SuccessCount:N0} | [red]❌ Erros:[/] {destMetrics.ErrorCount:N0}");
        }

        var globalLogs = _viewModel.GetGlobalLogs();
        if (globalLogs.Length > 0)
        {
            AnsiConsole.MarkupLine($"[grey]Último log:[/] {globalLogs.Last()}");
        }

        AnsiConsole.WriteLine();
    }

    private Layout CreateLayout()
    {
        // Estrutura: Header | Body (Source | Destination) | Footer
        var layout = new Layout("Root")
            .SplitRows(
                new Layout("Header").Size(7),
                new Layout("Body"),
                new Layout("Footer").Size(12)
            );

        // Header
        layout["Header"].Update(CreateHeaderPanel());

        // Body dividido verticalmente: Esquerda (Source) | Direita (Destination)
        layout["Body"].SplitColumns(
            new Layout("Source"),
            new Layout("Dest")
        );

        layout["Body"]["Source"].Update(CreateSourcePanel());
        layout["Body"]["Dest"].Update(CreateDestinationPanel());

        // Footer
        layout["Footer"].Update(CreateFooterPanel());

        return layout;
    }

    private Panel CreateHeaderPanel()
    {
        var statusIcon = _viewModel.Status switch
        {
            DashboardStatus.Running => "[yellow]●[/]",
            DashboardStatus.Completed => "[green]✓[/]",
            DashboardStatus.Error => "[red]✗[/]",
            DashboardStatus.Paused => "[grey]⏸[/]",
            _ => "[grey]○[/]"
        };

        var statusText = _viewModel.Status switch
        {
            DashboardStatus.Running => "[yellow]Em Andamento[/]",
            DashboardStatus.Completed => "[green]Concluído[/]",
            DashboardStatus.Error => "[red]Erro[/]",
            DashboardStatus.Paused => "[grey]Pausado[/]",
            _ => "[grey]Desconhecido[/]"
        };

        var metadataTable = new Table()
            .Border(TableBorder.None)
            .HideHeaders()
            .AddColumn(new TableColumn("Key").NoWrap())
            .AddColumn(new TableColumn("Value"));

        metadataTable.AddRow(
            "[cyan1]Aplicação:[/]",
            $"[white]{_viewModel.ApplicationName}[/] [grey](v{_viewModel.ApplicationVersion})[/]"
        );

        metadataTable.AddRow(
            "[cyan1]Execução:[/]",
            $"[yellow]{_viewModel.ExecutionName}[/]" +
            (!string.IsNullOrWhiteSpace(_viewModel.ExecutionDescription)
                ? $" [grey]- {_viewModel.ExecutionDescription}[/]"
                : "")
        );

        if (!string.IsNullOrWhiteSpace(_viewModel.ExecutionId))
        {
            metadataTable.AddRow(
                "[cyan1]ID:[/]",
                $"[grey]{_viewModel.ExecutionId}[/]"
            );
        }

        metadataTable.AddRow(
            "[cyan1]Status:[/]",
            $"{statusIcon} {statusText}"
        );

        return new Panel(metadataTable)
            .Header("[bold cyan1]═══ PIPELINE DATA INTEGRATION ═══[/]", Justify.Center)
            .BorderColor(Color.Cyan1)
            .Padding(1, 0)
            .Expand();
    }

    private Panel CreateSourcePanel()
    {
        var mainGrid = new Grid()
            .AddColumn(new GridColumn());

        // === CONFIGURAÇÕES ===
        var configTable = new Table()
            .Border(TableBorder.None)
            .HideHeaders()
            .AddColumn(new TableColumn("Key").NoWrap())
            .AddColumn(new TableColumn("Value"));

        configTable.AddRow("[cyan1]Tipo:[/]", $"[yellow]{_viewModel.Configuration.Source.Type}[/]");

        foreach (var setting in _viewModel.Configuration.Source.Settings.Take(3))
        {
            var value = setting.Value?.ToString() ?? "null";
            configTable.AddRow($"  [grey]{setting.Key}:[/]", $"[white]{value}[/]");
        }

        mainGrid.AddRow(new Panel(configTable)
            .Header("[underline cyan1]⚙️  Configurações[/]", Justify.Left)
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Blue)
            .Padding(0, 0));

        mainGrid.AddEmptyRow();

        // === PERFORMANCE ===
        var sourceMetrics = _viewModel.Source?.GetMetrics();
        var perfTable = new Table()
            .Border(TableBorder.None)
            .HideHeaders()
            .AddColumn(new TableColumn("Key").NoWrap())
            .AddColumn(new TableColumn("Value"));

        if (sourceMetrics != null)
        {
            perfTable.AddRow("[cyan1]Registros Lidos:[/]", $"[yellow]{sourceMetrics.TotalRecordsRead:N0}[/]");
            perfTable.AddRow("[cyan1]Filtrados:[/]", $"[grey]{sourceMetrics.FilteredRecords:N0}[/]");
            perfTable.AddRow("[cyan1]Tempo Decorrido:[/]", $"[yellow]{FormatTimeSpan(sourceMetrics.ElapsedTime)}[/]");
            perfTable.AddRow("[cyan1]Taxa de Leitura:[/]", $"[green]{sourceMetrics.RecordsPerSecond:F1}[/] rec/s");

            if (sourceMetrics.BytesRead > 0)
            {
                var mb = sourceMetrics.BytesRead / 1024.0 / 1024.0;
                perfTable.AddRow("[cyan1]Bytes Lidos:[/]", $"[grey]{mb:F2}[/] MB");
            }
        }
        else
        {
            perfTable.AddRow("[grey]Aguardando dados...[/]", "");
        }

        mainGrid.AddRow(new Panel(perfTable)
            .Header("[underline cyan1]📊 Performance[/]", Justify.Left)
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Blue)
            .Padding(0, 0));

        mainGrid.AddEmptyRow();

        // === PROGRESSO ===
        var progressGrid = new Grid().AddColumn();

        if (sourceMetrics != null && _viewModel.EstimatedTotal.HasValue)
        {
            var read = sourceMetrics.TotalRecordsRead;
            var total = _viewModel.EstimatedTotal.Value;
            var percentage = total > 0 ? (read * 100.0 / total) : 0;

            var barWidth = 30;
            var filledWidth = Math.Max(0, Math.Min(barWidth, (int)(barWidth * percentage / 100)));
            var emptyWidth = Math.Max(0, barWidth - filledWidth);

            var bar = $"[blue]{new string('█', filledWidth)}[/][grey]{new string('░', emptyWidth)}[/]";
            progressGrid.AddRow(new Markup($"{bar} [yellow]{percentage:F1}%[/]"));
            progressGrid.AddRow(new Markup($"[grey]{read:N0} / {total:N0}[/]"));
            
            // Tempo estimado
            if (sourceMetrics.RecordsPerSecond > 0 && read < total)
            {
                var remaining = total - read;
                var secondsRemaining = remaining / sourceMetrics.RecordsPerSecond;
                var etaTime = FormatTimeSpan(TimeSpan.FromSeconds(secondsRemaining));
                progressGrid.AddRow(new Markup($"[grey]⏱️  ETA: {etaTime}[/]"));
            }
        }
        else if (sourceMetrics != null)
        {
            progressGrid.AddRow(new Markup($"[yellow]{sourceMetrics.TotalRecordsRead:N0}[/] registros lidos"));
        }
        else
        {
            progressGrid.AddRow(new Markup("[grey]Aguardando início...[/]"));
        }

        mainGrid.AddRow(new Panel(progressGrid)
            .Header("[underline cyan1]📈 Progresso[/]", Justify.Left)
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Blue)
            .Padding(0, 0));

        mainGrid.AddEmptyRow();

        // === FILTROS ===
        if (_viewModel.Configuration.Filters != null && _viewModel.Configuration.Filters.Count > 0)
        {
            var filtersGrid = new Grid().AddColumn();
            var validFilters = 0;
            
            foreach (var filter in _viewModel.Configuration.Filters.Take(5))
            {
                if (filter == null) continue;
                
                var field = !string.IsNullOrWhiteSpace(filter.Field) ? filter.Field : "(vazio)";
                var op = !string.IsNullOrWhiteSpace(filter.Operator) ? filter.Operator : "Equals";
                var value = filter.Value?.ToString();
                
                // Pular filtros completamente vazios
                if (string.IsNullOrWhiteSpace(value))
                {
                    continue;
                }
                
                filtersGrid.AddRow(new Markup($"[grey]•[/] [yellow]{field}[/] [blue]{op}[/] [white]\"{value}\"[/]"));
                validFilters++;
            }
            
            if (validFilters == 0)
            {
                filtersGrid.AddRow(new Markup("[grey]Nenhum filtro configurado[/]"));
            }
            else if (_viewModel.Configuration.Filters.Count > 5)
            {
                filtersGrid.AddRow(new Markup($"[grey]... +{_viewModel.Configuration.Filters.Count - 5}[/]"));
            }

            mainGrid.AddRow(new Panel(filtersGrid)
                .Header($"[underline cyan1]🔍 Filtros ({_viewModel.Configuration.Filters.Count})[/]", Justify.Left)
                .Border(BoxBorder.Rounded)
                .BorderColor(Color.Blue)
                .Padding(0, 0));

            mainGrid.AddEmptyRow();
        }

        return new Panel(mainGrid)
            .Header("[bold blue]📥 ORIGEM (SOURCE)[/]", Justify.Center)
            .BorderColor(Color.Blue)
            .Padding(1, 0)
            .Expand();
    }

    private Panel CreateDestinationPanel()
    {
        var mainGrid = new Grid()
            .AddColumn(new GridColumn());

        // === CONFIGURAÇÕES ===
        var configTable = new Table()
            .Border(TableBorder.None)
            .HideHeaders()
            .AddColumn(new TableColumn("Key").NoWrap())
            .AddColumn(new TableColumn("Value"));

        configTable.AddRow("[cyan1]Tipo:[/]", $"[yellow]{_viewModel.Configuration.Destination.Type}[/]");

        foreach (var setting in _viewModel.Configuration.Destination.Settings.Take(3))
        {
            var value = setting.Value?.ToString() ?? "null";
            configTable.AddRow($"  [grey]{setting.Key}:[/]", $"[white]{value}[/]");
        }

        mainGrid.AddRow(new Panel(configTable)
            .Header("[underline cyan1]⚙️  Configurações[/]", Justify.Left)
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Green)
            .Padding(0, 0));

        mainGrid.AddEmptyRow();

        // === PERFORMANCE ===
        var destMetrics = _viewModel.Destination?.GetMetrics();
        var perfTable = new Table()
            .Border(TableBorder.None)
            .HideHeaders()
            .AddColumn(new TableColumn("Key").NoWrap())
            .AddColumn(new TableColumn("Value"));

        if (destMetrics != null)
        {
            perfTable.AddRow("[cyan1]Registros Escritos:[/]", $"[yellow]{destMetrics.TotalRecordsWritten:N0}[/]");
            perfTable.AddRow("[green]✅ Sucessos:[/]", $"[green]{destMetrics.SuccessCount:N0}[/]");
            perfTable.AddRow("[red]❌ Erros:[/]", $"[red]{destMetrics.ErrorCount:N0}[/]");
            perfTable.AddRow("[cyan1]Tentativas:[/]", $"[yellow]{destMetrics.TotalRetries:N0}[/]");
            perfTable.AddRow("[cyan1]Tempo Decorrido:[/]", $"[yellow]{FormatTimeSpan(destMetrics.ElapsedTime)}[/]");
            perfTable.AddRow("[cyan1]Taxa de Escrita:[/]", $"[green]{destMetrics.RecordsPerSecond:F1}[/] rec/s");
            perfTable.AddRow("[cyan1]Tempo Médio:[/]", $"[grey]{destMetrics.AverageResponseTimeMs:F1}[/] ms");

            if (destMetrics.MinResponseTimeMs != long.MaxValue)
            {
                perfTable.AddRow("[cyan1]Min / Max:[/]",
                    $"[green]{destMetrics.MinResponseTimeMs}[/] / [red]{destMetrics.MaxResponseTimeMs}[/] ms");
            }

            // HTTP Status Codes
            if (destMetrics.CustomMetrics.TryGetValue("StatusCodes", out var statusCodesObj) &&
                statusCodesObj is Dictionary<int, long> statusCodes && statusCodes.Any())
            {
                var statusList = new List<string>();
                foreach (var kvp in statusCodes.OrderBy(x => x.Key).Take(4))
                {
                    var color = GetStatusColor(kvp.Key);
                    statusList.Add($"[{color}]{kvp.Key}[/]:[{color}]{kvp.Value}[/]");
                }

                var statusLine = string.Join(" ", statusList);
                if (statusCodes.Count > 4)
                    statusLine += $" [grey]+{statusCodes.Count - 4}[/]";

                perfTable.AddRow("[cyan1]HTTP Codes:[/]", statusLine);
            }
        }
        else
        {
            perfTable.AddRow("[grey]Aguardando dados...[/]", "");
        }

        mainGrid.AddRow(new Panel(perfTable)
            .Header("[underline cyan1]📊 Performance[/]", Justify.Left)
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Green)
            .Padding(0, 0));

        mainGrid.AddEmptyRow();

        // === PROGRESSO ===
        var progressGrid = new Grid().AddColumn();
        var sourceMetrics = _viewModel.Source?.GetMetrics();

        if (destMetrics != null)
        {
            var written = destMetrics.TotalRecordsWritten;
            var total = _viewModel.EstimatedTotal ?? sourceMetrics?.TotalRecordsRead ?? written;
            var success = destMetrics.SuccessCount;
            var errors = destMetrics.ErrorCount;

            // Progresso geral (baseado no total estimado)
            var overallProgress = total > 0 ? (written * 100.0 / total) : 0;

            // Taxas de sucesso/erro dentro do que foi escrito
            var successRate = written > 0 ? (success * 100.0 / written) : 0;
            var errorRate = written > 0 ? (errors * 100.0 / written) : 0;

            var barWidth = 30;
            
            // Barra mostra: quantos foram escritos com sucesso, quantos com erro, e quanto falta
            var successWidth = total > 0 ? Math.Max(0, Math.Min(barWidth, (int)(barWidth * success / total))) : 0;
            var errorWidth = total > 0 ? Math.Max(0, Math.Min(barWidth - successWidth, (int)(barWidth * errors / total))) : 0;
            var pendingWidth = Math.Max(0, barWidth - successWidth - errorWidth);

            var bar =
                $"[green]{new string('█', successWidth)}[/][red]{new string('█', errorWidth)}[/][grey]{new string('░', pendingWidth)}[/]";

            progressGrid.AddRow(new Markup(bar));
            progressGrid.AddRow(new Markup(
                $"[yellow]{overallProgress:F1}%[/] concluído | [green]✅ {successRate:F1}%[/] | [red]❌ {errorRate:F1}%[/]"));
            progressGrid.AddRow(new Markup($"[grey]{written:N0} / {total:N0} registros[/]"));
            
            // Tempo estimado
            if (destMetrics.RecordsPerSecond > 0 && written < total)
            {
                var remaining = total - written;
                var secondsRemaining = remaining / destMetrics.RecordsPerSecond;
                var etaTime = FormatTimeSpan(TimeSpan.FromSeconds(secondsRemaining));
                progressGrid.AddRow(new Markup($"[grey]⏱️  ETA: {etaTime}[/]"));
            }
        }
        else
        {
            progressGrid.AddRow(new Markup("[grey]Aguardando início...[/]"));
        }

        mainGrid.AddRow(new Panel(progressGrid)
            .Header("[underline cyan1]📈 Progresso[/]", Justify.Left)
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Green)
            .Padding(0, 0));

        mainGrid.AddEmptyRow();

        // === TRANSFORMAÇÕES ===
        if (_viewModel.Configuration.Transforms != null && _viewModel.Configuration.Transforms.Count > 0)
        {
            var transformsGrid = new Grid().AddColumn();
            var validTransforms = 0;
            
            foreach (var transform in _viewModel.Configuration.Transforms.Take(5))
            {
                if (transform == null) continue;
                
                var transformType = !string.IsNullOrWhiteSpace(transform.Type) ? transform.Type : "(vazio)";
                var field = !string.IsNullOrWhiteSpace(transform.Field) ? transform.Field : "(vazio)";
                
                // Pular transformações completamente vazias
                if (transformType == "(vazio)" && field == "(vazio)")
                {
                    continue;
                }
                
                transformsGrid.AddRow(new Markup($"[grey]•[/] [yellow]{field}[/] [grey]→[/] [green]{transformType}[/]"));
                validTransforms++;
            }
            
            if (validTransforms == 0)
            {
                transformsGrid.AddRow(new Markup("[grey]Nenhuma transformação configurada[/]"));
            }
            else if (_viewModel.Configuration.Transforms.Count > 5)
            {
                transformsGrid.AddRow(new Markup($"[grey]... +{_viewModel.Configuration.Transforms.Count - 5}[/]"));
            }

            mainGrid.AddRow(new Panel(transformsGrid)
                .Header($"[underline cyan1]⚙️  Transformações ({_viewModel.Configuration.Transforms.Count})[/]", Justify.Left)
                .Border(BoxBorder.Rounded)
                .BorderColor(Color.Green)
                .Padding(0, 0));

            mainGrid.AddEmptyRow();
        }

        return new Panel(mainGrid)
            .Header("[bold green]📤 DESTINO (DESTINATION)[/]", Justify.Center)
            .BorderColor(Color.Green)
            .Padding(1, 0)
            .Expand();
    }

    private Panel CreateFooterPanel()
    {
        var logsGrid = new Grid().AddColumn();
        var globalLogs = _viewModel.GetGlobalLogs();

        if (globalLogs.Length > 0)
        {
            foreach (var log in globalLogs.TakeLast(10))
            {
                logsGrid.AddRow(new Markup(log));
            }
        }
        else
        {
            logsGrid.AddRow(new Markup("[grey]Aguardando eventos do sistema...[/]"));
        }

        return new Panel(logsGrid)
            .Header("[bold grey]📋 LOGS GLOBAIS DO SISTEMA[/]", Justify.Center)
            .BorderColor(Color.Grey)
            .Padding(1, 0)
            .Expand();
    }

    private string FormatTimeSpan(TimeSpan ts)
    {
        if (ts.TotalSeconds < 60)
            return $"{ts.TotalSeconds:F0}s";
        if (ts.TotalMinutes < 60)
            return $"{(int)ts.TotalMinutes}min {ts.Seconds}s";
        return $"{(int)ts.TotalHours}h {ts.Minutes}min";
    }

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
