# Exemplo de Uso com API Real

## Exemplo 1: Webhook.site (para testes)

Para testar a aplicação com uma API real sem precisar criar um servidor, você pode usar o [webhook.site](https://webhook.site):

1. Acesse https://webhook.site
2. Copie a URL única gerada (ex: `https://webhook.site/12345678-abcd-...`)
3. Atualize o `config.yaml`:

```yaml
endpoints:
  - name: "teste"
    endpointUrl: "https://webhook.site/sua-url-unica-aqui"
    headers:
      Authorization: "Bearer seu-token-se-necessario"
    method: "POST"
    requestTimeout: 30
    mapping:
      - attribute: "name"
        csvColumn: "Name"
      - attribute: "email"
        csvColumn: "Email"
```

4. Execute o programa:
```bash
dotnet run
```

5. Verifique as requisições recebidas no webhook.site

## Exemplo 2: API REST Real

### Configuração para API de Cadastro de Usuários

```yaml
file:
    inputPath: "data/usuarios.csv"
    batchLines: 50
    logDirectory: "logs"
    csvDelimiter: ","
    checkpointDirectory: "checkpoints"
    mapping:
        - column: "Nome Completo"
          type: "string"
        - column: "E-mail"
          type: "string"
          regex: "^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\\.[a-zA-Z]{2,}$"
        - column: "CPF"
          type: "string"
          regex: "^\\d{3}\\.\\d{3}\\.\\d{3}-\\d{2}$"
        - column: "Data Nascimento"
          type: "date"
          format: "DD/MM/YYYY"
        - column: "CEP"
          type: "string"
          regex: "^\\d{5}-\\d{3}$"

endpoints:
  - name: "usuarios"
    endpointUrl: "https://api.exemplo.com.br/api/v1/usuarios"
    headers:
      Authorization: "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
      X-API-Version: "1.0"
    method: "POST"
    requestTimeout: 45
    mapping:
      - attribute: "nome"
        csvColumn: "Nome Completo"
      - attribute: "email"
        csvColumn: "E-mail"
      - attribute: "cpf"
        csvColumn: "CPF"
      - attribute: "dataNascimento"
        csvColumn: "Data Nascimento"
      - attribute: "endereco.cep"
        csvColumn: "CEP"
      - attribute: "endereco.rua"
        csvColumn: "Rua"
      - attribute: "endereco.numero"
        csvColumn: "Numero"
      - attribute: "endereco.cidade"
        csvColumn: "Cidade"
      - attribute: "endereco.estado"
        csvColumn: "Estado"
```

### Arquivo CSV de Exemplo (usuarios.csv)

```csv
Nome Completo,E-mail,CPF,Data Nascimento,CEP,Rua,Numero,Cidade,Estado
João da Silva,joao.silva@email.com,123.456.789-00,15/05/1990,12345-678,Rua das Flores,100,São Paulo,SP
Maria Santos,maria.santos@email.com,987.654.321-00,22/08/1985,98765-432,Av. Principal,250,Rio de Janeiro,RJ
```

### Payload Gerado

```json
{
  "nome": "João da Silva",
  "email": "joao.silva@email.com",
  "cpf": "123.456.789-00",
  "dataNascimento": "15/05/1990",
  "endereco": {
    "cep": "12345-678",
    "rua": "Rua das Flores",
    "numero": "100",
    "cidade": "São Paulo",
    "estado": "SP"
  }
}
```

## Exemplo 3: Atualização em Massa (PUT)

```yaml
endpoints:
  - name: "atualizacao"
    endpointUrl: "https://api.exemplo.com/usuarios/{id}/atualizar"
    headers:
      Authorization: "Bearer seu-token-aqui"
    method: "PUT"
    requestTimeout: 30
    mapping:
      - attribute: "id"
        csvColumn: "ID"
      - attribute: "status"
        csvColumn: "Status"
      - attribute: "ultimaAtualizacao"
        csvColumn: "Data Atualizacao"
```

## Dicas de Performance

### Para arquivos grandes (1M+ linhas)

```yaml
file:
    batchLines: 500        # Lotes maiores
```

### Para APIs lentas

```yaml
file:
    batchLines: 10         # Lotes menores para evitar timeout
```

### Para máxima velocidade

```yaml
file:
    batchLines: 1000       # Lotes grandes
```

## Monitoramento de Progresso

A aplicação mostra o progresso em tempo real:

```
Processadas 100 linhas. Erros: 5
Processadas 200 linhas. Erros: 12
Processadas 300 linhas. Erros: 15
...
Total de linhas processadas: 10000
Total de erros: 234
Processamento concluído!
```

## Análise de Logs

### Verificar total de erros
```bash
wc -l logs/process.log
```

### Ver apenas erros de validação (HTTP 400)
```bash
grep ",400," logs/process.log
```

### Ver apenas erros de API (HTTP 500+)
```bash
grep -E ",(500|502|503|504)," logs/process.log
```

### Extrair emails com erro
```bash
awk -F',' '{print $3}' logs/process.log | tail -n +2
```

## Integração com Scripts

### Processar múltiplos arquivos

```bash
#!/bin/bash
for config in configs/*.yaml; do
    echo "Processando: $config"
    dotnet run -- "$config"
done
```

### Agendar com cron

```bash
# Executar todos os dias às 2h da manhã
0 2 * * * cd /path/to/CsvToApi && dotnet run -- config-diario.yaml
```

## Troubleshooting

### Erro: "Arquivo CSV não encontrado"
- Verifique o caminho em `file.inputPath`
- Use caminhos relativos ou absolutos

### Erro: "Connection timeout"
- Aumente o `requestTimeout` do endpoint no config.yaml
- Reduza `file.batchLines`
- Verifique conectividade com a API

### Erro: "401 Unauthorized"
- Verifique o header `Authorization` na configuração do endpoint
- Certifique-se que o token não expirou

### Muitos erros de validação
- Revise as expressões regex em `file.mapping`
- Verifique o formato dos dados no CSV
- Ajuste o formato de data se necessário

## Exemplos com Argumentos de Linha de Comando

### Processar arquivo diferente sem alterar config.yaml
```bash
dotnet run -- --input data/vendas-janeiro.csv
```

### Usar endpoint de produção temporariamente
```bash
dotnet run -- --endpoint-name producao --verbose
```

### Processar com lotes maiores
```bash
dotnet run -- --batch-lines 1000 --verbose
```

### Processar arquivo com delimitador ponto-e-vírgula
```bash
dotnet run -- --input data/export.csv --delimiter ";" --verbose
```

### Retomar processamento após falha
```bash
# Se o processamento falhou, use o mesmo execution-id
dotnet run -- \
  --execution-id abc-123-def-456 \
  --verbose
```

### Processar apenas um subconjunto de linhas para teste
```bash
# Processar apenas as primeiras 100 linhas
dotnet run -- \
  --input data/clientes.csv \
  --max-lines 100 \
  --endpoint-name teste \
  --verbose
```

## Exemplo 4: Usando Valores Fixos

### Cenário: Importação de dados com metadados

Quando você precisa enviar dados do CSV junto com informações fixas (como origem da importação, versão da API, tenant ID, etc.):

```yaml
endpoints:
  - name: "clientes"
    endpointUrl: "https://api.exemplo.com/v1/clientes"
    headers:
      Authorization: "Bearer xyz123..."
      X-Tenant-ID: "empresa-123"
    method: "POST"
    requestTimeout: 30
    mapping:
      # Dados dinâmicos do CSV
      - attribute: "nome"
        csvColumn: "Nome"
      - attribute: "email"
        csvColumn: "Email"
      - attribute: "telefone"
        csvColumn: "Telefone"
      
      # Valores fixos (metadados)
      - attribute: "origem"
        fixedValue: "importacao-csv"
      - attribute: "versaoApi"
        fixedValue: "v1"
      - attribute: "ambiente"
        fixedValue: "producao"
```

### CSV de Entrada
```csv
Nome,Email,Telefone
João Silva,joao@email.com,11999998888
Maria Santos,maria@email.com,11988887777
```

### Payload Gerado
Cada linha do CSV gera um payload com dados dinâmicos + valores fixos:

```json
{
  "nome": "João Silva",
  "email": "joao@email.com",
  "telefone": "11999998888",
  "origem": "importacao-csv",
  "versaoApi": "v1",
  "tenantId": "empresa-123",
  "ambiente": "producao"
}
```

### Casos de Uso para Valores Fixos

1. **Identificação da origem dos dados**
   ```yaml
   - attribute: "source"
     fixedValue: "csv-batch-import"
   ```

2. **Tenant ID em sistemas multi-tenant**
   ```yaml
   - attribute: "tenantId"
     fixedValue: "cliente-xyz"
   ```

3. **Versão da API ou formato**
   ```yaml
   - attribute: "apiVersion"
     fixedValue: "2.0"
   ```

4. **Status padrão para novos registros**
   ```yaml
   - attribute: "status"
     fixedValue: "pending"
   ```

5. **Metadados de importação**
   ```yaml
   - attribute: "metadata.importedAt"
     fixedValue: "2024-01-15"
   - attribute: "metadata.importedBy"
     fixedValue: "sistema-batch"
   ```

## Exemplo 4: Múltiplos Endpoints

### Caso de Uso: Rotear dados para diferentes sistemas baseado em tipo de cliente

Este exemplo mostra como enviar dados de diferentes clientes para endpoints específicos.

### Arquivo CSV (clientes.csv)
```csv
Nome,Email,TipoCliente,Telefone
João Silva,joao@empresa.com,premium,11999999999
Maria Santos,maria@email.com,basic,11888888888
Pedro Costa,pedro@premium.com,premium,11777777777
Ana Oliveira,ana@email.com,basic,11666666666
```

### Configuração com Múltiplos Endpoints

```yaml
file:
    inputPath: "data/clientes.csv"
    batchLines: 50
    logDirectory: "logs"
    csvDelimiter: ","
    checkpointDirectory: "checkpoints"
    mapping:
        - column: "Nome"
          type: "string"
        - column: "Email"
          type: "string"
          regex: "^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\\.[a-zA-Z]{2,}$"
        - column: "TipoCliente"
          type: "string"
        - column: "Telefone"
          type: "string"

# Nome da coluna que define qual endpoint usar
endpointColumnName: "TipoCliente"

# Configuração padrão (caso TipoCliente não seja reconhecido)
defaultEndpoint: "standard"

# Endpoints específicos por tipo de cliente
endpoints:
  - name: "standard"
    endpointUrl: "https://api.sistema.com/clientes/default"
    headers:
      Authorization: "Bearer token_default"
    method: "POST"
    requestTimeout: 30
    mapping:
      - attribute: "nome"
        csvColumn: "Nome"
      - attribute: "email"
        csvColumn: "Email"
      - attribute: "categoria"
        fixedValue: "standard"

  - name: "premium"
    endpointUrl: "https://api.premium.com/clientes"
    headers:
      Authorization: "Bearer token_premium_abc123"
      X-Client-Tier: "premium"
    method: "POST"
    requestTimeout: 45
    retryAttempts: 5
    retryDelaySeconds: 10
    maxRequestsPerSecond: 20
    mapping:
      - attribute: "nomeCompleto"
        csvColumn: "Nome"
        transform: "title-case"
      - attribute: "emailContato"
        csvColumn: "Email"
        transform: "lowercase"
      - attribute: "telefone"
        csvColumn: "Telefone"
      - attribute: "categoria"
        fixedValue: "premium"
      - attribute: "prioridade"
        fixedValue: "alta"
      - attribute: "sla"
        fixedValue: "24h"
  
  - name: "basic"
    endpointUrl: "https://api.basico.com/usuarios"
    headers:
      Authorization: "Bearer token_basic_xyz789"
    method: "POST"
    requestTimeout: 30
    retryAttempts: 3
    retryDelaySeconds: 5
    maxRequestsPerSecond: 10
    mapping:
      - attribute: "nome"
        csvColumn: "Nome"
      - attribute: "email"
        csvColumn: "Email"
      - attribute: "categoria"
        fixedValue: "basic"
      - attribute: "prioridade"
        fixedValue: "normal"
```

### Payloads Gerados

**Cliente Premium (João Silva):**
```json
{
  "nomeCompleto": "João Silva",
  "emailContato": "joao@empresa.com",
  "telefone": "11999999999",
  "categoria": "premium",
  "prioridade": "alta",
  "sla": "24h"
}
```
*Enviado para: https://api.premium.com/clientes*

**Cliente Basic (Maria Santos):**
```json
{
  "nome": "Maria Santos",
  "email": "maria@email.com",
  "categoria": "basic",
  "prioridade": "normal"
}
```
*Enviado para: https://api.basico.com/usuarios*

### Execução

**Processar usando a coluna TipoCliente do CSV:**
```bash
dotnet run -- --config config.yaml --verbose
```

**Forçar todos para endpoint premium (ignora coluna CSV):**
```bash
dotnet run -- --config config.yaml --endpoint-name premium
```

**Forçar todos para endpoint basic:**
```bash
dotnet run -- --endpoint-name basic
```

### Vantagens dessa Abordagem

1. **Flexibilidade**: Cada tipo de cliente pode ter endpoint, autenticação e mapeamento próprios
2. **Performance**: Diferentes limites de rate limiting por endpoint
3. **SLA**: Diferentes configurações de timeout e retry por prioridade
4. **Estrutura de dados**: Payloads customizados para cada sistema
5. **Fallback**: Configuração padrão para casos não mapeados

## Exemplo 5: Usando Filtros de Dados

### Cenário 1: Processar apenas uma campanha específica

Imagine que você tem um CSV com dados de múltiplas campanhas, mas quer processar apenas os registros de uma campanha específica:

```yaml
file:
    inputPath: "data/campanhas.csv"
    batchLines: 100
    mapping:
        - column: "nome"
          type: "string"
        
        - column: "email"
          type: "string"
        
        # Filtro: processar apenas registros da campanha "black_friday_2024"
        - column: "campanha"
          type: "string"
          filter:
            operator: "Equals"
            value: "black_friday_2024"
            caseInsensitive: true
        
        - column: "status"
          type: "string"

endpoints:
  - name: "campanha_api"
    endpointUrl: "https://api.exemplo.com/campanhas"
    method: "POST"
    mapping:
      - attribute: "nome"
        csvColumn: "nome"
      - attribute: "email"
        csvColumn: "email"
```

**Resultado**: Apenas linhas onde `campanha = "black_friday_2024"` serão enviadas para a API.

### Cenário 2: Excluir registros de teste

```yaml
file:
    mapping:
        - column: "email"
          type: "string"
          # Filtro: excluir emails de teste
          filter:
            operator: "NotContains"
            value: "test"
            caseInsensitive: true
        
        - column: "status"
          type: "string"
          # Filtro: excluir status cancelado
          filter:
            operator: "NotEquals"
            value: "cancelado"
            caseInsensitive: true
```

**Resultado**: Ignora linhas com emails contendo "test" ou status cancelado.

### Cenário 3: Processar apenas clientes de uma região

```yaml
file:
    inputPath: "data/clientes.csv"
    mapping:
        - column: "nome"
          type: "string"
        
        # Filtro: processar apenas clientes de SP
        - column: "estado"
          type: "string"
          filter:
            operator: "Contains"
            value: "SP"  # São Paulo
            caseInsensitive: true
        
        # Filtro: processar apenas plano premium
        - column: "plano"
          type: "string"
          filter:
            operator: "Equals"
            value: "premium"
            caseInsensitive: true
```

**Resultado**: Apenas clientes de SP com plano premium serão processados.

### Cenário 4: Filtrar múltiplos valores (OR simulado)

Para processar registros que tenham um entre vários valores (operação OR), você precisa executar o programa múltiplas vezes ou usar configurações separadas:

#### Opção 1: Execuções separadas

```bash
# Processar campanha A
dotnet run -- --config config-campanha-a.yaml

# Processar campanha B
dotnet run -- --config config-campanha-b.yaml
```

#### Opção 2: Usar "Contains" para múltiplos valores

Se os valores fazem parte de um padrão:

```yaml
file:
    mapping:
        # Processa campanhas que contenham "promo" (ex: promo2024, promo_natal, etc)
        - column: "campanha"
          type: "string"
          filter:
            operator: "Contains"
            value: "promo"
            caseInsensitive: true
```

### Cenário 5: Validar campos obrigatórios

```yaml
file:
    mapping:
        # Processar apenas linhas com email preenchido
        - column: "email"
          type: "string"
          filter:
            operator: "NotEquals"
            value: ""
        
        # Processar apenas linhas com telefone preenchido
        - column: "telefone"
          type: "string"
          filter:
            operator: "NotEquals"
            value: ""
```

**Resultado**: Apenas linhas com email E telefone preenchidos serão processadas.

### Dicas de Uso com Filtros

1. **Teste primeiro**: Use `maxLines: 10` para testar os filtros com poucas linhas
   ```yaml
   file:
       maxLines: 10  # Processar apenas 10 linhas
   ```

2. **Monitore as estatísticas**: O sistema mostra quantas linhas foram filtradas
   ```
   🔍 Filtros ativos (2):
     - Coluna 'campanha' igual a 'promo2024' (ignorar maiúsculas/minúsculas)
     - Coluna 'status' diferente de 'cancelado' (ignorar maiúsculas/minúsculas)
   
   🔍 Total de linhas filtradas: 1523
   ```

3. **Combine com validações**: Filtros são aplicados antes das validações, economizando processamento

4. **Use dry-run**: Teste sem enviar para a API
   ```bash
   dotnet run -- --dry-run
   ```

Veja a documentação completa em [data/README-FILTROS.md](data/README-FILTROS.md).

## Exemplo 6: Usando Transformações de Dados

### Cenário: Normalização de dados antes do envio

Muitas vezes os dados do CSV precisam ser transformados antes de serem enviados para a API. A aplicação oferece 20+ transformações prontas.

### Arquivo CSV (clientes.csv)
```csv
Nome,Email,CPF,Telefone,CEP
JOÃO SILVA,JOAO.SILVA@EMAIL.COM,12345678900,(11) 99999-9999,12345678
maria santos,Maria@Email.COM,98765432100,11-88888-8888,98765-432
Pedro Costa,pedro@EXEMPLO.com,11122233344,1177777777,01234567
```

### Configuração com Transformações

```yaml
endpoints:
  - name: "clientes_api"
    endpointUrl: "https://api.exemplo.com/clientes"
    headers:
      Authorization: "Bearer token123"
    method: "POST"
    mapping:
      # Transformar nome para Title Case (Primeira Letra Maiúscula)
      - attribute: "nome"
        csvColumn: "Nome"
        transform: "title-case"
      
      # Transformar email para minúsculas
      - attribute: "email"
        csvColumn: "Email"
        transform: "lowercase"
      
      # Formatar CPF (adiciona pontos e traço)
      - attribute: "cpf"
        csvColumn: "CPF"
        transform: "format-cpf"
      
      # Remover caracteres não-numéricos do telefone
      - attribute: "telefone"
        csvColumn: "Telefone"
        transform: "remove-non-numeric"
      
      # Formatar CEP
      - attribute: "cep"
        csvColumn: "CEP"
        transform: "format-cep"
```

### Payloads Gerados

**Linha 1 (JOÃO SILVA):**
```json
{
  "nome": "João Silva",           // title-case
  "email": "joao.silva@email.com", // lowercase
  "cpf": "123.456.789-00",         // format-cpf
  "telefone": "11999999999",       // remove-non-numeric
  "cep": "12345-678"               // format-cep
}
```

**Linha 2 (maria santos):**
```json
{
  "nome": "Maria Santos",          // title-case
  "email": "maria@email.com",      // lowercase
  "cpf": "987.654.321-00",         // format-cpf
  "telefone": "11888888888",       // remove-non-numeric
  "cep": "98765-432"               // format-cep
}
```

### Mais Exemplos de Transformações

#### Limpeza de Dados
```yaml
mapping:
  # Remover espaços extras
  - attribute: "codigo"
    csvColumn: "Codigo"
    transform: "trim"
  
  # Remover acentos
  - attribute: "slug"
    csvColumn: "Nome"
    transform: "slugify"
  
  # MAIÚSCULAS
  - attribute: "siglaEstado"
    csvColumn: "Estado"
    transform: "uppercase"
```

#### Formatações Brasileiras
```yaml
mapping:
  # CPF: 000.000.000-00
  - attribute: "cpf"
    csvColumn: "CPF"
    transform: "format-cpf"
  
  # CNPJ: 00.000.000/0000-00
  - attribute: "cnpj"
    csvColumn: "CNPJ"
    transform: "format-cnpj"
  
  # Telefone: (00) 00000-0000
  - attribute: "telefone"
    csvColumn: "Telefone"
    transform: "format-phone-br"
  
  # CEP: 00000-000
  - attribute: "cep"
    csvColumn: "CEP"
    transform: "format-cep"
```

#### Transformações Especiais
```yaml
mapping:
  # Codificar em Base64
  - attribute: "documentoBase64"
    csvColumn: "Documento"
    transform: "base64-encode"
  
  # URL encode
  - attribute: "parametro"
    csvColumn: "Parametro"
    transform: "url-encode"
  
  # Reverter string
  - attribute: "reverso"
    csvColumn: "Texto"
    transform: "reverse"
  
  # Formatar data
  - attribute: "dataNascimento"
    csvColumn: "DataNasc"
    transform: "date-format:DD/MM/YYYY"
```

### Transformações Disponíveis

**Texto:**
- `uppercase` - MAIÚSCULAS
- `lowercase` - minúsculas
- `capitalize` - Primeira letra maiúscula
- `title-case` - Primeira Letra De Cada Palavra
- `trim` - Remove espaços nas extremidades

**Limpeza:**
- `remove-spaces` - Remove todos os espaços
- `remove-all-spaces` - Remove todos os espaços em branco
- `remove-accents` - Remove acentos
- `remove-non-numeric` - Mantém apenas números
- `remove-non-alphanumeric` - Remove caracteres especiais

**Formatações Brasileiras:**
- `format-cpf` - 000.000.000-00
- `format-cnpj` - 00.000.000/0000-00
- `format-phone-br` - (00) 00000-0000
- `format-cep` - 00000-000

**Outras:**
- `slugify` - texto-url-friendly
- `base64-encode` - Codifica em Base64
- `url-encode` - Codifica para URL
- `reverse` - Inverte a string
- `date-format:FORMATO` - Reformata datas

Veja a documentação completa em [TRANSFORMACOES.md](TRANSFORMACOES.md).

## Exemplo 7: Dry-Run e Execution ID

### Dry-Run: Teste sem Requisições Reais

Use o modo dry-run para validar configurações e dados sem fazer chamadas HTTP reais:

```bash
# Validar configuração completa
dotnet run -- --dry-run --verbose

# Testar com subset de dados
dotnet run -- --dry-run --max-lines 100 -v

# Testar endpoint específico
dotnet run -- --dry-run --endpoint-name producao -v
```

**Vantagens:**
- Valida arquivo CSV e configuração YAML
- Testa transformações e filtros
- Não consome créditos da API
- Identifica problemas antes do processamento real

### Execution ID: Controle de Checkpoints

Cada execução tem um UUID único que identifica seus logs e checkpoints:

```bash
# Nova execução (gera UUID automaticamente)
dotnet run

# Exemplo de saída:
# 🆔 Execution ID: 6869cdf3-5fb0-4178-966d-9a21015ffb4d
# 📁 Log: logs/process_6869cdf3-5fb0-4178-966d-9a21015ffb4d.log
# 💾 Checkpoint: checkpoints/checkpoint_6869cdf3-5fb0-4178-966d-9a21015ffb4d.json
```

#### Continuar Execução Existente

Se o processamento foi interrompido, continue de onde parou:

```bash
# Usar o mesmo execution-id
dotnet run -- --execution-id 6869cdf3-5fb0-4178-966d-9a21015ffb4d -v
```

#### Processar Mais Linhas na Mesma Execução

```bash
# Processar mais 1000 linhas na execução existente
dotnet run -- \
  --execution-id 6869cdf3-5fb0-4178-966d-9a21015ffb4d \
  --max-lines 1000 \
  -v
```

#### Estrutura de Arquivos por Execução

```
logs/
  ├── process_6869cdf3-5fb0-4178-966d-9a21015ffb4d.log
  ├── process_abc-123-def-456.log
  └── process_xyz-789-ghi-012.log

checkpoints/
  ├── checkpoint_6869cdf3-5fb0-4178-966d-9a21015ffb4d.json
  ├── checkpoint_abc-123-def-456.json
  └── checkpoint_xyz-789-ghi-012.json
```

**Vantagens:**
- Rastreabilidade completa de cada processamento
- Múltiplas execuções do mesmo arquivo sem conflito
- Fácil retomada após falhas
- Histórico organizado

