<div align="center">
  <h1>🔍 Filtros de Dados</h1>
  <p><strong>Processe apenas as linhas que realmente importam</strong></p>
</div>

---

## 📋 Visão Geral

Os **filtros de dados** permitem processar apenas linhas do CSV que atendem a critérios específicos. Isso é especialmente útil para:

- ✅ **Processar apenas registros ativos**
- ✅ **Filtrar por campanha ou categoria**
- ✅ **Excluir registros cancelados ou inativos**
- ✅ **Selecionar dados de um período específico**
- ✅ **Reduzir custos** processando menos dados

### ✨ Múltiplos Filtros na Mesma Coluna

Você pode aplicar **múltiplos filtros na mesma coluna**, permitindo lógicas mais complexas como:

- Excluir vários valores diferentes (ex: não processar "cancelado" NEM "inativo")
- Combinar condições positivas e negativas
- Filtros mais refinados sem precisar de múltiplas colunas

**Formato:**

```yaml
# Um único filtro
- column: "Status"
  type: "string"
  filters:
    - operator: "Equals"
      value: "ativo"

# Múltiplos filtros na mesma coluna
- column: "Status"
  type: "string"
  filters:  # ← Use sempre "filters" (plural)
    - operator: "NotEquals"
      value: "cancelado"
    - operator: "NotEquals"
      value: "inativo"
    - operator: "NotEquals"
      value: "suspenso"
```

---

## 🎯 Como Funciona

Os filtros são configurados **diretamente em cada coluna** no arquivo de configuração. Uma linha só será processada se **passar em TODOS os filtros** configurados (operação AND).

```yaml
file:
  mapping:
    - column: "Status"
      type: "string"
      filters:                   # ← Filtros configurados
        - operator: "Equals"
          value: "ativo"
          caseInsensitive: true
```

---

## 🔧 Operadores Disponíveis

### `Equals` - Valor igual

Processa apenas linhas onde o valor é **exatamente igual** ao especificado.

```yaml
filters:
  - operator: "Equals"
    value: "ativo"
    caseInsensitive: true    # Opcional: ignora maiúsculas/minúsculas
```

**Exemplos:**
- ✅ `"ativo"` == `"ativo"` → **Processa**
- ✅ `"ATIVO"` == `"ativo"` (com `caseInsensitive: true`) → **Processa**
- ❌ `"inativo"` == `"ativo"` → **Ignora**
- ❌ `"ativo "` == `"ativo"` → **Ignora** (espaço extra)

---

### `NotEquals` - Valor diferente

Processa apenas linhas onde o valor é **diferente** do especificado.

```yaml
filters:
  - operator: "NotEquals"
    value: "cancelado"
    caseInsensitive: true
```

**Exemplos:**
- ✅ `"ativo"` != `"cancelado"` → **Processa**
- ✅ `"pendente"` != `"cancelado"` → **Processa**
- ❌ `"cancelado"` != `"cancelado"` → **Ignora**
- ❌ `"CANCELADO"` != `"cancelado"` (com `caseInsensitive: true`) → **Ignora**

---

### `Contains` - Contém o texto

Processa apenas linhas onde o valor **contém** o texto especificado.

```yaml
filters:
  - operator: "Contains"
    value: "promo"
    caseInsensitive: true
```

**Exemplos:**
- ✅ `"promo2024"` contém `"promo"` → **Processa**
- ✅ `"super-promo-verao"` contém `"promo"` → **Processa**
- ✅ `"PROMOCAO"` contém `"promo"` (com `caseInsensitive: true`) → **Processa**
- ❌ `"desconto"` contém `"promo"` → **Ignora**

---

### `NotContains` - Não contém o texto

Processa apenas linhas onde o valor **não contém** o texto especificado.

```yaml
filters:
  - operator: "NotContains"
    value: "teste"
    caseInsensitive: true
```

**Exemplos:**
- ✅ `"producao"` não contém `"teste"` → **Processa**
- ✅ `"cliente-real"` não contém `"teste"` → **Processa**
- ❌ `"ambiente-teste"` não contém `"teste"` → **Ignora**
- ❌ `"TESTE-123"` não contém `"teste"` (com `caseInsensitive: true`) → **Ignora**

