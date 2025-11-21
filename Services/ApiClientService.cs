using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using n2n.Models;
using n2n.Utils;

namespace n2n.Services;

/// <summary>
///     Serviço para comunicação com API REST
/// </summary>
public class ApiClientService
{
    private readonly LoggingService _loggingService;
    private readonly MetricsService _metricsService;
    private readonly AppExecutionContext _context;
    private SemaphoreSlim? _rateLimiter;
    private Timer? _rateLimitTimer;

    public ApiClientService(LoggingService loggingService, MetricsService metricsService, AppExecutionContext context)
    {
        _loggingService = loggingService;
        _metricsService = metricsService;
        _context = context;

        // O rate limiting será configurado na primeira chamada, quando o ActiveEndpoint já estiver disponível
    }

    private void EnsureRateLimiterInitialized()
    {
        // Configurar rate limiting se especificado e ainda não foi inicializado
        if (_rateLimiter == null && 
            _context.ActiveEndpoint != null && 
            _context.ActiveEndpoint.MaxRequestsPerSecond.HasValue && 
            _context.ActiveEndpoint.MaxRequestsPerSecond.Value > 0)
        {
            _rateLimiter = new SemaphoreSlim(0, _context.ActiveEndpoint.MaxRequestsPerSecond.Value);
            _rateLimitTimer = new Timer(_ =>
            {
                try
                {
                    // Liberar tokens a cada segundo
                    var currentCount = _rateLimiter.CurrentCount;
                    var tokensToRelease = _context.ActiveEndpoint.MaxRequestsPerSecond.Value - currentCount;
                    if (tokensToRelease > 0) _rateLimiter.Release(tokensToRelease);
                }
                catch (SemaphoreFullException)
                {
                    // Ignorar se já estiver cheio
                }
            }, null, 0, 1000);
        }
    }

    /// <summary>
    ///     Cria e configura o HttpClient
    /// </summary>
    public HttpClient CreateHttpClient()
    {
        var httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(_context.ActiveEndpoint.RequestTimeout)
        };

        // Configurar headers customizados
        // Nota: Content-Type e outros headers de conteúdo devem ser configurados no HttpContent, não no HttpClient
        if (_context.ActiveEndpoint.Headers.Count > 0)
        {
            var contentHeaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Content-Type", "Content-Length", "Content-Encoding", "Content-Language",
                "Content-Location", "Content-MD5", "Content-Range", "Expires", "Last-Modified"
            };

