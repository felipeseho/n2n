# Guia Rápido de Argumentos de Linha de Comando

## Ver Todas as Opções

```bash
dotnet run -- --help
```

## Tabela de Referência Rápida

| Argumento         | Forma Curta | Descrição                    | Exemplo                    |
|-------------------|-------------|------------------------------|----------------------------|
| `--config`        | `-c`        | Arquivo de configuração YAML | `--config config.yaml`     |
| `--input`         | `-i`        | Arquivo CSV de entrada       | `--input data/vendas.csv`  |
| `--batch-lines`   | `-b`        | Linhas por lote              | `--batch-lines 500`        |
| `--start-line`    | `-s`        | Linha inicial                | `--start-line 100`         |
| `--max-lines`     | `-n`        | Máximo de linhas a processar | `--max-lines 1000`         |
| `--log-dir`       | `-l`        | Diretório de logs            | `--log-dir logs/prod`      |
| `--delimiter`     | `-d`        | Delimitador do CSV           | `--delimiter ";"`          |
| `--execution-id`  | `--exec-id` | UUID para continuar execução | `--exec-id abc-123...`     |
| `--endpoint-name` |             | Endpoint configurado a usar  | `--endpoint-name producao` |
| `--verbose`       | `-v`        | Logs detalhados              | `--verbose`                |
| `--dry-run`       | `--test`    | Teste sem requisições        | `--dry-run`                |

## Opções Principais

### Arquivo de Configuração

```bash
# Especificar arquivo de configuração
dotnet run -- --config config-prod.yaml
dotnet run -- -c config-test.yaml
```

### Sobrescrever Arquivo CSV

```bash
# Processar arquivo diferente
dotnet run -- --input data/vendas.csv
dotnet run -- -i data/clientes.csv
```

### Ajustar Processamento em Lote

```bash
# Processar 500 linhas por vez
dotnet run -- --batch-lines 500
dotnet run -- -b 1000
```

### Linha Inicial

```bash
# Começar processamento a partir da linha 100
dotnet run -- --start-line 100
dotnet run -- -s 500

# Útil para retomar processamento após falha
dotnet run -- -i data/vendas.csv -s 1001 -v
```

### Limitar Quantidade de Linhas

```bash
# Processar apenas as primeiras 1000 linhas
dotnet run -- --max-lines 1000
dotnet run -- -n 500

# Útil para testes ou processamento parcial
dotnet run -- -i data/vendas.csv -n 100 -v

# Combinar com linha inicial para processar um intervalo específico
# Exemplo: processar linhas 101 a 200
dotnet run -- -s 101 -n 100 -v
```

### Execution ID (Controle de Checkpoint)

```bash
# Nova execução (gera UUID automaticamente)
dotnet run

# Continuar execução existente usando o UUID
dotnet run -- --execution-id 6869cdf3-5fb0-4178-966d-9a21015ffb4d
dotnet run -- --exec-id 6869cdf3-5fb0-4178-966d-9a21015ffb4d

# Processar mais linhas em uma execução existente
dotnet run -- --execution-id 6869cdf3-5fb0-4178-966d-9a21015ffb4d --max-lines 1000

# Cada execução tem seus próprios arquivos de log e checkpoint:
# - logs/process_{uuid}.log
# - checkpoints/checkpoint_{uuid}.json
```

### Selecionar Endpoint Específico

```bash
# Usar endpoint nomeado configurado no YAML
dotnet run -- --endpoint-name webhook1
dotnet run -- --endpoint-name producao

# Sobrescreve coluna CSV e defaultEndpoint
dotnet run -- --endpoint-name teste --verbose
```

### Modo Dry-Run (Teste sem Requisições)

```bash
# Validar configuração e dados sem fazer chamadas HTTP
dotnet run -- --dry-run
dotnet run -- --test

# Combinar com verbose para ver detalhes
dotnet run -- --dry-run --verbose

# Testar com subset de dados
dotnet run -- --dry-run --max-lines 100 -v
```

### Delimitador CSV

```bash
# Usar ponto-e-vírgula como delimitador
dotnet run -- --delimiter ";"
dotnet run -- -d "|"
```

### Diretório de Logs

```bash
# Especificar diretório de logs diferente
dotnet run -- --log-dir logs/producao
dotnet run -- -l logs/teste
```

## Exemplos Combinados

### Teste em Desenvolvimento com Dry-Run

```bash
dotnet run -- \
  -i data/test.csv \
  --endpoint-name desenvolvimento \
  -b 10 \
  -n 50 \
  --dry-run \
  -v
```

### Produção com Todas as Configurações

```bash
dotnet run -- \
  --config config-prod.yaml \
  --input data/vendas-diarias.csv \
  --endpoint-name producao \
  --batch-lines 1000 \
  --verbose
```

### Teste Rápido com Webhook

```bash
dotnet run -- \
  -i data/sample.csv \
  --endpoint-name webhook1 \
  -b 5 \
  --dry-run \
  -v
```

### Processar Arquivo com Configuração Específica

```bash
dotnet run -- \
  --config config.yaml \
  --input data/novos-usuarios.csv \
  --batch-lines 100 \
  --log-dir logs/usuarios \
  --verbose
```

### Retomar Processamento Após Falha

```bash
# Se o processamento falhou, use o mesmo execution-id
dotnet run -- \
  --execution-id abc-123-def-456 \
  --verbose

# Ou comece de uma linha específica
dotnet run -- \
  --input data/vendas-grandes.csv \
  --start-line 501 \
  --batch-lines 100 \
  --verbose
```

### Processar Intervalo Específico de Linhas

```bash
# Processar linhas 1001 a 2000
dotnet run -- \
  -i data/grande-arquivo.csv \
  -s 1001 \
  -n 1000 \
  --endpoint-name producao \
  -v
```

## Prioridade das Configurações

1. **Argumentos de linha de comando** (maior prioridade)
2. Arquivo YAML especificado em `--config`
3. `config.yaml` padrão (se nenhum argumento for fornecido)

## Dicas

- Use `-v` ou `--verbose` para debug e acompanhamento do processo
- Combine argumentos para testes rápidos sem modificar arquivos YAML
- Argumentos são especialmente úteis em scripts e CI/CD
- Sempre teste com `--batch-lines` pequeno primeiro (ex: 10) antes de processar arquivos grandes
