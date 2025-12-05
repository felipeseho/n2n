using System.Diagnostics;
using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using n2n.Models;
using Spectre.Console;

namespace n2n.Services;

/// <summary>
///     Serviço principal para processamento de arquivos CSV
/// </summary>
public class CsvProcessorService
{
    private readonly ApiClientService _apiClientService;
    private readonly CheckpointService _checkpointService;
    private readonly LoggingService _loggingService;
    private readonly MetricsService _metricsService;
    private readonly ValidationService _validationService;
    private readonly AppExecutionContext _context;

    public CsvProcessorService(
        ValidationService validationService,
        LoggingService loggingService,
        ApiClientService apiClientService,
        CheckpointService checkpointService,
        MetricsService metricsService,
        AppExecutionContext context)
    {
        _validationService = validationService;
        _loggingService = loggingService;
        _apiClientService = apiClientService;
        _checkpointService = checkpointService;
        _metricsService = metricsService;
        _context = context;
    }

    /// <summary>
    ///     Processa arquivo CSV completo
    /// </summary>
    public async Task ProcessCsvFileAsync(DashboardService dashboardService)
    {
        // Criar FilterService com as colunas configuradas (após context estar populado)
        var filterService = new FilterService(_context.Configuration.File.Columns);

        using var httpClient = _apiClientService.CreateHttpClient();

        var csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = _context.Configuration.File.CsvDelimiter,
            HasHeaderRecord = true,
            MissingFieldFound = null
        };

        // Contar total de linhas primeiro (ou usar maxLines se configurado)
        var totalLines = 0;
        if (_context.Configuration.File.MaxLines.HasValue)
        {
            // Se maxLines está configurado, usar ele como total estimado
            totalLines = _context.Configuration.File.MaxLines.Value;
            dashboardService.AddLogMessage($"Modo de teste: processando até {totalLines:N0} linhas", "INFO");
            _metricsService.StartProcessing(totalLines);
        }
        else
        {
            // Apenas conta todas as linhas se não houver limite configurado
            dashboardService.AddLogMessage("Contando linhas do arquivo CSV...", "INFO");
            await Task.Run(() =>
            {
                totalLines = CountCsvLines(_context.Configuration.File.InputPath);
                _metricsService.StartProcessing(totalLines);
            });
            dashboardService.AddLogMessage($"Total de {totalLines:N0} linhas encontradas", "SUCCESS");
        }

        using var reader = new StreamReader(_context.Configuration.File.InputPath);
        using var csv = new CsvReader(reader, csvConfig);

        // Ler cabeçalho
        await csv.ReadAsync();
        csv.ReadHeader();
        var headers = csv.HeaderRecord;

        if (headers == null || headers.Length == 0)
        {
            AnsiConsole.MarkupLine("[red]✗ Arquivo CSV não contém cabeçalho[/]");
            return;
        }

        // Tentar carregar checkpoint se configurado
        Checkpoint? checkpoint = null;
        var startLineFromCheckpoint = _context.Configuration.File.StartLine;

        if (!string.IsNullOrWhiteSpace(_context.ExecutionPaths.CheckpointPath))
        {
            checkpoint = _checkpointService.LoadCheckpoint(_context.ExecutionPaths.CheckpointPath);
            if (checkpoint != null)
            {
                startLineFromCheckpoint = checkpoint.LastProcessedLine + 1;
                dashboardService.AddLogMessage($"Checkpoint encontrado! Retomando da linha {checkpoint.LastProcessedLine + 1}", "INFO");
                dashboardService.AddLogMessage($"Progresso anterior: {checkpoint.SuccessCount} sucessos, {checkpoint.ErrorCount} erros", "INFO");
            }
        }

        // Log inicial
        dashboardService.AddLogMessage("Contagem de linhas concluída", "SUCCESS");

        var batch = new List<CsvRecord>();
        var lineNumber = 1; // Linha 1 é o cabeçalho
        var totalProcessed = checkpoint?.TotalProcessed ?? 0;
        var totalErrors = checkpoint?.ErrorCount ?? 0;
        var totalSuccess = checkpoint?.SuccessCount ?? 0;
        var totalSkipped = 0;

        // Pular linhas até a linha inicial configurada
        while (lineNumber < startLineFromCheckpoint && await csv.ReadAsync())
        {
            lineNumber++;
            totalSkipped++;
        }

        if (totalSkipped > 0) _metricsService.RecordSkippedLines(totalSkipped);

        var lastCheckpointSave = DateTime.Now;
        var checkpointIntervalSeconds = 30; // Salvar checkpoint a cada 30 segundos
        var linesProcessedCount = 0; // Contador de linhas processadas (sem contar as puladas)

        // Criar CancellationTokenSource para controlar o dashboard
        var cts = new CancellationTokenSource();

        // Iniciar dashboard em background
        var dashboardTask = Task.Run(async () => { await dashboardService.StartLiveDashboard(cts.Token); }, cts.Token);

        // Aguardar um pouco para o dashboard inicializar
        await Task.Delay(500, cts.Token);
        
        // Log de início do processamento de linhas
        dashboardService.AddLogMessage($"Iniciando leitura do CSV a partir da linha {startLineFromCheckpoint}", "INFO");

