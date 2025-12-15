# Processamento de Múltiplos Arquivos

## Visão Geral

A partir desta versão, o n2n suporta o processamento de múltiplos arquivos CSV em uma única execução. Cada arquivo é processado sequencialmente na ordem configurada, com checkpoint e log individuais.

## Configuração

### Opção 1: Arquivo Único (Compatibilidade)

Para manter a compatibilidade com versões anteriores, você pode continuar usando `inputPath`:

```yaml
file:
  inputPath: "data/input.csv"
  batchLines: 100
  # ... demais configurações
```

### Opção 2: Múltiplos Arquivos (NOVO)

Para processar múltiplos arquivos, use `inputPaths` com uma lista de caminhos:

```yaml
file:
  inputPaths:
    - "data/file1.csv"
    - "data/file2.csv"
    - "data/file3.csv"
  batchLines: 100
  # ... demais configurações
```

## Comportamento

### Ordem de Processamento

Os arquivos são processados **sequencialmente** na ordem especificada no YAML. O processamento do próximo arquivo só inicia após o término do anterior.

### Checkpoint e Log Individuais

Cada arquivo gera seus próprios arquivos de checkpoint e log com nomenclatura única:

- **Checkpoint**: `checkpoint_{executionId}_{nomeDoArquivo}.json`
- **Log**: `process_{executionId}_{nomeDoArquivo}.log`

Exemplo:
```
checkpoints/
  checkpoint_abc123_file1.json
  checkpoint_abc123_file2.json
  checkpoint_abc123_file3.json

logs/
  process_abc123_file1.log
  process_abc123_file2.log
  process_abc123_file3.log
```

### Arquivos Não Encontrados

Se um arquivo não for encontrado:

1. ⚠️  Uma mensagem de erro é exibida no console
2. 📝 Um arquivo de log é criado registrando o erro
3. 💾 Um checkpoint é criado com `errorCount = 1` e mensagem descritiva
4. ➡️  O processamento **continua** para o próximo arquivo da lista

Isso permite que você configure uma lista de arquivos sem se preocupar se todos existem - arquivos faltantes são registrados mas não interrompem o processamento.

## Exemplo Completo

```yaml
file:
  inputPaths:
    - "/caminho/completo/dados_jan_2024.csv"
    - "/caminho/completo/dados_fev_2024.csv"
    - "/caminho/completo/dados_mar_2024.csv"
    - "/caminho/completo/dados_abr_2024.csv"  # Mesmo que não exista, será registrado
  
  batchLines: 100
  startLine: 1
  maxLines: 1000  # Limite por arquivo (opcional)
  logDirectory: "logs"
  csvDelimiter: ","
  checkpointDirectory: "checkpoints"
  
  columns:
    - column: "id"
      type: "string"
    - column: "nome"
      type: "string"

defaultEndpoint: "api_principal"

endpoints:
  - name: "api_principal"
    endpointUrl: "https://api.exemplo.com/v1/endpoint"
    method: "POST"
    mapping:
      - attribute: "id"
        csvColumn: "id"
      - attribute: "nome"
        csvColumn: "nome"
```

## Métricas e Dashboard

O dashboard mostra:
- Total de arquivos a processar
- Arquivo atual sendo processado (X/Y)
- Progresso individual de cada arquivo
- Métricas consolidadas ao final

## Casos de Uso

### 1. Processamento de Dados Mensais
```yaml
inputPaths:
  - "dados/jan-2024.csv"
  - "dados/fev-2024.csv"
  - "dados/mar-2024.csv"
```

### 2. Separação por Origem
```yaml
inputPaths:
  - "imports/sistema-a.csv"
  - "imports/sistema-b.csv"
  - "imports/sistema-c.csv"
```

### 3. Processamento Incremental
```yaml
inputPaths:
  - "batches/batch-001.csv"
  - "batches/batch-002.csv"
  - "batches/batch-003.csv"
  - "batches/batch-004.csv"
```

## Migração

Se você tem um arquivo de configuração existente com `inputPath`, **não precisa fazer nada** - ele continuará funcionando. Para migrar:

```yaml
# Antes
file:
  inputPath: "data/input.csv"

# Depois (se quiser usar múltiplos arquivos)
file:
  inputPaths:
    - "data/input.csv"
    - "data/input2.csv"
```

## Notas Importantes

1. ✅ **Retrocompatibilidade**: Configurações existentes com `inputPath` continuam funcionando
2. 📋 **Ordem**: Os arquivos são processados exatamente na ordem da lista
3. 💾 **Checkpoints**: Cada arquivo tem checkpoint independente, permitindo retomar processamento individual
4. 🔄 **Continuidade**: Arquivos não encontrados não interrompem o processamento
5. 📊 **Logs**: Logs separados facilitam auditoria e troubleshooting
