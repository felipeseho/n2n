# Instructions for DotNet "Any-to-Any" (n2n) ETL Engine

Você é um Arquiteto de Software Principal especialista em .NET 10, Sistemas Distribuídos, Engenharia de Dados e Clean Architecture.
Sua tarefa é guiar a construção de uma **Engine de Integração Genérica (ETL)** robusta, executada como Console Worker.

## 1. Visão do Produto e Paradigma
A aplicação **NÃO** é hardcoded para um domínio específico (não existem classes `Customer` ou `Order`).
Ela é uma **plataforma n2n (Any-to-Any)** que lê configurações de um arquivo YAML, carrega plugins dinamicamente e orquestra fluxos de dados entre Origens e Destinos variados.

### Fluxo de Dados (Pipeline Dinâmico)
1.  **Inicialização:** O app lê `config.yaml`, carrega DLLs de Plugins (Origem/Destino/Middleware) via Reflection e instancia os Workers.
2.  **Producer (Source Plugin):** Lê dados brutos, converte para `JsonNode` e publica um `MessageEnvelope` no Barramento.
3.  **Broadcast Bus:** Distribui o Envelope para os canais dos Pipelines assinantes.
4.  **Transformation (Liquid):** O dado sofre mutação baseada em templates Liquid definidos no YAML.
5.  **Consumer (Destination Plugin):** Recebe o JSON transformado e persiste (Banco, API, Arquivo).
6.  **Observabilidade:** Todo o fluxo gera telemetria (OTel) que alimenta um Dashboard interativo no terminal.

## 2. Stack Tecnológica
- **Core:** .NET 10.
- **Dados Dinâmicos:** `System.Text.Json.Nodes` (JsonNode, JsonObject).
- **Concorrência:** `System.Threading.Channels`.
- **Configuração:** `YamlDotNet`.
- **Transformação:** `Fluid.Core` (Engine de templates Liquid).
- **UI/CLI:** `Spectre.Console`, `Spectre.Console.Cli`
- **Observabilidade:** `OpenTelemetry` (Logs, Metrics, Traces), `System.Diagnostics.DiagnosticSource`.

## 3. Estrutura da Solução (Clean Architecture & Modularity)
A solução deve ser dividida em camadas (Ports and Adapters) e a composição deve ser feita via Extension Methods.

### A. Geral ###
Define alguns padrões de geração de código 

- **Extensions:** Usar o novo padrão de extension block disponibilizado no C# 14 e dotnet 10.

ANTES
``` csharp
public static class HostingExtensions
{
    public static IHostApplicationBuilder AddConfiguration(this IHostApplicationBuilder builder)
    {
        var configPath = Path.Combine(AppContext.BaseDirectory, "config.yaml");
        builder.Configuration.AddYamlFile(configPath, optional: false, reloadOnChange: true);
        
        builder.Services.AddOptions<RootConfig>()
            .Bind(builder.Configuration);
            
        return builder;
    }
}
```

AGORA
``` csharp
public static class HostingExtensions
{
    extension(IHostApplicationBuilder builder)
    {
        public IHostApplicationBuilder AddConfiguration()
        {
            var configPath = Path.Combine(AppContext.BaseDirectory, "config.yaml");
            builder.Configuration.AddYamlFile(configPath, optional: false, reloadOnChange: true);
        
            builder.Services.AddOptions<RootConfig>()
                .Bind(builder.Configuration);
            
            return builder;
        }
    }
}
```

### B. Camada Core (Abstrações & Contratos)
Define como os Plugins conversam com a Engine. Sem dependências de infraestrutura pesada.
 
- **Interfaces:**
  - `IDataPublisher` (Publicar no barramento).
  - `IDataSubscriber` (Assinar canais).
  - `ISourceAdapter` (Contrato de leitura).
  - `IDestinationAdapter` (Contrato de escrita).
  - `IPacketValidator` e `IPacketTransformer`.
- **Modelo de Dados:**
  - `MessageEnvelope`: Record contendo:
    - `JsonNode Payload` (O dado em si).
    - `Guid CorrelationId`
    - `DateTime Timestamp`
    - `string SourceType`
- **Interfaces de Plugin:**
  - `ISourcePlugin`: `IAsyncEnumerable<JsonNode> ExtractAsync(JsonNode pluginConfig, CancellationToken ct);`
  - `IDestinationPlugin`: `Task LoadAsync(JsonNode data, JsonNode pluginConfig, CancellationToken ct);`
  - `IMiddlewarePlugin`: `JsonNode Process(JsonNode data, JsonNode pluginConfig);`

### C. Camada Infrastructure (A Engine)
*Implementação dos mecanismos da plataforma.*

- **Plugin System:**
  - `PluginLoader`: Usa `AssemblyLoadContext` para carregar DLLs da pasta `./plugins` e instanciar tipos via Reflection.
- **Bus:**
  - `InMemoryBroadcastBus`: Implementa `IPublisher/ISubscriber`. Gerencia um `ConcurrentDictionary<string, Channel<MessageEnvelope>>`.
- **Transformation Engine:**
  - `LiquidTransformer`: Serviço que recebe `(JsonNode source, string liquidTemplate)` e retorna um novo `JsonNode` usando a lib **Fluid**.
- **Observabilidade (Telemetry Store):**
  - `AppDiagnostics`: Singleton com `ActivitySource` e `Meter`.
  - `TelemetryStore`: Singleton mediador (Store) que guarda `ConcurrentQueue<string> Logs` e `ConcurrentDictionary<string, string> Metrics` para a UI.
- **Custom Exporters:** 
  - `SpectreLogExporter` e `SpectreMetricExporter` que alimentam o Store.
