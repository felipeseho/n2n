using System.Diagnostics;
using System.Globalization;
using System.IO;
using CsvHelper;
using CsvHelper.Configuration;
using CsvToApi.Models;

namespace CsvToApi.Services;

/// <summary>
/// Serviço principal para processamento de arquivos CSV
/// </summary>
public class CsvProcessorService
{
    private readonly ValidationService _validationService;
    private readonly LoggingService _loggingService;
    private readonly ApiClientService _apiClientService;
    private readonly CheckpointService _checkpointService;
    private readonly MetricsService _metricsService;

    public CsvProcessorService(
        ValidationService validationService,
        LoggingService loggingService,
        ApiClientService apiClientService,
        CheckpointService checkpointService,
        MetricsService metricsService)
    {
        _validationService = validationService;
        _loggingService = loggingService;
        _apiClientService = apiClientService;
        _checkpointService = checkpointService;
        _metricsService = metricsService;
    }

    /// <summary>
    /// Processa arquivo CSV completo
    /// </summary>
    public async Task ProcessCsvFileAsync(Configuration config, bool dryRun = false)
    {
        using var httpClient = _apiClientService.CreateHttpClient(config.Api);

        var csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = config.File.CsvDelimiter,
            HasHeaderRecord = true,
            MissingFieldFound = null
        };

        // Contar total de linhas primeiro
        var totalLines = CountCsvLines(config.File.InputPath);
        _metricsService.StartProcessing(totalLines);

        using var reader = new StreamReader(config.File.InputPath);
        using var csv = new CsvReader(reader, csvConfig);

        // Ler cabeçalho
        await csv.ReadAsync();
        csv.ReadHeader();
        var headers = csv.HeaderRecord;

        if (headers == null || headers.Length == 0)
        {
            Console.WriteLine("Arquivo CSV não contém cabeçalho");
            return;
        }

        // Tentar carregar checkpoint se configurado
        Checkpoint? checkpoint = null;
        var startLineFromCheckpoint = config.File.StartLine;
        
        if (!string.IsNullOrWhiteSpace(config.File.CheckpointPath))
        {
            checkpoint = _checkpointService.LoadCheckpoint(config.File.CheckpointPath);
            if (checkpoint != null)
            {
                Console.WriteLine($"📍 Checkpoint encontrado! Retomando da linha {checkpoint.LastProcessedLine + 1}");
                Console.WriteLine($"   Progresso anterior: {checkpoint.SuccessCount} sucessos, {checkpoint.ErrorCount} erros");
                startLineFromCheckpoint = checkpoint.LastProcessedLine + 1;
            }
        }

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

        if (totalSkipped > 0)
        {
            Console.WriteLine($"⏭️  Puladas {totalSkipped} linhas (iniciando na linha {startLineFromCheckpoint})");
            _metricsService.RecordSkippedLines(totalSkipped);
        }

        var lastCheckpointSave = DateTime.Now;
        var lastMetricsDisplay = DateTime.Now;
        var checkpointIntervalSeconds = 30; // Salvar checkpoint a cada 30 segundos
        var metricsDisplayIntervalSeconds = 5; // Atualizar métricas a cada 5 segundos

        while (await csv.ReadAsync())
        {
            lineNumber++;
            
            var record = new CsvRecord
            {
                LineNumber = lineNumber,
                Data = new Dictionary<string, string>()
            };

            foreach (var header in headers)
            {
                record.Data[header] = csv.GetField(header) ?? string.Empty;
            }

            // Validar campos
            var validationError = _validationService.ValidateRecord(record, config.File.Mapping);
            if (validationError != null)
            {
                await _loggingService.LogError(config.File.LogPath, record, 400, validationError, headers);
                totalErrors++;
                _metricsService.RecordValidationError();
                continue;
            }

            batch.Add(record);

            // Processar lote quando atingir o tamanho configurado
            if (batch.Count >= config.File.BatchLines)
            {
                var batchTimer = Stopwatch.StartNew();
                var errors = await _apiClientService.ProcessBatchAsync(httpClient, batch, config, headers, dryRun);
                batchTimer.Stop();
                
                _metricsService.RecordBatchTime(batchTimer.ElapsedMilliseconds);
                
                totalProcessed += batch.Count;
                totalErrors += errors;
                totalSuccess += (batch.Count - errors);
                batch.Clear();

                // Exibir atualização de progresso
                if ((DateTime.Now - lastMetricsDisplay).TotalSeconds >= metricsDisplayIntervalSeconds)
                {
                    _metricsService.DisplayProgressUpdate();
                    lastMetricsDisplay = DateTime.Now;
                }

                // Salvar checkpoint periodicamente
                if (!string.IsNullOrWhiteSpace(config.File.CheckpointPath) && 
                    (DateTime.Now - lastCheckpointSave).TotalSeconds >= checkpointIntervalSeconds)
                {
                    await _checkpointService.SaveCheckpointAsync(
                        config.File.CheckpointPath, 
                        lineNumber, 
                        totalProcessed, 
                        totalSuccess, 
                        totalErrors);
                    lastCheckpointSave = DateTime.Now;
                }
            }
        }

        // Processar lote restante
        if (batch.Count > 0)
        {
            var batchTimer = Stopwatch.StartNew();
            var errors = await _apiClientService.ProcessBatchAsync(httpClient, batch, config, headers, dryRun);
            batchTimer.Stop();
            
            _metricsService.RecordBatchTime(batchTimer.ElapsedMilliseconds);
            
            totalProcessed += batch.Count;
            totalErrors += errors;
            totalSuccess += (batch.Count - errors);
        }

        // Finalizar métricas
        _metricsService.EndProcessing();
        Console.WriteLine(); // Nova linha após progress update

        // Salvar checkpoint final
        if (!string.IsNullOrWhiteSpace(config.File.CheckpointPath))
        {
            await _checkpointService.SaveCheckpointAsync(
                config.File.CheckpointPath, 
                lineNumber, 
                totalProcessed, 
                totalSuccess, 
                totalErrors);
            
            Console.WriteLine($"💾 Checkpoint salvo em: {config.File.CheckpointPath}");
        }

        // Exibir dashboard final
        _metricsService.DisplayDashboard();
    }

    /// <summary>
    /// Conta o número de linhas no arquivo CSV (excluindo cabeçalho)
    /// </summary>
    private int CountCsvLines(string filePath)
    {
        try
        {
            using var reader = new StreamReader(filePath);
            int count = 0;
            while (reader.ReadLine() != null)
            {
                count++;
            }
            return count - 1; // Excluir cabeçalho
        }
        catch
        {
            return 0;
        }
    }
}

