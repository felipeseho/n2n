using System.Diagnostics;
using n2n.Core;
using n2n.Factories;
using n2n.Models;
using Spectre.Console;

namespace n2n.Services;

/// <summary>
///     Serviço de processamento de pipeline de dados
/// </summary>
public class DataPipelineService
{
    private readonly IDataSourceFactory _sourceFactory;
    private readonly IDataDestinationFactory _destinationFactory;
    private readonly PipelineConfiguration _configuration;
    private readonly PipelineDashboardService _dashboardService;
    private readonly PipelineCheckpointService _checkpointService;
    private readonly string _executionId;
    private IDataSource? _source;
    private IDataDestination? _destination;
    private PipelineCheckpoint? _checkpoint;

    public DataPipelineService(
        IDataSourceFactory sourceFactory,
        IDataDestinationFactory destinationFactory,
        PipelineConfiguration configuration,
        PipelineDashboardService dashboardService,
        PipelineCheckpointService checkpointService,
        string? executionId = null)
    {
        _sourceFactory = sourceFactory;
        _destinationFactory = destinationFactory;
        _configuration = configuration;
        _dashboardService = dashboardService;
        _checkpointService = checkpointService;
        _executionId = executionId ?? _checkpointService.GenerateExecutionId();
    }

    /// <summary>
    ///     Executa o pipeline de dados
    /// </summary>
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        _dashboardService.SetExecutionId(_executionId);
        _dashboardService.AddLogMessage($"Iniciando pipeline '{_configuration.Name}'", "INFO");
        _dashboardService.AddLogMessage($"Execution ID: {_executionId}", "INFO");