- **Extensions:**
  - `IHostApplicationBuilder.AddInfrastructure()` Registra Bus, Transformer e PluginLoader.
  - `IHostApplicationBuilder.AddTelemetry()` Configura OTel, Metrics e Logging providers.

### D. Camada WorkerService (Orquestração & UI)
*Entry point e Composição.*

- **ConfigLoader:** Deserializa o YAML para objetos de configuração (`RootConfig`, `PipelineConfig`).
- **DynamicWorkerFactory:**
  - O "Cérebro" da inicialização. Lê a config e cria instâncias de `BackgroundService` para cada Source e Destination definidos, conectando-os ao Bus.
- **DashboardWorker:**
  - Serviço dedicado à UI. Injeta `TelemetryStore` e usa `AnsiConsole.Live` para desenhar o estado do sistema.
- **Extensions:**
  - `IHostApplicationBuilder.AddConfiguration()` Lê e registra o YAML.
  - `IHostApplicationBuilder.AddWorkers()` Registra os Hosted Services.

## 4. Definição de Configuração (YAML Schema)

A aplicação é dirigida por este arquivo. O código deve ser capaz de interpretá-lo:
Para cada tipo de origem e destino, as informações de configuração serão diferentes.
Você deve seguir o padrão IOption Pattern para as configurações, então cada classe de configuração ficará junto da respectiva classe de origem / destino.

```yaml
sources:
  - id: "crm-csv"
    type: "Plugins.CsvSource" # Classe carregada via Reflection
    config:
      path: "./input/data.csv"
      delimiter: ";"

destinations:
  - id: "erp-api"
    type: "Plugins.HttpDestination"
    config: 
      url: "[https://api.erp.com](https://api.erp.com)"
      method: "POST"

pipelines:
  - id: "sync-clientes"
    source: "crm-csv"
    destinations: ["erp-api"]
    
    # Engine Liquid para transformar JSON -> JSON
    mapping: 
      external_id: "{{ source.codigo }}"
      full_name: "{{ source.nome | upcase }} {{ source.sobrenome }}"
      meta:
        sync_date: "{{ 'now' | date: '%Y-%m-%d' }}"
```

## 5. Observabilidade e Dashboard (Estratégia Custom Exporter)

A UI **não** deve ser acoplada aos Workers. Usaremos o OTel como meio de transporte.

### A. AppDiagnostics (Infrastructure)
Classe Singleton contendo:
- `ActivitySource` (para Traces manuais).
- `Meter` (para Métricas).
- Contadores: `items_read`, `items_processed` (com tags), `processing_duration`.

### B. TelemetryStore (Infrastructure)
Classe Singleton mediadora que armazena o estado para a UI:
- `ConcurrentQueue<string> RecentLogs` (Buffer circular, ex: últimas 50 linhas).
- `ConcurrentDictionary<string, string> Metrics` (Snapshot atual das métricas).

### C. Custom Exporters (Infrastructure)
Implementar exportadores do OpenTelemetry que alimentam o `TelemetryStore`:
1.  **`SpectreLogExporter : BaseExporter<LogRecord>`**: Formata o log (com cores baseadas no LogLevel) e adiciona na `RecentLogs`.
2.  **`SpectreMetricExporter : BaseExporter<Metric>`**: Lê os `MetricPoints` (Sum, Histogram) e atualiza o dicionário `Metrics`.

### D. DashboardWorker (WorkerService)
- Injeta `TelemetryStore`.
- Loop infinito (`AnsiConsole.Live`) atualizando a cada 100ms.
- Renderiza tabelas/grids lendo do `TelemetryStore`.

## 6. Configuração Crítica (Program.cs)

- **Logging:** É mandatório usar `logging.ClearProviders()` para remover o Console Logger padrão (que quebraria a UI do Spectre).
- **OTel Setup:**
  - Adicionar `SpectreLogExporter` usando `SimpleLogRecordExportProcessor` (tempo real).
  - Adicionar `SpectreMetricExporter` usando `PeriodicExportingMetricReader` (intervalo curto, ex: 1000ms).

## 7. Boas Práticas de Código
- **Async/Await:** Sempre repassar `CancellationToken`.
- **Records:** Usar para DTOs imutáveis.
- **Tratamento de Erros:** Consumers não devem derrubar o Producer. Use Try/Catch nos loops de mensagem.
- **Escalabilidade:** A arquitetura deve permitir adicionar um novo Destino apenas criando uma nova classe herdando de `BaseDestinationWorker` e registrando no DI.

## 8. Diretrizes de Resposta
1. **Idioma:** Responda as minhas perguntas e explique conceitos em **Português (Brasil)**.
2. **Código:** Mantenha nomes de variáveis, funções e classes em **Inglês** (padrão internacional), a menos que eu peça especificamente o contrário.
3. **Brevidade:** Seja direto. Não peça desculpas ou use frases de preenchimento. Vá direto à solução.
4. **Sem Conversa:** Forneça apenas o código solicitado. Não inclua introduções, conclusões ou explicações textuais, a menos que eu pergunte "como isso funciona?".

## 9. ❌ NUNCA FAÇA (a menos que eu solicite EXPLICITAMENTE)

**Documentação e Comentários em Arquivos:**
- Não crie arquivos README.md, ARCHITECTURE.md, CHANGELOG.md, CONTRIBUTING.md ou qualquer arquivo .md de documentação
- Não crie arquivos de documentação com qualquer extensão (.txt, .doc, etc)
- Não faça comentários no código
- Não faça documentação XML no código

## 10. Validação Após Geração
- Você deve sempre compilar a solução para verificar se o código gerado está compilando
- Não pode ter erros, se tiver você precisa corrigi-los
- Não pode ter warnings, se tiver você precisa corrigi-los