# Changelog

Todas as mudanças notáveis neste projeto serão documentadas neste arquivo.

O formato é baseado em [Keep a Changelog](https://keepachangelog.com/pt-BR/1.0.0/),
e este projeto adere ao [Versionamento Semântico](https://semver.org/lang/pt-BR/).

## [0.7.0] - 2024-11-19

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

## [0.6.0] - 2024-11-19

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

## [0.5.0] - 2024-11-19

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

## [0.4.0] - 2024-11-19

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

## [0.3.0] - 2024-11-19

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

## [0.2.0] - 2024-11-19

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

## [0.1.0] - 2024-11-19

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