---

## 💡 Exemplos Práticos

### Exemplo 1: Filtro Simples - Apenas Ativos

**Objetivo:** Processar apenas registros com status "ativo".

```yaml
file:
  inputPath: "data/usuarios.csv"
  mapping:
    - column: "Status"
      type: "string"
      filters:
        - operator: "Equals"
          value: "ativo"
          caseInsensitive: true

endpoints:
  - name: "api"
    endpointUrl: "https://api.exemplo.com/users"
    mapping:
      - attribute: "nome"
        csvColumn: "Nome"
      - attribute: "email"
        csvColumn: "Email"
```

**CSV:**

```csv
Nome,Email,Status
João Silva,joao@email.com,ativo
Maria Santos,maria@email.com,inativo
Pedro Costa,pedro@email.com,ATIVO
Ana Lima,ana@email.com,cancelado
```

**Resultado:**
- ✅ João Silva → **Processado**
- ❌ Maria Santos → **Ignorado** (inativo)
- ✅ Pedro Costa → **Processado** (ATIVO = ativo com caseInsensitive)
- ❌ Ana Lima → **Ignorado** (cancelado)

---

### Exemplo 2: Múltiplos Filtros (Operação AND)

**Objetivo:** Processar apenas registros da campanha "promo2024" que **não** estejam cancelados.

```yaml
file:
  mapping:
    # Filtro 1: Campanha específica
    - column: "Campanha"
      type: "string"
      filters:
        - operator: "Equals"
          value: "promo2024"
          caseInsensitive: true
    
    # Filtro 2: Excluir cancelados
    - column: "Status"
      type: "string"
      filters:
        - operator: "NotEquals"
          value: "cancelado"
          caseInsensitive: true

endpoints:
  - name: "marketing"
    endpointUrl: "https://api.marketing.com/contacts"
    mapping:
      - attribute: "nome"
        csvColumn: "Nome"
      - attribute: "email"
        csvColumn: "Email"
```

**CSV:**

```csv
Nome,Email,Campanha,Status
João Silva,joao@email.com,promo2024,ativo
Maria Santos,maria@email.com,promo2024,cancelado
Pedro Costa,pedro@email.com,natal2024,ativo
Ana Lima,ana@email.com,promo2024,pendente
```

**Resultado:**
- ✅ João Silva → **Processado** (promo2024 + ativo)
- ❌ Maria Santos → **Ignorado** (promo2024 + cancelado)
- ❌ Pedro Costa → **Ignorado** (natal2024 + ativo)
- ✅ Ana Lima → **Processado** (promo2024 + pendente)

---

### Exemplo 3: Filtro por Plano Premium

**Objetivo:** Processar apenas clientes com planos que contenham "premium".

```yaml
file:
  mapping:
    - column: "Plano"
      type: "string"
      filters:
        - operator: "Contains"
          value: "premium"
          caseInsensitive: true

endpoints:
  - name: "api"
    endpointUrl: "https://api.exemplo.com/premium"
    mapping:
      - attribute: "nome"
        csvColumn: "Nome"
      - attribute: "plano"
        csvColumn: "Plano"
```

**CSV:**

```csv
Nome,Plano
João Silva,premium
Maria Santos,basic
Pedro Costa,premium-plus
Ana Lima,PREMIUM-GOLD
Carlos Souza,standard
```

**Resultado:**
- ✅ João Silva → **Processado** (premium)
- ❌ Maria Santos → **Ignorado** (basic)
- ✅ Pedro Costa → **Processado** (contém "premium")
- ✅ Ana Lima → **Processado** (PREMIUM-GOLD contém "premium")
- ❌ Carlos Souza → **Ignorado** (standard)

---

### Exemplo 4: Excluir Ambientes de Teste

**Objetivo:** Processar apenas dados de produção, excluindo qualquer coisa com "teste".

