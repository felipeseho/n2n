using System.Diagnostics;
using n2n.Models;
using Spectre.Console;

namespace n2n.Services;

/// <summary>
///     Serviço para coletar e gerenciar métricas de processamento
/// </summary>
public class MetricsService
{
    private readonly List<long> _batchTimes = new();
    private readonly object _lock = new();
    private readonly ProcessingMetrics _metrics;
    private readonly List<long> _responseTimes = new();

    public MetricsService()
    {
        _metrics = new ProcessingMetrics
        {
            StartTime = DateTime.Now
        };
    }

    public ProcessingMetrics GetMetrics()
    {
        return _metrics;
    }

    /// <summary>
    ///     Inicia o rastreamento do processamento
    /// </summary>
    public void StartProcessing(int totalLines)
    {
        lock (_lock)
        {
            _metrics.StartTime = DateTime.Now;
            _metrics.TotalLines = totalLines;
        }
    }

    /// <summary>
    ///     Finaliza o rastreamento do processamento
    /// </summary>
    public void EndProcessing()
    {
        lock (_lock)
        {
            _metrics.EndTime = DateTime.Now;
        }
    }

    /// <summary>
    ///     Registra linhas puladas
    /// </summary>
    public void RecordSkippedLines(int count)
    {
        lock (_lock)
        {
            _metrics.SkippedLines += count;
        }
    }

    /// <summary>
    ///     Registra linhas filtradas
    /// </summary>
    public void RecordFilteredLines(int count)
    {
        lock (_lock)
        {
            _metrics.FilteredLines += count;
        }
    }

    /// <summary>
    ///     Registra uma linha processada com sucesso
    /// </summary>
    public void RecordSuccess()
    {
        lock (_lock)
        {
            _metrics.ProcessedLines++;
            _metrics.SuccessCount++;
        }
    }

    /// <summary>
    ///     Registra um erro de processamento
    /// </summary>
    public void RecordError()
    {
        lock (_lock)
        {
            _metrics.ProcessedLines++;
            _metrics.ErrorCount++;
        }
    }

    /// <summary>
    ///     Registra um erro de validação
    /// </summary>
    public void RecordValidationError()
    {
        lock (_lock)
        {
            _metrics.ValidationErrors++;
        }
    }

    /// <summary>
    ///     Registra uma tentativa de retry
    /// </summary>
    public void RecordRetry()
    {
        lock (_lock)
        {
            _metrics.TotalRetries++;
        }
    }

    /// <summary>
    ///     Registra tempo de resposta HTTP
    /// </summary>
    public void RecordResponseTime(long milliseconds)
    {
        lock (_lock)
        {
            _responseTimes.Add(milliseconds);

            if (milliseconds < _metrics.MinResponseTimeMs)
                _metrics.MinResponseTimeMs = milliseconds;

            if (milliseconds > _metrics.MaxResponseTimeMs)
                _metrics.MaxResponseTimeMs = milliseconds;

            _metrics.AverageResponseTimeMs = _responseTimes.Average();
        }
    }

    /// <summary>
    ///     Registra código de status HTTP
    /// </summary>
    public void RecordHttpStatusCode(int statusCode)
    {
        lock (_lock)
        {
            if (!_metrics.HttpStatusCodes.ContainsKey(statusCode)) _metrics.HttpStatusCodes[statusCode] = 0;
            _metrics.HttpStatusCodes[statusCode]++;
        }
    }

    /// <summary>
    ///     Registra tempo de processamento de um batch
    /// </summary>
    public void RecordBatchTime(long milliseconds)
    {
        lock (_lock)
        {
            _batchTimes.Add(milliseconds);
            _metrics.BatchesProcessed++;
            _metrics.AverageBatchTimeMs = _batchTimes.Average();
        }
    }

    /// <summary>
    ///     Retorna uma medição de tempo para um batch
    /// </summary>
    public Stopwatch StartBatchTimer()
    {
        return Stopwatch.StartNew();
    }

    /// <summary>
    ///     Retorna uma medição de tempo para uma requisição
    /// </summary>
    public Stopwatch StartRequestTimer()
    {
        return Stopwatch.StartNew();
    }

