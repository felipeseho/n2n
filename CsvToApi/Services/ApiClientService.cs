using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using CsvToApi.Models;
using CsvToApi.Utils;

namespace CsvToApi.Services;

/// <summary>
/// Serviço para comunicação com API REST
/// </summary>
public class ApiClientService
{
    private readonly LoggingService _loggingService;
    private readonly MetricsService? _metricsService;
    private readonly SemaphoreSlim? _rateLimiter;
    private readonly Timer? _rateLimitTimer;

    public ApiClientService(LoggingService loggingService, ApiConfiguration apiConfig, MetricsService? metricsService = null)
    {
        _loggingService = loggingService;
        _metricsService = metricsService;
        
        // Configurar rate limiting se especificado
        if (apiConfig.MaxRequestsPerSecond.HasValue && apiConfig.MaxRequestsPerSecond.Value > 0)
        {
            _rateLimiter = new SemaphoreSlim(0, apiConfig.MaxRequestsPerSecond.Value);
            _rateLimitTimer = new Timer(_ =>
            {
                try
                {
                    // Liberar tokens a cada segundo
                    var currentCount = _rateLimiter.CurrentCount;
                    var tokensToRelease = apiConfig.MaxRequestsPerSecond.Value - currentCount;
                    if (tokensToRelease > 0)
                    {
                        _rateLimiter.Release(tokensToRelease);
                    }
                }
                catch (SemaphoreFullException)
                {
                    // Ignorar se já estiver cheio
                }
            }, null, 0, 1000);
        }
    }

    /// <summary>
    /// Cria e configura o HttpClient
    /// </summary>
    public HttpClient CreateHttpClient(ApiConfiguration apiConfig)
    {
        var httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(apiConfig.RequestTimeout)
        };

        // Configurar autenticação
        if (!string.IsNullOrWhiteSpace(apiConfig.AuthToken))
        {
            httpClient.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", apiConfig.AuthToken);
        }

        return httpClient;
    }

    /// <summary>
    /// Processa um lote de registros
    /// </summary>
    public async Task<int> ProcessBatchAsync(HttpClient httpClient, List<CsvRecord> batch, 
        Configuration config, string[] headers, bool dryRun = false)
    {
        // Processar em paralelo para melhor performance
        var tasks = batch.Select(record => ProcessRecordAsync(httpClient, record, config, headers, dryRun));
        var results = await Task.WhenAll(tasks);

        return results.Count(r => !r);
    }

    /// <summary>
    /// Processa um único registro
    /// </summary>
    private async Task<bool> ProcessRecordAsync(HttpClient httpClient, CsvRecord record, 
        Configuration config, string[] headers, bool dryRun = false)
    {
        // Aguardar rate limiter se configurado
        if (_rateLimiter != null)
        {
            await _rateLimiter.WaitAsync();
        }

        try
        {
            // Construir payload da API
            var payload = PayloadBuilder.BuildApiPayload(record, config.Api.Mapping);
            var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions 
            { 
                WriteIndented = false 
            });

            // Modo dry run: apenas simula o envio
            if (dryRun)
            {
                Console.WriteLine($"[DRY RUN] Linha {record.LineNumber}: {json}");
                return true;
            }

            return await SendWithRetryAsync(httpClient, config, json, record, headers);
        }
        catch (Exception ex)
        {
            await _loggingService.LogError(config.File.LogPath, record, 500, ex.Message, headers);
            return false;
        }
    }

    /// <summary>
    /// Envia requisição com retry policy
    /// </summary>
    private async Task<bool> SendWithRetryAsync(HttpClient httpClient, Configuration config, 
        string json, CsvRecord record, string[] headers)
    {
        int attempts = 0;
        Exception? lastException = null;
        var requestTimer = Stopwatch.StartNew();

        while (attempts < config.Api.RetryAttempts)
        {
            attempts++;
            
            try
            {
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                HttpResponseMessage response;
                if (config.Api.Method.ToUpper() == "POST")
                {
                    response = await httpClient.PostAsync(config.Api.EndpointUrl, content);
                }
                else if (config.Api.Method.ToUpper() == "PUT")
                {
                    response = await httpClient.PutAsync(config.Api.EndpointUrl, content);
                }
                else
                {
                    throw new NotSupportedException($"Método HTTP '{config.Api.Method}' não suportado");
                }

                requestTimer.Stop();
                
                // Registrar métricas
                _metricsService?.RecordResponseTime(requestTimer.ElapsedMilliseconds);
                _metricsService?.RecordHttpStatusCode((int)response.StatusCode);
                
                if (attempts > 1)
                {
                    _metricsService?.RecordRetry();
                }

                if (!response.IsSuccessStatusCode)
                {
                    // Se for erro do servidor (5xx) ou timeout, tentar novamente
                    if ((int)response.StatusCode >= 500 || response.StatusCode == System.Net.HttpStatusCode.RequestTimeout)
                    {
                        if (attempts < config.Api.RetryAttempts)
                        {
                            Console.WriteLine($"Tentativa {attempts}/{config.Api.RetryAttempts} falhou (HTTP {(int)response.StatusCode}). Aguardando {config.Api.RetryDelaySeconds}s...");
                            await Task.Delay(config.Api.RetryDelaySeconds * 1000);
                            requestTimer.Restart();
                            continue;
                        }
                    }

                    var errorMessage = await response.Content.ReadAsStringAsync();
                    await _loggingService.LogError(config.File.LogPath, record, (int)response.StatusCode, 
                        errorMessage, headers);
                    _metricsService?.RecordError();
                    return false;
                }

                _metricsService?.RecordSuccess();
                return true;
            }
            catch (HttpRequestException ex)
            {
                lastException = ex;
                if (attempts < config.Api.RetryAttempts)
                {
                    Console.WriteLine($"Tentativa {attempts}/{config.Api.RetryAttempts} falhou ({ex.Message}). Aguardando {config.Api.RetryDelaySeconds}s...");
                    await Task.Delay(config.Api.RetryDelaySeconds * 1000);
                    requestTimer.Restart();
                    continue;
                }
            }
            catch (TaskCanceledException ex)
            {
                lastException = ex;
                if (attempts < config.Api.RetryAttempts)
                {
                    Console.WriteLine($"Tentativa {attempts}/{config.Api.RetryAttempts} timeout. Aguardando {config.Api.RetryDelaySeconds}s...");
                    await Task.Delay(config.Api.RetryDelaySeconds * 1000);
                    requestTimer.Restart();
                    continue;
                }
            }
        }

        // Todas as tentativas falharam
        await _loggingService.LogError(config.File.LogPath, record, 500, 
            lastException?.Message ?? "Todas as tentativas falharam", headers);
        _metricsService?.RecordError();
        return false;
    }

    public void Dispose()
    {
        _rateLimitTimer?.Dispose();
        _rateLimiter?.Dispose();
    }
}

