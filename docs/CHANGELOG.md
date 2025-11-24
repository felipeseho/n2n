# Changelog

Todas as mudanças notáveis neste projeto serão documentadas neste arquivo.

O formato é baseado em [Keep a Changelog](https://keepachangelog.com/pt-BR/1.0.0/),
e este projeto adere ao [Versionamento Semântico](https://semver.org/lang/pt-BR/).

## [0.9.0] - 2025-11-24

### Adicionado

- **Modo Fallback para Terminais Pequenos**: Sistema adaptativo que detecta automaticamente o tamanho do terminal
  - Validação automática de dimensões mínimas (80x25)
  - Modo texto simples para terminais com altura inferior a 25 linhas
  - Resumo compacto mostrando métricas essenciais: progresso, sucessos, erros, tempo e velocidade
  - Exibição do último log para acompanhamento

### Melhorado

- **Robustez do Dashboard**: Sistema de renderização mais resiliente e tolerante a falhas
  - Tratamento robusto de exceções `ArgumentOutOfRangeException`
  - Proteção contra valores nulos em todos os painéis
  - Try-catch em métodos `StartLiveDashboard()` e `UpdateOnce()`
  - Fallback automático para modo simples em caso de erros de renderização
  - Validação de largura de barras de progresso (evita valores negativos)
  - Dashboard funcional em qualquer tamanho de terminal

- **Experiência do Usuário**:
  - Mensagens claras quando o terminal é muito pequeno
  - Orientação sobre dimensões mínimas recomendadas
  - Continuidade do processamento mesmo com dashboard desabilitado
  - Atualização menos frequente no modo texto (2s vs 500ms) para melhor legibilidade

### Corrigido

- Crash `ArgumentOutOfRangeException` ao renderizar dashboard em terminais pequenos
- Erro ao calcular largura de barras de progresso com valores negativos
- Falhas de renderização quando terminal é redimensionado durante execução
- NullReferenceException em painéis quando métricas ainda não foram inicializadas

### Técnico

- Adicionado método `ValidateTerminalSize()` no `DashboardService`
- Adicionado método `ShowSimpleSummary()` para modo texto compacto
- Proteção com `Math.Max()` e `Math.Min()` em cálculos de dimensões
- Verificações de nullable em todas as propriedades de métricas e configurações

## [0.8.2] - 2025-11-22

### Modificado

- **Documentação Profissional**: Reestruturação completa da documentação seguindo padrões de projetos open source
  - README.md reformulado com visual moderno e organização profissional
  - Header centralizado com badges informativos
  - Estrutura clara: Sobre, Interface, Funcionalidades, Requisitos, Instalação, Quick Start, Comandos, Exemplos
  - Seções organizadas com emojis estratégicos para navegação visual

- **Documentação Interna Padronizada**: Todos os arquivos da pasta `docs/` atualizados
  - Arquivos renomeados para inglês (QUICKSTART.md, CLI-ARGUMENTS.md, EXAMPLES.md, TRANSFORMATIONS.md, FILTERS.md)
  - Conteúdo mantido em português
  - Padrão visual consistente em todos os documentos
  - Headers centralizados, seções bem definidas, tabelas de referência
  - Links de navegação interna e externa
  - Footer profissional com "Voltar ao topo"

- **Melhorias na Organização**:
  - Removidos arquivos antigos (ARGUMENTOS.md, EXEMPLOS.md, TRANSFORMACOES.md, README-FILTROS.md)
  - Todas as referências atualizadas no README principal
  - Documentação mais acessível e fácil de navegar

## [0.8.1] - 2025-11-22

### Modificado

- **Rebranding do projeto**: Renomeado de "n2n" para **"n2n"** (Any to Any)
- Novo título: "n2n: De qualquer origem para qualquer destino"
- Nova descrição: "A ferramenta definitiva para integrar seus dados. Conecte Arquivos, APIs e Bancos de Dados em fluxos unificados, sem complexidade."
- Atualização da identidade visual e posicionamento do produto

## [0.8.0] - 2025-11-20

### Adicionado

- **Dashboard em Tempo Real**: Interface interativa com atualização automática a cada 500ms
- Layout organizado em 4 seções principais:
    - ⚙️ **Importação**: Execution ID, Checkpoint, Start Line, Batch Size, Max Lines
    - 📄 **Arquivo**: Nome, tamanho (formatado), total de linhas, filtros aplicados
    - 🌐 **Endpoint**: URL, método HTTP, timeout, número de retries
    - 📊 **Progresso**: Barra visual, estatísticas, tempo decorrido/estimado, velocidade, performance HTTP
- Rodapé com distribuição de códigos HTTP em tempo real
- Métrica de linhas filtradas no dashboard
- Campo `ExecutionId` em `ExecutionPaths`
- Novo serviço `DashboardService` para gerenciar a exibição
- Documentação completa do dashboard em `DASHBOARD.md`

### Melhorado

- Substituída barra de progresso simples por dashboard interativo completo
- Métricas agora incluem linhas filtradas separadamente
- Melhor visualização de performance HTTP em tempo real
- Cores e emojis para facilitar identificação rápida de informações
- Interface mais profissional e informativa

### Modificado

- `CsvProcessorService`: Integrado com `DashboardService`
- `MetricsService`: Adicionado método `RecordFilteredLines()`
- `ProcessingMetrics`: Adicionada propriedade `FilteredLines`
- Removidas mensagens de progresso intermediárias em favor do dashboard

## [0.7.0] - 2025-11-19

### Adicionado

- Sistema de múltiplos endpoints nomeados
- Suporte a seleção dinâmica de endpoint via coluna CSV (`endpointColumnName`)
- Configuração de endpoint padrão (`defaultEndpoint`)
- Seleção de endpoint via argumento CLI (`--endpoint-name`)
- Possibilidade de configurar múltiplos endpoints em `config.yaml`

### Melhorado

- Estrutura de configuração refatorada para suportar endpoints nomeados
- Documentação atualizada com exemplos de múltiplos endpoints
- Interface de visualização mostrando qual endpoint está sendo usado

## [0.6.0] - 2025-11-19

### Adicionado

- Interface visual moderna com Spectre.Console
- Banner ASCII art estilizado
- Dashboard de métricas em tempo real
- Barras de progresso animadas
- Tabelas formatadas para configurações
- Spinners animados durante operações
- Cores temáticas para diferentes tipos de mensagens
- Visualização de configurações antes do processamento

### Melhorado

- Experiência de usuário significativamente aprimorada
- Feedback visual durante processamento
- Exibição de estatísticas e métricas

## [0.5.0] - 2025-11-19

### Adicionado

- Sistema de filtros de dados para processar apenas linhas específicas
- Operadores de filtro: `Equals`, `NotEquals`, `Contains`, `NotContains`
- Filtros configuráveis por coluna no arquivo YAML
- Opção de filtros case-sensitive/case-insensitive
- Serviço dedicado para processamento de filtros (`FilterService`)
- Estatísticas de linhas filtradas vs processadas
- Documentação completa de filtros em `README-FILTROS.md`
- Arquivo de exemplo `exemplo-filtros.csv`

### Melhorado

- Logs mostram quantidade de linhas filtradas
- Métricas incluem informações sobre filtros aplicados

## [0.4.0] - 2025-11-19

### Adicionado

- Sistema completo de transformações de dados
- 20+ transformações disponíveis:
    - Transformações de texto: `uppercase`, `lowercase`, `capitalize`, `title-case`
    - Limpeza de dados: `trim`, `remove-spaces`, `remove-all-spaces`, `remove-accents`
    - Formatações brasileiras: `format-cpf`, `format-cnpj`, `format-phone-br`, `format-cep`
    - Outras transformações: `slugify`, `base64-encode`, `url-encode`, `reverse`
    - Transformação de datas: `date-format:FORMATO`
- Propriedade `transform` no mapeamento da API
- Utilitário dedicado `DataTransformer`
- Documentação completa em `TRANSFORMACOES.md`

### Melhorado

- Processamento de dados antes do envio para API
- Flexibilidade no tratamento de dados do CSV

## [0.3.0] - 2025-11-19

### Adicionado

- Sistema de checkpoints com UUID por execução
- Argumento `--execution-id` / `--exec-id` para continuar execução existente
- Checkpoints únicos por execução em `checkpoints/checkpoint_{uuid}.json`
- Logs únicos por execução em `logs/process_{uuid}.log`
- Geração automática de UUID para novas execuções
- Modo dry-run com argumento `--dry-run` ou `--test`
- Validação de execuções sem fazer requisições reais

### Melhorado

- Rastreabilidade de execuções
- Capacidade de retomar processamento específico
- Testes sem impacto em APIs de produção
- Organização de logs e checkpoints

## [0.2.0] - 2025-11-19

### Adicionado

- Interface CLI completa com Spectre.Console.Cli
- Argumentos de linha de comando para todas as configurações principais:
    - `--config` / `-c`: arquivo de configuração
    - `--input` / `-i`: arquivo CSV de entrada
    - `--batch-lines` / `-b`: tamanho do lote
    - `--log-dir` / `-l`: diretório de logs
    - `--delimiter` / `-d`: delimitador CSV
    - `--start-line` / `-s`: linha inicial
    - `--max-lines` / `-n`: limite de linhas
    - `--verbose` / `-v`: modo verboso
- Comando `--help` para exibir todas as opções
- Validação de argumentos com Spectre.Console.Cli
- Documentação completa em `ARGUMENTOS.md`

### Melhorado

- Flexibilidade de configuração via CLI
- Possibilidade de sobrescrever configurações do YAML
- Experiência de uso mais intuitiva

## [0.1.0] - 2025-11-19

### Adicionado

- Processamento de arquivos CSV em lotes
- Envio de dados para API REST via POST/PUT
- Validação de dados com regex
- Validação de formatos de data
- Processamento paralelo
- Sistema de logs de erros com detalhes (linha, HTTP code, mensagem)
- Sistema de checkpoints para retomar processamento
- Suporte a atributos aninhados no payload (ex: `address.street`)
- Configuração via arquivo YAML
- Autenticação Bearer Token
- Headers HTTP customizados
- Retry automático em falhas
- Rate limiting (max requests per second)
- Mapeamento flexível CSV → API
- Valores fixos no payload (`fixedValue`)
- Documentação básica (README, QUICKSTART, EXEMPLOS)

### Técnico

- .NET 10
- YamlDotNet para configuração
- CsvHelper para parsing CSV
- Arquitetura em camadas (Models, Services, Utils)
- Injeção de dependências
- Serviços especializados:
    - `CsvProcessorService`: processamento principal
    - `ApiClientService`: comunicação HTTP
    - `ValidationService`: validação de dados
    - `CheckpointService`: gerenciamento de checkpoints
    - `LoggingService`: registro de erros
    - `ConfigurationService`: carregamento de configuração
    - `MetricsService`: coleta de métricas