```yaml
file:
  mapping:
    - column: "Ambiente"
      type: "string"
      filters:
        - operator: "NotContains"
          value: "teste"
          caseInsensitive: true

endpoints:
  - name: "api"
    endpointUrl: "https://api.exemplo.com/data"
    mapping:
      - attribute: "nome"
        csvColumn: "Nome"
      - attribute: "ambiente"
        csvColumn: "Ambiente"
```

**CSV:**

```csv
Nome,Ambiente
Cliente A,producao
Cliente B,ambiente-teste
Cliente C,homologacao
Cliente D,TESTE-DEV
Cliente E,prod
```

**Resultado:**
- ✅ Cliente A → **Processado** (producao)
- ❌ Cliente B → **Ignorado** (contém "teste")
- ✅ Cliente C → **Processado** (homologacao)
- ❌ Cliente D → **Ignorado** (contém "TESTE")
- ✅ Cliente E → **Processado** (prod)

---

### Exemplo 5: Filtro Complexo - Campanha Premium Ativa

**Objetivo:** Processar apenas registros que sejam da campanha "promo2024", tenham plano "premium" e status "ativo".

```yaml
file:
  mapping:
    - column: "Campanha"
      type: "string"
      filters:
        - operator: "Equals"
          value: "promo2024"
          caseInsensitive: true
    
    - column: "Plano"
      type: "string"
      filters:
        - operator: "Contains"
          value: "premium"
          caseInsensitive: true
    
    - column: "Status"
      type: "string"
      filters:
        - operator: "Equals"
          value: "ativo"
          caseInsensitive: true

endpoints:
  - name: "api"
    endpointUrl: "https://api.exemplo.com/premium-active"
    mapping:
      - attribute: "nome"
        csvColumn: "Nome"
```

**CSV:**

```csv
Nome,Campanha,Plano,Status
João Silva,promo2024,premium,ativo
Maria Santos,promo2024,premium,inativo
Pedro Costa,promo2024,basic,ativo
Ana Lima,natal2024,premium,ativo
Carlos Souza,promo2024,premium,ativo
```

**Análise:**
- ✅ **João Silva**: promo2024 ✓ + premium ✓ + ativo ✓ → **Processado**
- ❌ **Maria Santos**: promo2024 ✓ + premium ✓ + inativo ✗ → **Ignorado**
- ❌ **Pedro Costa**: promo2024 ✓ + basic ✗ + ativo ✓ → **Ignorado**
- ❌ **Ana Lima**: natal2024 ✗ + premium ✓ + ativo ✓ → **Ignorado**
- ✅ **Carlos Souza**: promo2024 ✓ + premium ✓ + ativo ✓ → **Processado**

**Total processado:** 2 linhas (João Silva e Carlos Souza)

---

### Exemplo 6: Múltiplos Filtros na Mesma Coluna (NOVO!)

**Objetivo:** Processar apenas registros que NÃO sejam "cancelado", "inativo" ou "suspenso".

```yaml
file:
  mapping:
    - column: "Status"
      type: "string"
      filters:  # ← Múltiplos filtros na mesma coluna
        - operator: "NotEquals"
          value: "cancelado"
          caseInsensitive: true
        - operator: "NotEquals"
          value: "inativo"
          caseInsensitive: true
        - operator: "NotEquals"
          value: "suspenso"
          caseInsensitive: true

endpoints:
  - name: "api"
    endpointUrl: "https://api.exemplo.com/users"
    mapping:
      - attribute: "nome"
        csvColumn: "Nome"
      - attribute: "status"
        csvColumn: "Status"
```

**CSV:**

```csv
Nome,Status
João Silva,ativo
Maria Santos,cancelado
Pedro Costa,pendente
Ana Lima,INATIVO
Carlos Souza,ativo
Rita Oliveira,suspenso
Paulo Mendes,aprovado
```

