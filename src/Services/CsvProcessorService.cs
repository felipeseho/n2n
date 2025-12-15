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
    ///     Processa múltiplos arquivos CSV conforme configurado
    /// </summary>
    public async Task ProcessCsvFileAsync(DashboardService dashboardService)
    {
        var inputFiles = _context.Configuration.File.GetInputFiles();

        if (inputFiles.Count == 0)
        {
            dashboardService.AddLogMessage("Nenhum arquivo de entrada configurado", "ERROR");
            return;
        }

        dashboardService.AddLogMessage($"Total de {inputFiles.Count} arquivo(s) para processar", "INFO");

        // Verificar quais arquivos já foram completados
        var currentExecutionId = _context.CommandLineOptions.ExecutionId ?? Guid.NewGuid().ToString();
        var configService = new ConfigurationService();

        for (var i = 0; i < inputFiles.Count; i++)
        {
            var inputFile = inputFiles[i];
            
            // Verificar se este arquivo já foi completado
            var checkpointPath = configService.GenerateExecutionPaths(_context.Configuration, currentExecutionId, inputFile).CheckpointPath;
            var existingCheckpoint = _checkpointService.LoadCheckpoint(checkpointPath);
            
            if (existingCheckpoint != null && existingCheckpoint.IsCompleted)
            {
                dashboardService.AddLogMessage($"⏭️  Arquivo {i + 1}/{inputFiles.Count} já processado: {Path.GetFileName(inputFile)}", "INFO");
                continue;
            }
            
            dashboardService.AddLogMessage($"═══════════════════════════════════════════════════", "INFO");
            dashboardService.AddLogMessage($"Processando arquivo {i + 1}/{inputFiles.Count}: {Path.GetFileName(inputFile)}", "INFO");
            dashboardService.AddLogMessage($"═══════════════════════════════════════════════════", "INFO");

            await ProcessSingleCsvFileAsync(dashboardService, inputFile);

            if (i < inputFiles.Count - 1)
            {
                dashboardService.AddLogMessage($"Arquivo {i + 1} concluído. Próximo arquivo...", "SUCCESS");
                await Task.Delay(1000); // Pequena pausa entre arquivos
            }
        }

        dashboardService.AddLogMessage("═══════════════════════════════════════════════════", "SUCCESS");
        dashboardService.AddLogMessage($"Todos os {inputFiles.Count} arquivos foram processados!", "SUCCESS");
        dashboardService.AddLogMessage("═══════════════════════════════════════════════════", "SUCCESS");
        
        // Mostrar resumo final se houver múltiplos arquivos
        if (inputFiles.Count > 1)
        {
            await Task.Delay(500);
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine($"[bold cyan1]📊 RESUMO FINAL - {inputFiles.Count} ARQUIVOS PROCESSADOS[/]");
            AnsiConsole.WriteLine();
        }
    }

    /// <summary>
    ///     Processa um único arquivo CSV
    /// </summary>
    private async Task ProcessSingleCsvFileAsync(DashboardService dashboardService, string inputFilePath)
    {
        // Verificar se arquivo existe
        if (!File.Exists(inputFilePath))
        {
            dashboardService.AddLogMessage($"⚠️  Arquivo não encontrado: {inputFilePath}", "ERROR");

            // Gerar paths específicos para este arquivo
            var executionId = _context.CommandLineOptions.ExecutionId ?? Guid.NewGuid().ToString();
            var configService = new ConfigurationService();
            var executionPaths = configService.GenerateExecutionPaths(_context.Configuration, executionId, inputFilePath);

            // Criar checkpoint e log registrando que o arquivo não foi encontrado
            await _loggingService.LogError(
                executionPaths.LogPath,
                new CsvRecord { LineNumber = 0, Data = new Dictionary<string, string>() },
                404,
                $"Arquivo não encontrado: {inputFilePath}",
                Array.Empty<string>());

            await _checkpointService.SaveCheckpointAsync(
                executionPaths.CheckpointPath,
                0,
                0,
                0,
                1,
                _context,
                DateTime.Now,
                $"Arquivo não encontrado: {inputFilePath}");

            dashboardService.AddLogMessage($"Checkpoint e log criados para arquivo não encontrado", "WARNING");
            return;
        }

        // Atualizar executionPaths com o arquivo atual
        var currentExecutionId = _context.CommandLineOptions.ExecutionId ?? Guid.NewGuid().ToString();
        var currentConfigService = new ConfigurationService();
        _context.ExecutionPaths = currentConfigService.GenerateExecutionPaths(_context.Configuration, currentExecutionId, inputFilePath);

        dashboardService.AddLogMessage($"Arquivo: {inputFilePath}", "INFO");
        dashboardService.AddLogMessage($"Log: {_context.ExecutionPaths.LogPath}", "INFO");
        dashboardService.AddLogMessage($"Checkpoint: {_context.ExecutionPaths.CheckpointPath}", "INFO");

        // Resetar métricas para o novo arquivo
        _metricsService.Reset();

        // Tempo de início para este arquivo específico
        var executionStartTime = DateTime.Now;

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
                totalLines = CountCsvLines(inputFilePath);
                _metricsService.StartProcessing(totalLines);
            });
            dashboardService.AddLogMessage($"Total de {totalLines:N0} linhas encontradas", "SUCCESS");
        }

        using var reader = new StreamReader(inputFilePath);
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
        var lineNumber = 0; // Contador de linhas de dados (cabeçalho não conta)
        
        // Começar do zero ou do checkpoint se existir
        var totalProcessed = checkpoint?.TotalProcessed ?? 0;
        var totalErrors = checkpoint?.ErrorCount ?? 0;
        var totalSuccess = checkpoint?.SuccessCount ?? 0;
        var totalSkipped = 0;

        // Pular linhas até a linha inicial configurada (startLine conta a partir de 1 para primeira linha de dados)
        while (lineNumber < (startLineFromCheckpoint - 1) && await csv.ReadAsync())
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
                            totalErrors,
                            _context,
                            executionStartTime);
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

        // Salvar checkpoint final marcado como completo
        if (!string.IsNullOrWhiteSpace(_context.ExecutionPaths.CheckpointPath))
        {
            await _checkpointService.SaveCheckpointAsync(
                _context.ExecutionPaths.CheckpointPath,
                lineNumber,
                totalProcessed,
                totalSuccess,
                totalErrors,
                _context,
                executionStartTime,
                errorMessage: null,
                isCompleted: true); // Marcar como completo

            dashboardService.AddLogMessage($"💾 Checkpoint salvo: {Path.GetFileName(_context.ExecutionPaths.CheckpointPath)}", "SUCCESS");
        }
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