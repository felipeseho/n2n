# 🚀 Quick Start - CsvToApi

## Em 5 Minutos

### 1. Verificar Pré-requisitos

```bash
dotnet --version  # Deve ser >= 10.0
```

### 2. Navegar para o Projeto

```bash
cd /Users/felipeseho/Development/felipeseho/csv-to-api/CsvToApi/CsvToApi
```

### 3. Restaurar Dependências

```bash
dotnet restore
```

### 4. Executar

```bash
dotnet run
```

## Testando com API Real

### Opção 1: Webhook.site (Recomendado para Testes)

1. Acesse: https://webhook.site
2. Copie sua URL única
3. Execute com argumento:
   ```bash
   dotnet run -- --endpoint "https://webhook.site/SUA-URL-AQUI" --verbose
   ```

   **OU** edite `config.yaml`:
   ```yaml
   api:
       endpointUrl: "https://webhook.site/SUA-URL-AQUI"
   ```
4. Execute:
   ```bash
   dotnet run
   ```
5. Veja as requisições em webhook.site

### Opção 2: Usar Argumentos de Linha de Comando

Sobrescreva configurações sem editar arquivos:

```bash
# Teste rápido
dotnet run -- --input data/input.csv --batch-lines 10 --verbose

# Modo dry-run (teste sem requisições reais)
dotnet run -- --dry-run --verbose

# Endpoint customizado
dotnet run -- --endpoint-name webhook1 --verbose

# Múltiplas configurações
dotnet run -- \
  --input data/vendas.csv \
  --endpoint-name webhook1 \
  --batch-lines 500 \
  --verbose

# Continuar de um checkpoint existente
dotnet run -- --execution-id abc-123-def-456 --verbose
```

Ver todas as opções:

```bash
dotnet run -- --help
```

### Opção 3: Seu Próprio Endpoint

Edite `config.yaml` com suas configurações:

```yaml
file:
    inputPath: "data/seu-arquivo.csv"
    batchLines: 100
    logDirectory: "logs"

endpoints:
  - name: "meu-endpoint"
    endpointUrl: "https://sua-api.com/endpoint"
    headers:
      Authorization: "Bearer seu-token-aqui"
      X-Custom-Header: "valor-customizado"
    method: "POST"
```

## Estrutura Mínima Necessária

```
CsvToApi/
├── Program.cs              # ✅ Código principal
├── CsvToApi.csproj         # ✅ Projeto
├── config.yaml             # ✅ Configuração
└── data/
    └── input.csv           # ✅ Seu arquivo CSV
```

## Exemplo de CSV

Crie `data/meu-arquivo.csv`:

```csv
Name,Email,Phone
John Doe,john@example.com,+1234567890
Jane Smith,jane@example.com,+0987654321
```

## Exemplo de Configuração Mínima

Crie `config.yaml`:

```yaml
file:
    inputPath: "data/meu-arquivo.csv"
    batchLines: 100
    logDirectory: "logs"
    csvDelimiter: ","
    checkpointDirectory: "checkpoints"
    mapping: []

endpoints:
  - name: "default"
    endpointUrl: "https://webhook.site/SUA-URL"
    headers:
      Authorization: "Bearer seu-token-aqui"
    method: "POST"
    requestTimeout: 30
    mapping:
      - attribute: "name"
        csvColumn: "Name"
        transform: "title-case"    # Opcional: transformar dados
      - attribute: "email"
        csvColumn: "Email"
        transform: "lowercase"     # Converter para minúsculas
      - attribute: "phone"
        csvColumn: "Phone"
```

## Executar

```bash
dotnet run
```

## Output Esperado

```
Processadas 2 linhas. Erros: 0

Total de linhas processadas: 2
Total de erros: 0
Processamento concluído!
```

## Ver Logs de Erro (se houver)

```bash
cat logs/errors.log
```

## Build para Produção

```bash
# macOS ARM64 (M1/M2/M3)
dotnet publish -c Release -r osx-arm64 --self-contained

# Executar
./bin/Release/net10.0/osx-arm64/publish/CsvToApi
```

## Comandos Úteis

```bash
# Ver progresso em tempo real
dotnet run -- --verbose

# Teste sem requisições reais (dry-run)
dotnet run -- --dry-run --verbose

# Executar com configuração específica
dotnet run -- --config minha-config.yaml

# Processar apenas primeiras 100 linhas
dotnet run -- --max-lines 100 --verbose

# Usar endpoint específico
dotnet run -- --endpoint-name producao --verbose

# Build release
dotnet build -c Release

# Limpar build
dotnet clean
```

## Troubleshooting Rápido

### ❌ "Arquivo CSV não encontrado"

```bash
# Verificar se o arquivo existe
ls -la data/input.csv

# Usar caminho absoluto no config.yaml
inputPath: "/caminho/completo/para/arquivo.csv"
```

### ❌ "URL do endpoint não configurada"

```bash
# Verificar config.yaml
cat config.yaml | grep endpointUrl
```

### ❌ Build fails

```bash
# Limpar e rebuildar
dotnet clean
dotnet restore
dotnet build
```

## Próximos Passos

1. ✅ **Teste básico funcionando** → Leia [README.md](README.md)
2. ✅ **Entender validações** → Leia [EXEMPLOS.md](EXEMPLOS.md)
3. ✅ **Usar transformações** → Leia [TRANSFORMACOES.md](TRANSFORMACOES.md)
4. ✅ **Configurar filtros** → Leia [data/README-FILTROS.md](data/README-FILTROS.md)
5. ✅ **Argumentos CLI** → Leia [ARGUMENTOS.md](ARGUMENTOS.md)
6. ✅ **Customizar** → Ajuste `config.yaml`

## Suporte

- **Documentação Completa**: [README.md](README.md)
- **Exemplos de Uso**: [EXEMPLOS.md](EXEMPLOS.md)
- **Transformações**: [TRANSFORMACOES.md](TRANSFORMACOES.md)
- **Argumentos CLI**: [ARGUMENTOS.md](ARGUMENTOS.md)
- **Changelog**: [CHANGELOG.md](CHANGELOG.md)

---

**Tempo estimado para primeiro teste**: 5 minutos ⏱️

