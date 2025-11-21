# CSV to API - Processador de Arquivos CSV

**Versão**: 0.7.0 | [Changelog](CHANGELOG.md)

Aplicação .NET 10 que processa arquivos CSV em lotes e envia os dados para uma API REST.

## ✨ Interface Visual Moderna com Spectre.Console

Este projeto utiliza a biblioteca [Spectre.Console](https://spectreconsole.net/) para oferecer uma experiência de
console rica e visualmente atraente:

- 🎨 **Banner ASCII Art** estilizado
- 📊 **Dashboard de métricas** em tempo real
- 📈 **Barras de progresso** animadas
- 🎯 **Tabelas formatadas** para configurações e resultados
- 🌈 **Cores temáticas** para diferentes tipos de mensagens
- ⚡ **Spinners animados** durante operações

Veja detalhes completos em [SPECTRE_CONSOLE.md](SPECTRE_CONSOLE.md)

## Funcionalidades

- ✅ Leitura de arquivos CSV grandes em lotes configuráveis
- ✅ Validação de dados com regex e formatos de data
- ✅ **Filtros de dados para processar apenas linhas específicas**
- ✅ **Transformações de dados (20+ transformações disponíveis)**
- ✅ **Múltiplos endpoints nomeados com roteamento dinâmico**
- ✅ Processamento paralelo para alta performance
- ✅ Chamadas HTTP (POST/PUT) para API REST
- ✅ Log de erros com informações detalhadas (linha, HTTP code, mensagem)
- ✅ **Sistema de checkpoints com UUID por execução**
- ✅ **Modo dry-run para testes sem requisições reais**
- ✅ Suporte a atributos aninhados no payload da API (ex: `address.street`)
- ✅ Configuração via arquivo YAML
- ✅ Autenticação Bearer Token e headers customizados
- ✅ **Argumentos de linha de comando para sobrescrever configurações**
- ✅ **Interface visual moderna e interativa com Spectre.Console**
- ✅ Valores fixos e dinâmicos no payload da API

## Requisitos

- .NET 10 SDK
- Arquivo de configuração YAML

## Instalação

```bash
dotnet restore
dotnet build
```

## Uso

### Ajuda e Opções Disponíveis

```bash
dotnet run -- --help
```

### Execução básica (usando config.yaml padrão)

```bash
dotnet run
```

### Execução com arquivo de configuração customizado

```bash
dotnet run -- --config /caminho/para/config.yaml
# ou forma curta
dotnet run -- -c /caminho/para/config.yaml
```

### Sobrescrever configurações via argumentos

```bash
# Sobrescrever arquivo CSV de entrada
dotnet run -- --input data/outro-arquivo.csv

# Sobrescrever endpoint a ser usado
dotnet run -- --endpoint-name producao

# Sobrescrever múltiplas configurações
dotnet run -- \
  --config config.yaml \
  --input data/vendas.csv \
  --batch-lines 500 \
  --endpoint-name homologacao \
  --verbose

# Processar com logs detalhados
dotnet run -- --verbose
```

### Execução do executável compilado

```bash
./bin/Debug/net10.0/CsvToApi --help
./bin/Debug/net10.0/CsvToApi --config /caminho/para/config.yaml
./bin/Debug/net10.0/CsvToApi -i data/input.csv --endpoint-name producao -v
```

## Argumentos de Linha de Comando

Todos os argumentos são opcionais e sobrescrevem as configurações do arquivo YAML:

| Argumento | Forma Curta | Descrição | Exemplo |
|-----------|-------------|-----------|---------||
| `--config` | `-c` | Caminho do arquivo de configuração YAML | `--config config.yaml` |
| `--input` | `-i` | Caminho do arquivo CSV de entrada | `--input data/vendas.csv` |
| `--batch-lines` | `-b` | Número de linhas por lote | `--batch-lines 500` |
| `--start-line` | `-s` | Linha inicial para começar o processamento | `--start-line 100` |
| `--max-lines` | `-n` | Número máximo de linhas a processar | `--max-lines 1000` |
| `--log-dir` | `-l` | Diretório onde os logs serão salvos | `--log-dir logs` |
| `--delimiter` | `-d` | Delimitador do CSV | `--delimiter ";"` |
| `--execution-id` | `--exec-id` | UUID da execução para continuar checkpoint | `--exec-id abc-123...` |
| `--endpoint-name` | | Nome do endpoint configurado a ser usado | `--endpoint-name webhook1` |
| `--verbose` | `-v` | Exibir logs detalhados | `--verbose` |
| `--dry-run` | `--test` | Modo de teste: não faz requisições reais | `--dry-run` |

### Exemplos Práticos

**Processar arquivo diferente mantendo outras configurações:**

```bash
dotnet run -- -i data/clientes-2024.csv -v
```

**Usar endpoint específico:**

```bash
dotnet run -- --endpoint-name producao -v
```

**Teste rápido com lotes pequenos:**

```bash
dotnet run -- -b 10 -v
```

**Processar arquivo com delimitador ponto-e-vírgula:**

```bash
dotnet run -- -i data/export.csv -d ";" -v
```

**Continuar processamento a partir de uma linha específica:**

```bash
# Útil para retomar processamento após falha
dotnet run -- -i data/vendas.csv -s 1001 -v
```

**Processar apenas as primeiras N linhas (útil para testes):**

```bash
# Processar apenas as primeiras 100 linhas
dotnet run -- -i data/vendas.csv -n 100 -v

# Processar um intervalo específico (ex: linhas 101-200)
dotnet run -- -i data/vendas.csv -s 101 -n 100 -v
```

**Modo Dry-Run (teste sem requisições reais):**

```bash
# Validar configuração e dados sem fazer chamadas HTTP
dotnet run -- --dry-run -v
dotnet run -- --test -v
```

**Execution ID e Checkpoints:**

```bash
# Nova execução (gera UUID automaticamente)
dotnet run

# Continuar execução existente usando o UUID
dotnet run -- --execution-id 6869cdf3-5fb0-4178-966d-9a21015ffb4d -v

# Cada execução tem seus próprios arquivos:
# - logs/process_{uuid}.log
# - checkpoints/checkpoint_{uuid}.json
```

## Configuração (config.yaml)

```yaml
file:
    inputPath: "data/input.csv"           # Caminho do arquivo CSV
    batchLines: 100                       # Número de linhas por lote
    startLine: 1                          # Linha inicial (padrão: 1)
    maxLines: 1000                        # Número máximo de linhas a processar (opcional)
    logDirectory: "logs"                  # Diretório de logs
    csvDelimiter: ","                     # Delimitador do CSV
    checkpointDirectory: "checkpoints"    # Diretório de checkpoints
    mapping:                              # Validações de colunas
        - column: "Name"
          type: "string"
        - column: "Email"
          type: "string"
          regex: "^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\\.[a-zA-Z]{2,}$"
        - column: "Birthdate"
          type: "date"
          format: "YYYY-MM-DD"

# Nome da coluna CSV que contém o nome do endpoint (opcional)
endpointColumnName: "Endpoint"

# Endpoint padrão quando não especificado (opcional)
# Se não configurado e houver apenas 1 endpoint, ele será usado automaticamente
defaultEndpoint: "webhook1"

# Lista de endpoints (obrigatório - pelo menos um)
endpoints:
  - name: "webhook1"
    endpointUrl: "https://api.example.com/upload"
    headers:                              # Headers HTTP customizados (opcional)
      Authorization: "Bearer your_auth_token_here"
      X-Custom-Header: "valor-customizado"
      X-API-Key: "sua-chave-api"
    method: "POST"                        # POST ou PUT
    requestTimeout: 30                    # Timeout em segundos
    retryAttempts: 3
    retryDelaySeconds: 5
    maxRequestsPerSecond: 10
    mapping:                              # Mapeamento CSV -> API
      - attribute: "name"
        csvColumn: "Name"                 # Valor vem da coluna CSV
        transform: "uppercase"            # Opcional: transformação de dados
      - attribute: "email"
        csvColumn: "Email"
        transform: "lowercase"            # Converter para minúsculas
      - attribute: "address.street"       # Suporta atributos aninhados
        csvColumn: "Street"
        transform: "title-case"           # Primeira letra maiúscula
      - attribute: "birthdate"
        csvColumn: "Birthdate"
      - attribute: "cpf"
        csvColumn: "CPF"
        transform: "format-cpf"           # Formata como 000.000.000-00
      # Parâmetros com valores fixos (não vêm do CSV)
      - attribute: "source"
        fixedValue: "csv-import"          # Valor fixo para todos os registros
      - attribute: "version"
        fixedValue: "1.0"
```

### Headers HTTP Customizados

Você pode configurar headers HTTP customizados para cada endpoint. Isso permite:

- **Autenticação Bearer Token**: `Authorization: "Bearer seu-token"`
- **Autenticação API Key**: `X-API-Key: "sua-chave"`
- **Headers customizados**: Qualquer header HTTP válido
- **Content-Type**: Se não especificado, usa `application/json` por padrão

**Exemplo:**

```yaml
endpoints:
  - name: "producao"
    endpointUrl: "https://api.exemplo.com/v1/eventos"
    headers:
      Authorization: "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
      X-Tenant-ID: "empresa-123"
      X-API-Version: "2.0"
    method: "POST"
```

**Nota**: Headers de conteúdo como `Content-Type` são tratados automaticamente. O padrão é `application/json`.

## Múltiplos Endpoints

A aplicação trabalha com endpoints nomeados, permitindo rotear diferentes linhas do CSV para diferentes APIs.

### Configuração de Endpoints

```yaml
# Endpoint padrão quando não especificado (opcional)
defaultEndpoint: "webhook1"

# Nome da coluna CSV que define qual endpoint usar (opcional)
endpointColumnName: "Endpoint"

# Lista de endpoints (obrigatório - pelo menos um)
endpoints:
  - name: "webhook1"
    endpointUrl: "https://webhook.site/endpoint1"
    headers:
      Authorization: "Bearer token_endpoint1"
    method: "POST"
    requestTimeout: 30
    retryAttempts: 3
    retryDelaySeconds: 5
    maxRequestsPerSecond: 10
    mapping:
      - attribute: "name"
        csvColumn: "Name"
        transform: "uppercase"
      - attribute: "source"
        fixedValue: "endpoint1"
  
  - name: "webhook2"
    endpointUrl: "https://webhook.site/endpoint2"
    headers:
      Authorization: "Bearer token_endpoint2"
      X-API-Key: "chave-api-endpoint2"
    method: "POST"
    requestTimeout: 30
    mapping:
      - attribute: "fullName"
        csvColumn: "Name"
      - attribute: "source"
        fixedValue: "endpoint2"
```

### Formas de Selecionar o Endpoint

#### 1. Via Argumento de Linha de Comando (Prioridade 1)

Aplica o mesmo endpoint para todas as linhas:

```bash
dotnet run -- --endpoint-name webhook1
```

#### 2. Via Coluna CSV (Prioridade 2)

Configure `endpointColumnName` no YAML e adicione uma coluna no CSV:

**config.yaml:**

```yaml
endpointColumnName: "Endpoint"
```

**input.csv:**

```csv
Name,Email,Endpoint
John Doe,john@example.com,webhook1
Jane Smith,jane@example.com,webhook2
Bob Johnson,bob@example.com,webhook1
```

Cada linha será enviada para o endpoint especificado na coluna.

#### 3. Endpoint Padrão (Prioridade 3)

Configure `defaultEndpoint` no YAML:

**config.yaml:**

```yaml
defaultEndpoint: "webhook1"
```

#### 4. Endpoint Único Automático (Prioridade 4)

Se houver apenas um endpoint configurado e nenhum dos anteriores estiver definido, ele será usado automaticamente.

### Exemplos Práticos

**Processar todas as linhas usando webhook1:**

```bash
dotnet run -- --endpoint-name webhook1
```

**Processar com seleção dinâmica via CSV:**

```bash
dotnet run -- --config config.yaml
# Cada linha define seu endpoint na coluna "Endpoint"
```

**Combinar: usar endpoint via argumento sobrescreve CSV:**

```bash
dotnet run -- --endpoint-name webhook2
# Ignora a coluna "Endpoint" do CSV e usa webhook2 para tudo
```

**Usar endpoint padrão:**

```bash
dotnet run
# Usa o endpoint definido em 'defaultEndpoint'
```

## Formato do Arquivo de Log

Quando ocorrem erros, o arquivo de log contém:

- **LineNumber**: Número da linha no arquivo CSV original
- **Todas as colunas do CSV original**: Valores exatos da linha com erro
- **HttpCode**: Código HTTP do erro (400 para validação, 500 para exceções)
- **ErrorMessage**: Descrição do erro

Exemplo:

```csv
LineNumber,Name,Email,Street,Birthdate,HttpCode,ErrorMessage
5,John Doe,invalid-email,123 Main St,1990-05-15,400,"Valor 'invalid-email' inválido para coluna 'Email'"
8,Jane Smith,jane@example.com,456 Oak Ave,2025-13-45,400,"Data '2025-13-45' inválida para formato 'YYYY-MM-DD' na coluna 'Birthdate'"
```

## Estrutura do Projeto

```
CsvToApi/
├── Program.cs           # Código principal (top-level statements)
├── config.yaml          # Arquivo de configuração
├── CsvToApi.csproj      # Arquivo do projeto
├── data/
│   └── input.csv        # Arquivo CSV de entrada
└── logs/
    └── process.log      # Log de erros
```

## Performance

A aplicação foi otimizada para processar grandes volumes de dados:

1. **Processamento em lotes**: Evita carregar todo o arquivo na memória
2. **Paralelismo**: Múltiplas chamadas HTTP simultâneas
3. **Thread-safe**: Logging seguro com SemaphoreSlim
4. **Async/await**: Operações I/O não-bloqueantes

## Validações Suportadas

- **type: "string"**: Qualquer texto
- **type: "date"**: Valida formato de data
    - format: "YYYY-MM-DD", "DD/MM/YYYY", etc.
- **regex**: Validação com expressão regular customizada

## Exemplos de Payload da API

### Payload com dados do CSV e valores fixos

Com a configuração acima, cada linha do CSV gera um payload como:

```json
{
  "name": "John Doe",
  "email": "john.doe@example.com",
  "address": {
    "street": "123 Main St"
  },
  "birthdate": "1990-05-15",
  "source": "csv-import",
  "version": "1.0"
}
```

### Diferença entre csvColumn e fixedValue

No mapeamento da API, você pode usar:

- **csvColumn**: O valor vem da coluna correspondente no CSV (diferente para cada linha)
  ```yaml
  - attribute: "name"
    csvColumn: "Name"  # Valor varia por linha
  ```

- **fixedValue**: O valor é fixo para todos os registros (mesmo valor em todas as linhas)
  ```yaml
  - attribute: "source"
    fixedValue: "csv-import"  # Sempre "csv-import"
  ```

**Importante**: Cada mapping deve ter **OU** `csvColumn` **OU** `fixedValue`, mas não ambos.

## Filtros de Dados

O sistema permite filtrar as linhas do CSV antes do processamento, processando apenas registros que atendem a critérios
específicos. Os filtros são configurados **diretamente em cada coluna**.

### Exemplo de Configuração

```yaml
file:
    columns:
        - column: "campaign"
          type: "string"
          filter:
            operator: "Equals"
            value: "promo2024"
            caseInsensitive: true
        
        - column: "status"
          type: "string"
          filter:
            operator: "NotEquals"
            value: "cancelado"
            caseInsensitive: true
```

### Operadores Disponíveis

- **Equals**: Valor exatamente igual
- **NotEquals**: Valor diferente
- **Contains**: Valor contém o texto especificado
- **NotContains**: Valor não contém o texto especificado

**Documentação completa**: Veja [data/README-FILTROS.md](data/README-FILTROS.md) para exemplos detalhados e casos de
uso.

## Transformações de Dados

A aplicação oferece 20+ transformações que podem ser aplicadas aos dados antes do envio para a API.

### Transformações Disponíveis

**Texto:**

- `uppercase` - Converte para MAIÚSCULAS
- `lowercase` - Converte para minúsculas
- `capitalize` - Primeira letra maiúscula
- `title-case` - Primeira Letra De Cada Palavra
- `trim` - Remove espaços nas extremidades

**Limpeza:**

- `remove-spaces` - Remove todos os espaços
- `remove-accents` - Remove acentos
- `remove-non-numeric` - Mantém apenas números
- `remove-non-alphanumeric` - Remove caracteres especiais

**Formatações Brasileiras:**

- `format-cpf` - Formata como 000.000.000-00
- `format-cnpj` - Formata como 00.000.000/0000-00
- `format-phone-br` - Formata telefone brasileiro
- `format-cep` - Formata como 00000-000

**Outras:**

- `slugify` - Converte para URL-friendly
- `base64-encode` - Codifica em Base64
- `url-encode` - Codifica para URL
- `date-format:FORMATO` - Reformata datas

### Exemplo de Uso

```yaml
endpoints:
  - name: "api-usuarios"
    mapping:
      - attribute: "nome"
        csvColumn: "Nome"
        transform: "title-case"
      - attribute: "email"
        csvColumn: "Email"
        transform: "lowercase"
      - attribute: "cpf"
        csvColumn: "CPF"
        transform: "format-cpf"
```

**Documentação completa**: Veja [TRANSFORMACOES.md](TRANSFORMACOES.md) para todas as transformações e exemplos.

## Tratamento de Erros

A aplicação registra erros em três situações:

1. **Validação de dados**: Regex ou formato inválido (HTTP 400)
2. **Erro na API**: Response não-sucesso (HTTP code real da API)
3. **Exceções**: Timeout, conexão, etc. (HTTP 500)

## Dependências

- **YamlDotNet**: Leitura de arquivos YAML
- **CsvHelper**: Processamento eficiente de CSV
- **Spectre.Console**: Interface visual moderna e interativa
- **Spectre.Console.Cli**: Parsing robusto de argumentos CLI

## Licença

MIT