            foreach (var header in _context.ActiveEndpoint.Headers)
                // Ignorar headers de conteúdo (eles serão adicionados no HttpContent quando a requisição for feita)
                if (!contentHeaders.Contains(header.Key))
                    httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
        }

        return httpClient;
    }

    /// <summary>
    ///     Processa um lote de registros
    /// </summary>
    public async Task<int> ProcessBatchAsync(HttpClient httpClient, List<CsvRecord> batch, string[] headers)
    {
        // Processar em paralelo para melhor performance
        var tasks = batch.Select(record =>
            ProcessRecordAsync(httpClient, record, headers));
        var results = await Task.WhenAll(tasks);

        return results.Count(r => !r);
    }

    /// <summary>
    ///     Processa um único registro
    /// </summary>
    private async Task<bool> ProcessRecordAsync(HttpClient httpClient, CsvRecord record, string[] headers)
    {
        // Garantir que o rate limiter está inicializado
        EnsureRateLimiterInitialized();
        
        // Aguardar rate limiter se configurado
        if (_rateLimiter != null) await _rateLimiter.WaitAsync();

        try
        {
            // Determinar qual endpoint usar
            var endpointName = _context.CommandLineOptions.EndpointName; // Prioridade 1: Argumento linha de comando

            // Prioridade 2: Coluna CSV (se configurada)
            if (string.IsNullOrWhiteSpace(endpointName) && !string.IsNullOrWhiteSpace(_context.Configuration.EndpointColumnName))
                if (record.Data.TryGetValue(_context.Configuration.EndpointColumnName, out var csvEndpointName))
                    endpointName = csvEndpointName;

            // Selecionar configuração de API apropriada
            var apiConfig = GetEndpointConfiguration(endpointName);

            // Construir payload da API
            var payload = PayloadBuilder.BuildApiPayload(record, apiConfig.Mapping);
            var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
            {
                WriteIndented = false
            });

            // Modo dry run: apenas simula o envio
            if (_context.IsDryRun)
            {
                var endpointInfo = string.IsNullOrWhiteSpace(endpointName) ? "default" : endpointName;
                Console.WriteLine($"[DRY RUN] Linha {record.LineNumber} [endpoint: {endpointInfo}]: {json}");
                return true;
            }

            return await SendWithRetryAsync(httpClient, apiConfig, json, record, headers);
        }
        catch (Exception ex)
        {
            await _loggingService.LogError(_context.ExecutionPaths.LogPath, record, 500, ex.Message, headers);
            return false;
        }
    }

    /// <summary>
    ///     Retorna a configuração do endpoint apropriado
    /// </summary>
    private NamedEndpoint GetEndpointConfiguration(string? endpointName)
    {
        // Se não há nome de endpoint especificado, usar endpoint padrão configurado
        if (string.IsNullOrWhiteSpace(endpointName))
        {
            if (!string.IsNullOrWhiteSpace(_context.Configuration.DefaultEndpoint))
                endpointName = _context.Configuration.DefaultEndpoint;
            else if (_context.Configuration.Endpoints.Count == 1)
                // Se há apenas um endpoint, usar ele
                return _context.Configuration.Endpoints[0];
            else
                throw new InvalidOperationException(
                    "Nome do endpoint não especificado. Use --endpoint-name, configure 'endpointColumnName' no CSV, " +
                    "ou defina 'defaultEndpoint' na configuração. " +
                    $"Endpoints disponíveis: {string.Join(", ", _context.Configuration.Endpoints.Select(e => e.Name))}");
        }

        // Buscar endpoint pelo nome
        var endpoint = _context.Configuration.Endpoints.FirstOrDefault(e =>
            e.Name.Equals(endpointName, StringComparison.OrdinalIgnoreCase));

        if (endpoint == null)
            throw new InvalidOperationException(
                $"Endpoint '{endpointName}' não encontrado na configuração. " +
                $"Endpoints disponíveis: {string.Join(", ", _context.Configuration.Endpoints.Select(e => e.Name))}");

        return endpoint;
    }

    /// <summary>
    ///     Envia requisição com retry policy
    /// </summary>
    private async Task<bool> SendWithRetryAsync(HttpClient httpClient, NamedEndpoint endpointConfig,
        string json, CsvRecord record, string[] headers)
    {
        var attempts = 0;
        Exception? lastException = null;
        var requestTimer = Stopwatch.StartNew();

        while (attempts < endpointConfig.RetryAttempts)
        {
            attempts++;

            try
            {
                // Criar conteúdo da requisição
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // Aplicar Content-Type customizado se configurado
                if (endpointConfig.Headers != null && endpointConfig.Headers.ContainsKey("Content-Type"))
                    content.Headers.ContentType = new MediaTypeHeaderValue(endpointConfig.Headers["Content-Type"]);

                HttpResponseMessage response;
                if (endpointConfig.Method.ToUpper() == "POST")
                    response = await httpClient.PostAsync(endpointConfig.EndpointUrl, content);
                else if (endpointConfig.Method.ToUpper() == "PUT")
                    response = await httpClient.PutAsync(endpointConfig.EndpointUrl, content);
                else
                    throw new NotSupportedException($"Método HTTP '{endpointConfig.Method}' não suportado");

                requestTimer.Stop();

                // Registrar métricas
                _metricsService.RecordResponseTime(requestTimer.ElapsedMilliseconds);
                _metricsService.RecordHttpStatusCode((int)response.StatusCode);

                if (attempts > 1) _metricsService.RecordRetry();

                if (!response.IsSuccessStatusCode)
                {
                    // Se for erro do servidor (5xx) ou timeout, tentar novamente
                    if ((int)response.StatusCode >= 500 || response.StatusCode == HttpStatusCode.RequestTimeout)
                        if (attempts < endpointConfig.RetryAttempts)
                        {
                            Console.WriteLine(
                                $"Tentativa {attempts}/{endpointConfig.RetryAttempts} falhou (HTTP {(int)response.StatusCode}). Aguardando {endpointConfig.RetryDelaySeconds}s...");
                            await Task.Delay(endpointConfig.RetryDelaySeconds * 1000);
                            requestTimer.Restart();
                            continue;
                        }

                    var errorMessage = await response.Content.ReadAsStringAsync();
                    await _loggingService.LogError(_context.ExecutionPaths.LogPath, record, (int)response.StatusCode,
                        errorMessage, headers);
                    _metricsService.RecordError();
                    return false;
                }

                _metricsService.RecordSuccess();
                return true;
            }
            catch (HttpRequestException ex)
            {
                lastException = ex;
                if (attempts < endpointConfig.RetryAttempts)
                {
                    Console.WriteLine(
                        $"Tentativa {attempts}/{endpointConfig.RetryAttempts} falhou ({ex.Message}). Aguardando {endpointConfig.RetryDelaySeconds}s...");
                    await Task.Delay(endpointConfig.RetryDelaySeconds * 1000);
                    requestTimer.Restart();
                }
            }
            catch (TaskCanceledException ex)
            {
                lastException = ex;
                if (attempts < endpointConfig.RetryAttempts)
                {
                    Console.WriteLine(
                        $"Tentativa {attempts}/{endpointConfig.RetryAttempts} timeout. Aguardando {endpointConfig.RetryDelaySeconds}s...");
                    await Task.Delay(endpointConfig.RetryDelaySeconds * 1000);
                    requestTimer.Restart();
                }
            }
        }

        // Todas as tentativas falharam
        await _loggingService.LogError(_context.ExecutionPaths.LogPath, record, 500,
            lastException?.Message ?? "Todas as tentativas falharam", headers);
        _metricsService.RecordError();
        return false;
    }

    public void Dispose()
    {
        _rateLimitTimer?.Dispose();
        _rateLimiter?.Dispose();
    }
}