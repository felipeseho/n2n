# Changelog

Todas as mudanças notáveis neste projeto serão documentadas neste arquivo.

O formato é baseado em [Keep a Changelog](https://keepachangelog.com/pt-BR/1.0.0/),
e este projeto adere ao [Versionamento Semântico](https://semver.org/lang/pt-BR/).

## 0.12.0 - 2025-12-15

### ✨ Adicionado

#### 📁 Processamento de Múltiplos Arquivos

**Nova funcionalidade: processar vários arquivos CSV em sequência.**

Agora você pode configurar uma lista de arquivos para serem processados em ordem, cada um com seu próprio checkpoint e log.

**Configuração com `inputPaths` (novo):**
```yaml
file:
  inputPaths:
    - "data/arquivo1.csv"
    - "data/arquivo2.csv"
    - "data/arquivo3.csv"
  # ... demais configurações
```

**Características:**
- ✅ Cada arquivo tem checkpoint e log individual
- ✅ Processamento sequencial na ordem configurada
- ✅ Arquivos não encontrados são registrados e pulados
- ✅ Nomenclatura automática: `checkpoint_{execId}_{fileName}.json`
- ✅ Retrocompatibilidade total com `inputPath` (singular)

**Exemplo de arquivos gerados:**
```
checkpoints/
  checkpoint_abc123_arquivo1.json
  checkpoint_abc123_arquivo2.json
  checkpoint_abc123_arquivo3.json
logs/
  process_abc123_arquivo1.log
  process_abc123_arquivo2.log
  process_abc123_arquivo3.log
```

📖 **Documentação completa**: [MULTIPLE-FILES.md](MULTIPLE-FILES.md)

### 🔧 Modificado

- **FileConfiguration**: Adicionada propriedade `InputPaths` (lista) e método `GetInputFiles()`
- **ExecutionPaths**: Adicionada propriedade `CurrentInputFile` para rastrear arquivo atual
- **ConfigurationService**: `GenerateExecutionPaths()` agora aceita parâmetro opcional `inputFile`
- **ConfigurationService**: Validação não verifica mais existência de arquivos (tratado durante processamento)
- **CheckpointService**: `SaveCheckpointAsync()` agora aceita parâmetro opcional `errorMessage`
- **CsvProcessorService**: `ProcessCsvFileAsync()` agora itera sobre múltiplos arquivos
- **CsvProcessorService**: Novo método privado `ProcessSingleCsvFileAsync()` para processar arquivo individual

### 🛡️ Comportamento com Arquivos Não Encontrados

Quando um arquivo da lista não é encontrado:
1. ⚠️  Mensagem de aviso é exibida no console
2. 📝 Log de erro é criado (HTTP 404)
3. 💾 Checkpoint é salvo com `errorCount = 1` e mensagem descritiva
4. ➡️  Processamento continua para o próximo arquivo

---

## 0.11.0 - 2025-12-06

### ⚠️ BREAKING CHANGES

#### 🔍 Formato de Filtros Simplificado

**Removida a compatibilidade com o formato antigo de filtros (`filter:` singular).**

Agora todos os filtros devem usar o formato `filters:` (plural), que suporta múltiplos filtros na mesma coluna.

**❌ Formato antigo (não funciona mais):**
```yaml
- column: "Status"
  type: "string"
  filter:
    operator: "Equals"
    value: "ativo"
```

**✅ Formato novo (obrigatório):**
```yaml
- column: "Status"
  type: "string"
  filters:  # ← Sempre usar "filters" (plural)
    - operator: "Equals"
      value: "ativo"
```

**✅ Múltiplos filtros na mesma coluna:**
```yaml
- column: "Status"
  type: "string"
  filters:
    - operator: "NotEquals"
      value: "cancelado"
    - operator: "NotEquals"
      value: "inativo"
    - operator: "NotEquals"
      value: "suspenso"
```

**Benefícios:**
- ✅ Código mais simples e fácil de manter
- ✅ Suporte nativo a múltiplos filtros por coluna
- ✅ Lógica AND entre todos os filtros
- ✅ Dashboard aprimorado mostrando todos os filtros ativos

### 🔧 Melhorias

- **Dashboard:** Agora exibe o total de filtros e em quantas colunas estão aplicados
- **Dashboard:** Mostra até 5 filtros individuais com indicação de quantos filtros adicionais existem
- **Documentação:** Atualizada para refletir apenas o novo formato

## 0.10.0 - 2025-11-25

### ✨ Novos Recursos

#### 🔍 Múltiplos Filtros na Mesma Coluna

Agora é possível aplicar múltiplos filtros na mesma coluna do CSV, permitindo lógicas de filtragem mais complexas e refinadas.

**Formato antigo (ainda funciona):**
```yaml
- column: "Status"
  type: "string"
  filter:
    operator: "Equals"
    value: "ativo"
```

**Formato novo (múltiplos filtros):**
```yaml
- column: "Status"
  type: "string"
  filters:  # ← Note o "s" no final
    - operator: "NotEquals"
      value: "cancelado"
    - operator: "NotEquals"
      value: "inativo"
    - operator: "NotEquals"
      value: "suspenso"
```

**Benefícios:**
- ✅ Filtros mais complexos sem precisar de múltiplas colunas
- ✅ Lógica AND entre múltiplos filtros da mesma coluna
- ✅ Retrocompatível com configurações antigas
- ✅ Reduz necessidade de pré-processamento de dados

**Exemplo de uso:**
```yaml
file:
  columns:
    # Processar apenas registros que NÃO sejam "cancelado", "inativo" ou "suspenso"
    - column: "Status"
      type: "string"
      filters:
        - operator: "NotEquals"
          value: "cancelado"
          caseInsensitive: true
        - operator: "NotEquals"
          value: "inativo"
          caseInsensitive: true
        - operator: "NotEquals"
          value: "suspenso"
          caseInsensitive: true
```

Para mais detalhes, veja a [documentação de filtros](FILTERS.md#exemplo-6-múltiplos-filtros-na-mesma-coluna-novo).

---

#### 🎨 Múltiplas Transformações em Sequência

Agora é possível aplicar múltiplas transformações em sequência no mapeamento de endpoints, onde o resultado de uma transformação é passado como entrada para a próxima.

**Formato antigo (ainda funciona):**
```yaml
- attribute: "name"
  csvColumn: "Nome"
  transform: "uppercase"
```

**Formato novo (múltiplas transformações):**
```yaml
- attribute: "name"
  csvColumn: "Nome"
  transforms:  # ← Note o "s" no final
    - "trim"           # 1º Remove espaços nas extremidades
    - "title-case"     # 2º Converte para Title Case
    - "remove-accents" # 3º Remove acentos
```

**Benefícios:**
- ✅ Pipelines de transformação complexos
- ✅ Maior controle sobre normalização de dados
- ✅ Reduz necessidade de transformações customizadas
- ✅ Retrocompatível com configurações antigas
- ✅ Mais de 20 transformações podem ser combinadas

**Exemplo de pipeline:**
```
Entrada: "  JOÃO da SILVA  "
↓ trim: "JOÃO da SILVA"
↓ title-case: "João Da Silva"
↓ remove-accents: "Joao Da Silva"
```

**Exemplo de uso completo:**
```yaml
endpoints:
  - name: "api-users"
    mapping:
      # Email normalizado
      - attribute: "email"
        csvColumn: "Email"
        transforms:
          - "trim"
          - "lowercase"
      
      # Telefone limpo e formatado
      - attribute: "phone"
        csvColumn: "Telefone"
        transforms:
          - "remove-all-spaces"
          - "remove-non-numeric"
          - "format-phone-br"
      
      # CPF limpo e formatado
      - attribute: "document"
        csvColumn: "CPF"
        transforms:
          - "trim"
          - "remove-non-numeric"
          - "format-cpf"
      
      # Slug para URL
      - attribute: "slug"
        csvColumn: "Nome"
        transforms:
          - "lowercase"
          - "remove-accents"
          - "slugify"
```

Para mais detalhes, veja a [documentação de transformações](TRANSFORMATIONS.md#-encadeamento-de-transformações-novo).

---

### 🔄 Retrocompatibilidade

Todas as alterações são **100% retrocompatíveis**:

- ✅ Configurações antigas com `filter` (singular) continuam funcionando
- ✅ Configurações antigas com `transform` (singular) continuam funcionando
- ✅ Não é necessário alterar configurações existentes
- ✅ É possível misturar formatos antigo e novo no mesmo arquivo

---

### 📚 Documentação Atualizada

- ✅ [FILTERS.md](FILTERS.md) - Exemplos de múltiplos filtros
- ✅ [TRANSFORMATIONS.md](TRANSFORMATIONS.md) - Exemplos de múltiplas transformações
- ✅ [config-example-multiple-filters-transforms.yaml](../src/config-example-multiple-filters-transforms.yaml) - Arquivo de exemplo completo

---

## [0.9.1] - 2025-11-24

### Adicionado

- **Nova transformação `empty-to-null`**: Converte valores vazios (strings em branco ou apenas espaços) em `null`
  - Útil para APIs que diferenciam valores vazios de valores nulos
  - Aplica trim antes de verificar se está vazio
  - Integrado ao sistema de transformações existente

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