**Resultado:**
- ✅ João Silva → **Processado** (ativo - não é cancelado, inativo ou suspenso)
- ❌ Maria Santos → **Ignorado** (cancelado)
- ✅ Pedro Costa → **Processado** (pendente - não é cancelado, inativo ou suspenso)
- ❌ Ana Lima → **Ignorado** (INATIVO)
- ✅ Carlos Souza → **Processado** (ativo)
- ❌ Rita Oliveira → **Ignorado** (suspenso)
- ✅ Paulo Mendes → **Processado** (aprovado)

**Total processado:** 4 linhas

---

### Exemplo 7: Combinando Múltiplos Filtros na Mesma Coluna com Filtros em Outras Colunas

**Objetivo:** Processar registros da campanha "promo2024" que NÃO sejam "cancelado" nem "inativo".

```yaml
file:
  mapping:
    # Filtros múltiplos na coluna Status
    - column: "Status"
      type: "string"
      filters:
        - operator: "NotEquals"
          value: "cancelado"
        - operator: "NotEquals"
          value: "inativo"
    
    # Filtro em outra coluna
    - column: "Campanha"
      type: "string"
      filters:
        - operator: "Equals"
          value: "promo2024"

endpoints:
  - name: "marketing"
    endpointUrl: "https://api.marketing.com/contacts"
    mapping:
      - attribute: "nome"
        csvColumn: "Nome"
```

**CSV:**

```csv
Nome,Campanha,Status
João Silva,promo2024,ativo
Maria Santos,promo2024,cancelado
Pedro Costa,natal2024,ativo
Ana Lima,promo2024,inativo
Carlos Souza,promo2024,pendente
```

**Análise:**
- ✅ **João Silva**: Status ≠ cancelado ✓, Status ≠ inativo ✓, Campanha = promo2024 ✓ → **Processado**
- ❌ **Maria Santos**: Status = cancelado ✗ → **Ignorado**
- ❌ **Pedro Costa**: Campanha ≠ promo2024 ✗ → **Ignorado**
- ❌ **Ana Lima**: Status = inativo ✗ → **Ignorado**
- ✅ **Carlos Souza**: Status ≠ cancelado ✓, Status ≠ inativo ✓, Campanha = promo2024 ✓ → **Processado**

**Total processado:** 2 linhas (João Silva e Carlos Souza)

---

## 🧪 Testando Filtros

### Usar Dry-Run para Validar

Antes de processar dados reais, teste seus filtros com `--dry-run`:

```bash
dotnet run -- --dry-run --verbose
```

**Saída esperada:**

```
🔍 Filtros ativos (3):
  • Coluna 'Campanha' igual a 'promo2024' (ignorar maiúsculas/minúsculas)
  • Coluna 'Plano' contém 'premium' (ignorar maiúsculas/minúsculas)
  • Coluna 'Status' igual a 'ativo' (ignorar maiúsculas/minúsculas)

📊 Total de linhas no CSV: 10
🔍 Total de linhas filtradas: 8
✅ Linhas que serão processadas: 2
```

### Testar com Subset de Dados

```bash
# Testar apenas primeiras 100 linhas
dotnet run -- --max-lines 100 --dry-run --verbose
```

---

## 📊 Visualização de Filtros

### Como os Filtros São Aplicados

```
CSV com 10 linhas
      ↓
   Filtro 1 (Campanha = "promo2024")
      ↓ (6 linhas passaram)
   Filtro 2 (Plano contém "premium")
      ↓ (4 linhas passaram)
   Filtro 3 (Status = "ativo")
      ↓ (2 linhas passaram)
      ↓
   API recebe apenas 2 linhas
```

### Operação AND (E)

Todos os filtros devem ser satisfeitos:

```
Linha passa SE:
  Filtro 1 = TRUE
  E Filtro 2 = TRUE
  E Filtro 3 = TRUE
  E ...
```

---

## ⚙️ Opções de Configuração

### `caseInsensitive`

- **`true`**: Ignora diferenças entre maiúsculas e minúsculas
- **`false`**: Considera maiúsculas e minúsculas diferentes

```yaml
filter:
  operator: "Equals"
  value: "ativo"
  caseInsensitive: true    # "ATIVO", "ativo", "Ativo" são todos iguais
```