        // Processar linhas do CSV
        try
        {
            while (await csv.ReadAsync())
            {
                lineNumber++;

                var record = new CsvRecord
                {
                    LineNumber = lineNumber,
                    Data = new Dictionary<string, string>()
                };

                foreach (var header in headers) record.Data[header] = csv.GetField(header) ?? string.Empty;

                // Aplicar filtros
                if (!filterService.PassesFilters(record))
                {
                    _metricsService.RecordFilteredLines(1);
                    
                    // Verificar se atingiu o limite máximo de linhas LIDAS (não processadas)
                    // Isso evita que o filtro permita ler o arquivo inteiro
                    if (_context.Configuration.File.MaxLines.HasValue && 
                        linesProcessedCount >= _context.Configuration.File.MaxLines.Value)
                    {
                        break;
                    }
                    
                    continue;
                }

                // Validar campos
                var validationError = _validationService.ValidateRecord(record, _context.Configuration.File.Columns);
                if (validationError != null)
                {
                    await _loggingService.LogError(_context.ExecutionPaths.LogPath, record, 400, validationError, headers);
                    totalErrors++;
                    _metricsService.RecordValidationError();
                    
                    // Incrementar contador mesmo para linhas com erro
                    linesProcessedCount++;
                    
                    // Verificar se atingiu o limite máximo de linhas
                    if (_context.Configuration.File.MaxLines.HasValue && 
                        linesProcessedCount >= _context.Configuration.File.MaxLines.Value)
                    {
                        break;
                    }
                    
                    continue;
                }

                batch.Add(record);
                linesProcessedCount++;

                // Processar lote quando atingir o tamanho configurado
                if (batch.Count >= _context.Configuration.File.BatchLines)
                {
                    dashboardService.AddLogMessage($"Processando lote de {batch.Count} linhas", "INFO");
                    
                    var batchTimer = Stopwatch.StartNew();
                    var errors = await _apiClientService.ProcessBatchAsync(httpClient, batch, headers);
                    batchTimer.Stop();

                    _metricsService.RecordBatchTime(batchTimer.ElapsedMilliseconds);

                    totalProcessed += batch.Count;
                    totalErrors += errors;
                    totalSuccess += batch.Count - errors;
                    
                    if (errors > 0)
                    {
                        dashboardService.AddLogMessage($"Lote processado: {batch.Count - errors} sucessos, {errors} erros", "WARNING");
                    }
                    else
                    {
                        dashboardService.AddLogMessage($"Lote processado: {batch.Count} linhas com sucesso", "SUCCESS");
                    }

                    batch.Clear();

                    // Salvar checkpoint periodicamente
                    if (!string.IsNullOrWhiteSpace(_context.ExecutionPaths.CheckpointPath) &&
                        (DateTime.Now - lastCheckpointSave).TotalSeconds >= checkpointIntervalSeconds)
                    {
                        await _checkpointService.SaveCheckpointAsync(
                            _context.ExecutionPaths.CheckpointPath,
                            lineNumber,
                            totalProcessed,
                            totalSuccess,
                            totalErrors);
                        lastCheckpointSave = DateTime.Now;
                        dashboardService.AddLogMessage($"Checkpoint salvo - Linha {lineNumber}", "INFO");
                    }
                }
                
                // Verificar se atingiu o limite máximo de linhas APÓS adicionar ao batch
                if (_context.Configuration.File.MaxLines.HasValue && 
                    linesProcessedCount >= _context.Configuration.File.MaxLines.Value)
                {
                    break;
                }
            }

            // Processar lote restante
            if (batch.Count > 0)
            {
                dashboardService.AddLogMessage($"Processando lote final de {batch.Count} linhas", "INFO");
                
                var batchTimer = Stopwatch.StartNew();
                var errors = await _apiClientService.ProcessBatchAsync(httpClient, batch, headers);
                batchTimer.Stop();

                _metricsService.RecordBatchTime(batchTimer.ElapsedMilliseconds);

                totalProcessed += batch.Count;
                totalErrors += errors;
                totalSuccess += batch.Count - errors;
                
                if (errors > 0)
                {
                    dashboardService.AddLogMessage($"Lote final processado: {batch.Count - errors} sucessos, {errors} erros", "WARNING");
                }
                else
                {
                    dashboardService.AddLogMessage($"Lote final processado com sucesso", "SUCCESS");
                }
            }
            
            dashboardService.AddLogMessage("Processamento concluído!", "SUCCESS");
        }
        finally
        {
            // Parar o dashboard
            cts.Cancel();
            try
            {
                await dashboardTask;
            }
            catch (OperationCanceledException)
            {
                // Esperado quando cancelamos
            }
        }

        // Finalizar métricas
        _metricsService.EndProcessing();

        // Limpar console e mostrar dashboard final
        AnsiConsole.Clear();
        dashboardService.UpdateOnce();
        AnsiConsole.WriteLine();

        // Salvar checkpoint final
        if (!string.IsNullOrWhiteSpace(_context.ExecutionPaths.CheckpointPath))
        {
            await _checkpointService.SaveCheckpointAsync(
                _context.ExecutionPaths.CheckpointPath,
                lineNumber,
                totalProcessed,
                totalSuccess,
                totalErrors);

            AnsiConsole.MarkupLine($"[cyan1]💾 Checkpoint salvo em:[/] [grey]{_context.ExecutionPaths.CheckpointPath}[/]");
        }

        AnsiConsole.WriteLine();

        // Exibir dashboard de métricas detalhadas
        _metricsService.DisplayDashboard();
    }

    /// <summary>
    ///     Conta o número de linhas no arquivo CSV (excluindo cabeçalho)
    /// </summary>
    private int CountCsvLines(string filePath)
    {
        try
        {
            using var reader = new StreamReader(filePath);
            var count = 0;
            while (reader.ReadLine() != null) count++;
            return count - 1; // Excluir cabeçalho
        }
        catch
        {
            return 0;
        }
    }
}