        try
        {
            // Tentar carregar checkpoint existente
            _checkpoint = await _checkpointService.LoadCheckpointAsync(
                _configuration.Processing.CheckpointDirectory,
                _executionId);

            if (_checkpoint != null)
            {
                _dashboardService.AddLogMessage(
                    $"Checkpoint encontrado! Retomando da posição {_checkpoint.LastProcessedRecordId}",
                    "INFO");
                _dashboardService.AddLogMessage(
                    $"Progresso anterior: {_checkpoint.SuccessCount} sucessos, {_checkpoint.ErrorCount} erros",
                    "INFO");
            }
            else
            {
                // Criar novo checkpoint
                _checkpoint = new PipelineCheckpoint
                {
                    ExecutionId = _executionId,
                    PipelineName = _configuration.Name,
                    SourceType = _configuration.Source.Type,
                    DestinationType = _configuration.Destination.Type,
                    StartedAt = DateTime.UtcNow
                };
            }

            // Criar origem e destino
            _dashboardService.AddLogMessage($"Criando origem de dados tipo '{_configuration.Source.Type}'", "INFO");
            _source = await _sourceFactory.CreateAsync(_configuration.Source);
            _dashboardService.SetSource(_source);

            _dashboardService.AddLogMessage($"Criando destino de dados tipo '{_configuration.Destination.Type}'",
                "INFO");
            _destination = await _destinationFactory.CreateAsync(_configuration.Destination);
            _dashboardService.SetDestination(_destination);

            // Obter estimativa de registros
            var estimatedCount = await _source.GetEstimatedCountAsync();
            if (estimatedCount.HasValue)
            {
                _dashboardService.AddLogMessage($"Estimativa: {estimatedCount.Value:N0} registros", "INFO");
                _dashboardService.SetEstimatedTotal(estimatedCount);
            }

            // Iniciar dashboard
            var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var dashboardTask = Task.Run(async () => { await _dashboardService.StartLiveDashboard(cts.Token); },
                cts.Token);

            await Task.Delay(500, cancellationToken); // Aguardar dashboard inicializar

            // Processar dados
            await ProcessDataAsync(cancellationToken);

            // Parar dashboard
            cts.Cancel();
            try
            {
                await dashboardTask;
            }
            catch (OperationCanceledException)
            {
                // Esperado
            }

            // Exibir dashboard final
            AnsiConsole.Clear();
            _dashboardService.UpdateOnce();
            AnsiConsole.WriteLine();

            // Remover checkpoint ao concluir com sucesso
            _checkpointService.DeleteCheckpoint(_configuration.Processing.CheckpointDirectory, _executionId);

            _dashboardService.AddLogMessage("Pipeline concluído com sucesso!", "SUCCESS");
        }
        catch (Exception ex)
        {
            _dashboardService.AddLogMessage($"Erro no pipeline: {ex.Message}", "ERROR");
            
            // Salvar checkpoint em caso de erro
            if (_checkpoint != null)
            {
                await _checkpointService.SaveCheckpointAsync(
                    _configuration.Processing.CheckpointDirectory,
                    _checkpoint);
                _dashboardService.AddLogMessage($"Checkpoint salvo: {_executionId}", "INFO");
            }
            
            throw;
        }
        finally
        {
            _source?.Dispose();
            _destination?.Dispose();
        }
    }

    private async Task ProcessDataAsync(CancellationToken cancellationToken)
    {
        var batch = new List<DataRecord>();
        var processedCount = _checkpoint?.TotalProcessed ?? 0;
        var successCount = _checkpoint?.SuccessCount ?? 0;
        var errorCount = _checkpoint?.ErrorCount ?? 0;
        var filteredCount = 0;
        var lastCheckpointSave = DateTime.UtcNow;
        var shouldSkip = !string.IsNullOrWhiteSpace(_checkpoint?.LastProcessedRecordId);

        await foreach (var record in _source!.ReadAsync(cancellationToken))
        {
            // Pular registros já processados
            if (shouldSkip)
            {
                if (record.RecordId == _checkpoint!.LastProcessedRecordId)
                {
                    shouldSkip = false;
                }
                continue;
            }

            // Aplicar filtros
            if (!PassesFilters(record))
            {
                filteredCount++;
                continue;
            }

            // Aplicar transformações
            ApplyTransforms(record);

            batch.Add(record);

            // Obter BatchSize da origem
            var batchSize = GetBatchSizeFromSource();

            // Processar lote
            if (batch.Count >= batchSize)
            {
                _dashboardService.AddLogMessage($"Processando lote de {batch.Count} registros", "INFO");

                var batchTimer = Stopwatch.StartNew();
                var batchResult = await _destination!.WriteBatchAsync(batch, cancellationToken);
                batchTimer.Stop();

                successCount += batchResult.SuccessCount;
                errorCount += batchResult.ErrorCount;
                processedCount += batch.Count;

                // Atualizar checkpoint
                _checkpoint!.LastProcessedRecordId = batch.Last().RecordId;
                _checkpoint.TotalProcessed = processedCount;
                _checkpoint.SuccessCount = successCount;
                _checkpoint.ErrorCount = errorCount;

                // Log erros
                if (batchResult.ErrorCount > 0)
                {
                    _dashboardService.AddLogMessage(
                        $"Lote: {batchResult.SuccessCount} sucessos, {batchResult.ErrorCount} erros",
                        "WARNING");
                }
                else
                {
                    _dashboardService.AddLogMessage($"Lote processado com sucesso", "SUCCESS");
                }

                batch.Clear();

                // Salvar checkpoint periodicamente
                var timeSinceLastSave = (DateTime.UtcNow - lastCheckpointSave).TotalSeconds;
                if (timeSinceLastSave >= _configuration.Processing.CheckpointIntervalSeconds)
                {
                    await _checkpointService.SaveCheckpointAsync(
                        _configuration.Processing.CheckpointDirectory,
                        _checkpoint);
                    lastCheckpointSave = DateTime.UtcNow;
                    _dashboardService.AddLogMessage($"Checkpoint salvo", "INFO");
                }
            }
        }

        // Processar lote restante
        if (batch.Count > 0)
        {
            _dashboardService.AddLogMessage($"Processando lote final de {batch.Count} registros", "INFO");

            var batchResult = await _destination!.WriteBatchAsync(batch, cancellationToken);
            successCount += batchResult.SuccessCount;
            errorCount += batchResult.ErrorCount;
            processedCount += batch.Count;

            // Atualizar checkpoint
            _checkpoint!.LastProcessedRecordId = batch.Last().RecordId;
            _checkpoint.TotalProcessed = processedCount;
            _checkpoint.SuccessCount = successCount;
            _checkpoint.ErrorCount = errorCount;


            if (batchResult.ErrorCount > 0)
            {
                _dashboardService.AddLogMessage(
                    $"Lote final: {batchResult.SuccessCount} sucessos, {batchResult.ErrorCount} erros",
                    "WARNING");
            }
            else
            {
                _dashboardService.AddLogMessage($"Lote final processado com sucesso", "SUCCESS");
            }
        }

        _dashboardService.AddLogMessage(
            $"Processamento concluído: {successCount} sucessos, {errorCount} erros de {processedCount} registros ({filteredCount} filtrados)",
            errorCount > 0 ? "WARNING" : "SUCCESS");
    }

    private bool PassesFilters(DataRecord record)
    {
        // Se não há filtros, passa
        if (_configuration.Filters == null || _configuration.Filters.Count == 0)
        {
            return true;
        }

        // Todos os filtros devem passar (lógica AND)
        foreach (var filter in _configuration.Filters)
        {
            if (!record.Data.TryGetValue(filter.Field, out var fieldValue))
            {
                // Se o campo do filtro não existir no registro, ignora este filtro específico
                // em vez de rejeitar o registro inteiro.
                // Isso permite que os filtros sejam mais flexíveis.
                continue;
            }

            var fieldValueStr = fieldValue?.ToString() ?? string.Empty;
            var filterValueStr = filter.Value?.ToString() ?? string.Empty;

            if (!filter.CaseSensitive)
            {
                fieldValueStr = fieldValueStr.ToLowerInvariant();
                filterValueStr = filterValueStr.ToLowerInvariant();
            }

            var passes = filter.Operator.ToUpper() switch
            {
                "EQUALS" => fieldValueStr == filterValueStr,
                "NOTEQUALS" => fieldValueStr != filterValueStr,
                "CONTAINS" => fieldValueStr.Contains(filterValueStr),
                "NOTCONTAINS" => !fieldValueStr.Contains(filterValueStr),
                "STARTSWITH" => fieldValueStr.StartsWith(filterValueStr),
                "ENDSWITH" => fieldValueStr.EndsWith(filterValueStr),
                "GREATERTHAN" => string.Compare(fieldValueStr, filterValueStr, StringComparison.Ordinal) > 0,
                "LESSTHAN" => string.Compare(fieldValueStr, filterValueStr, StringComparison.Ordinal) < 0,
                _ => true
            };

            if (!passes)
            {
                return false;
            }
        }

        return true;
    }

    private void ApplyTransforms(DataRecord record)
    {
        foreach (var transform in _configuration.Transforms)
        {
            if (!record.Data.TryGetValue(transform.Field, out var fieldValue))
            {
                continue;
            }

            var transformedValue = transform.Type.ToUpper() switch
            {
                "TOUPPER" => fieldValue?.ToString()?.ToUpperInvariant(),
                "TOLOWER" => fieldValue?.ToString()?.ToLowerInvariant(),
                "TRIM" => fieldValue?.ToString()?.Trim(),
                "TRIMSTART" => fieldValue?.ToString()?.TrimStart(),
                "TRIMEND" => fieldValue?.ToString()?.TrimEnd(),
                _ => fieldValue
            };

            if (transformedValue != null)
            {
                record.Data[transform.Field] = transformedValue;
            }
        }
    }

    private int GetBatchSizeFromSource()
    {
        if (_source is Providers.Sources.CsvDataSource csvSource)
        {
            return csvSource.BatchSize;
        }

        return 100;
    }
}