```yaml
filter:
  operator: "Equals"
  value: "ativo"
  caseInsensitive: false   # Apenas "ativo" (minúsculo) será aceito
```

---

## 💡 Dicas e Boas Práticas

### ✅ Recomendações

- 💡 **Use `caseInsensitive: true`** para maior flexibilidade
- 💡 **Teste com `--dry-run`** antes de processar dados reais
- 💡 **Combine múltiplos filtros** para critérios complexos
- 💡 **Use `Contains`** para padrões parciais
- 💡 **Monitore logs** para ver quantas linhas foram filtradas

### ⚠️ Cuidados

- ❌ **Cuidado com espaços extras** - `"ativo "` ≠ `"ativo"`
- ❌ **Valide seus filtros** antes de processar grandes volumes
- ❌ **Considere performance** - filtros são aplicados linha por linha
- ❌ **Lembre-se do AND** - todos os filtros devem passar

---

## 🔧 Casos de Uso Avançados

### Processar Apenas Novos Registros

```yaml
file:
  mapping:
    - column: "Processado"
      type: "string"
      filter:
        operator: "NotEquals"
        value: "sim"
        caseInsensitive: true
```

### Filtrar por Período (usando string)

```yaml
file:
  mapping:
    - column: "Mes"
      type: "string"
      filter:
        operator: "Equals"
        value: "2024-01"
```

### Excluir Emails de Teste

```yaml
file:
  mapping:
    - column: "Email"
      type: "string"
      filter:
        operator: "NotContains"
        value: "@teste.com"
        caseInsensitive: true
```

### Processar Apenas Determinados Países

```yaml
file:
  mapping:
    - column: "Pais"
      type: "string"
      filter:
        operator: "Equals"
        value: "Brasil"
        caseInsensitive: true
```

---

## 📈 Monitoramento de Filtros

### Logs com Verbose

Ao usar `--verbose`, você verá:

```
🔍 Aplicando filtros...
📊 Total de linhas no CSV: 1000
🔍 Linhas que passaram nos filtros: 234
⏭️  Linhas filtradas (ignoradas): 766
```

### Arquivos de Log

O arquivo de log contém apenas as linhas que **passaram** nos filtros e foram processadas.

---

## 📝 Arquivo de Teste Incluído

O projeto inclui um arquivo de exemplo para testar filtros:

- **CSV:** `data/exemplo-filtros.csv`
- **Config:** `config-exemplo-filtros.yaml`

```bash
# Testar com arquivo de exemplo
dotnet run -- \
  --config config-exemplo-filtros.yaml \
  --input data/exemplo-filtros.csv \
  --dry-run \
  --verbose
```

---

## 🔄 Diferença entre Filtros e Validações

### Filtros

- ✅ **Silenciosamente ignoram** linhas que não atendem critérios
- ✅ **Não geram erros** no log
- ✅ **Usados para seleção** de dados

### Validações

- ❌ **Geram erros** no log
- ❌ **Indicam dados inválidos**
- ❌ **Usadas para garantir qualidade** dos dados

**Exemplo:**

```yaml
file:
  mapping:
    # VALIDAÇÃO: Email deve ser válido
    - column: "Email"
      type: "string"
      regex: "^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\\.[a-zA-Z]{2,}$"
    
    # FILTRO: Processar apenas status "ativo"
    - column: "Status"
      type: "string"
      filter:
        operator: "Equals"
        value: "ativo"
```

---

## 📚 Documentação Relacionada

- 📖 [README Principal](../README.md)
- 🚀 [Quick Start](QUICKSTART.md)
- 💡 [Exemplos](EXAMPLES.md)
- 🎨 [Transformações](TRANSFORMATIONS.md)
- ⚙️ [Argumentos CLI](CLI-ARGUMENTS.md)

---

<div align="center">
  <p><strong>💡 Precisa de um novo operador de filtro? Abra uma issue no GitHub!</strong></p>
  <p>
    <a href="#-visão-geral">Voltar ao topo ⬆️</a>
  </p>
</div>