    /// <summary>
    ///     Renderiza dashboard de métricas no console
    /// </summary>
    public void DisplayDashboard(bool showDetails = true)
    {
        var metrics = GetMetrics();
        var elapsed = metrics.ElapsedTime;

        // Criar tabela principal de progresso
        var progressTable = new Table()
            .Border(TableBorder.Double)
            .BorderColor(Color.Cyan1)
            .Title("[bold cyan1]📊 DASHBOARD DE PERFORMANCE[/]")
            .AddColumn(new TableColumn("[bold]Métrica[/]").LeftAligned())
            .AddColumn(new TableColumn("[bold]Valor[/]").RightAligned());

        // Progresso
        progressTable.AddRow("[cyan1]Total de Linhas[/]", $"[yellow]{metrics.TotalLines:N0}[/]");
        progressTable.AddRow("[cyan1]Linhas Processadas[/]",
            $"[green]{metrics.ProcessedLines:N0}[/] [grey]({metrics.ProgressPercentage:F1}%)[/]");
        if (metrics.SkippedLines > 0)
            progressTable.AddRow("[cyan1]Linhas Puladas[/]", $"[yellow]{metrics.SkippedLines:N0}[/]");

        // Resultados
        progressTable.AddEmptyRow();
        progressTable.AddRow("[bold green]✓ Sucessos[/]",
            $"[green]{metrics.SuccessCount:N0}[/] [grey]({metrics.SuccessRate:F1}%)[/]");
        progressTable.AddRow("[bold red]✗ Erros HTTP[/]",
            $"[red]{metrics.ErrorCount:N0}[/] [grey]({metrics.ErrorRate:F1}%)[/]");
        progressTable.AddRow("[bold yellow]⚠ Erros de Validação[/]", $"[yellow]{metrics.ValidationErrors:N0}[/]");

        // Tempo
        progressTable.AddEmptyRow();
        progressTable.AddRow("[cyan1]⏱️  Tempo Decorrido[/]", $"[yellow]{FormatTimeSpan(elapsed)}[/]");
        if (metrics.TotalLines > metrics.ProcessedLines && metrics.ProcessedLines > 0)
            progressTable.AddRow("[cyan1]⏳ Tempo Restante[/]",
                $"[yellow]{FormatTimeSpan(metrics.EstimatedTimeRemaining)}[/]");
        progressTable.AddRow("[cyan1]🚀 Velocidade[/]", $"[green]{metrics.LinesPerSecond:F1}[/] [grey]linhas/seg[/]");

        AnsiConsole.Write(progressTable);
        AnsiConsole.WriteLine();

        // Barra de progresso
        var progressBar = new BarChart()
            .Width(60)
            .Label("[bold cyan1]Progresso[/]")
            .CenterLabel();

        if (metrics.SuccessCount > 0)
            progressBar.AddItem("Sucessos", metrics.SuccessCount, Color.Green);
        if (metrics.ErrorCount > 0)
            progressBar.AddItem("Erros", metrics.ErrorCount, Color.Red);
        if (metrics.ValidationErrors > 0)
            progressBar.AddItem("Validação", metrics.ValidationErrors, Color.Yellow);

        if (metrics.SuccessCount > 0 || metrics.ErrorCount > 0 || metrics.ValidationErrors > 0)
        {
            AnsiConsole.Write(progressBar);
            AnsiConsole.WriteLine();
        }

        if (showDetails)
        {
            // Performance HTTP
            if (metrics.ProcessedLines > 0)
            {
                var perfTable = new Table()
                    .Border(TableBorder.Rounded)
                    .BorderColor(Color.Blue)
                    .Title("[bold blue]🌐 PERFORMANCE HTTP[/]")
                    .AddColumn(new TableColumn("[bold]Métrica[/]").Centered())
                    .AddColumn(new TableColumn("[bold]Valor[/]").RightAligned());

                perfTable.AddRow("[cyan1]Tempo Médio[/]", $"[yellow]{metrics.AverageResponseTimeMs:F0} ms[/]");
                if (_responseTimes.Count > 0)
                {
                    perfTable.AddRow("[cyan1]Tempo Mínimo[/]", $"[green]{metrics.MinResponseTimeMs} ms[/]");
                    perfTable.AddRow("[cyan1]Tempo Máximo[/]", $"[red]{metrics.MaxResponseTimeMs} ms[/]");
                }

                if (metrics.TotalRetries > 0)
                    perfTable.AddRow("[cyan1]Total de Retries[/]", $"[yellow]{metrics.TotalRetries}[/]");

                AnsiConsole.Write(perfTable);
                AnsiConsole.WriteLine();
            }

            // Batches
            if (metrics.BatchesProcessed > 0)
            {
                var batchTable = new Table()
                    .Border(TableBorder.Rounded)
                    .BorderColor(Color.Purple)
                    .Title("[bold purple]📦 PROCESSAMENTO EM LOTE[/]")
                    .AddColumn(new TableColumn("[bold]Métrica[/]").Centered())
                    .AddColumn(new TableColumn("[bold]Valor[/]").RightAligned());

                batchTable.AddRow("[cyan1]Batches Processados[/]", $"[yellow]{metrics.BatchesProcessed}[/]");
                batchTable.AddRow("[cyan1]Tempo Médio/Batch[/]", $"[yellow]{metrics.AverageBatchTimeMs:F0} ms[/]");

                AnsiConsole.Write(batchTable);
                AnsiConsole.WriteLine();
            }

            // Status HTTP
            if (metrics.HttpStatusCodes.Count > 0)
            {
                var statusTable = new Table()
                    .Border(TableBorder.Rounded)
                    .BorderColor(Color.Orange1)
                    .Title("[bold orange1]📊 CÓDIGOS HTTP[/]")
                    .AddColumn(new TableColumn("[bold]Código[/]").Centered())
                    .AddColumn(new TableColumn("[bold]Quantidade[/]").RightAligned())
                    .AddColumn(new TableColumn("[bold]Percentual[/]").RightAligned());

                foreach (var kvp in metrics.HttpStatusCodes.OrderBy(x => x.Key))
                {
                    var percentage = kvp.Value * 100.0 / metrics.ProcessedLines;
                    var color = GetStatusColor(kvp.Key);
                    statusTable.AddRow(
                        $"[{color}]{kvp.Key}[/]",
                        $"[{color}]{kvp.Value:N0}[/]",
                        $"[{color}]{percentage:F1}%[/]");
                }

                AnsiConsole.Write(statusTable);
                AnsiConsole.WriteLine();
            }
        }
    }

    /// <summary>
    ///     Exibe resumo compacto durante o processamento
    /// </summary>
    public void DisplayProgressUpdate()
    {
        // Esta função agora não é necessária pois usamos a barra de progresso do Spectre.Console
        // Mantida para compatibilidade, mas não faz nada
    }

    /// <summary>
    ///     Formata TimeSpan para exibição amigável
    /// </summary>
    private string FormatTimeSpan(TimeSpan ts)
    {
        if (ts.TotalSeconds < 60)
            return $"{ts.TotalSeconds:F0}s";
        if (ts.TotalMinutes < 60)
            return $"{ts.Minutes}min {ts.Seconds}s";
        return $"{ts.Hours}h {ts.Minutes}min {ts.Seconds}s";
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