# Exemplo de Teste com Filtros

Este diretório contém um exemplo prático de como usar filtros de dados.

## Arquivo CSV de Exemplo

O arquivo `exemplo-filtros.csv` contém 10 linhas de teste:

| #  | Name           | Email                | Campaign  | Status    | Plan    |
|----|----------------|----------------------|-----------|-----------|---------|
| 1  | João Silva     | joao@exemplo.com     | promo2024 | ativo     | premium |
| 2  | Maria Santos   | maria@test.com       | promo2024 | ativo     | basic   |
| 3  | Pedro Oliveira | pedro@exemplo.com    | natal2024 | ativo     | premium |
| 4  | Ana Costa      | ana@exemplo.com      | promo2024 | cancelado | premium |
| 5  | Carlos Lima    | carlos@exemplo.com   | promo2024 | ativo     | premium |
| 6  | Julia Ferreira | julia@exemplo.com    | promo2024 | ativo     | basic   |
| 7  | Roberto Alves  | roberto@exemplo.com  | verao2024 | ativo     | premium |
| 8  | Fernanda Souza | fernanda@exemplo.com | promo2024 | suspenso  | premium |
| 9  | Lucas Pereira  | lucas@exemplo.com    | promo2024 | ativo     | premium |
| 10 | Patricia Rocha | patricia@example.com | promo2024 | ativo     | premium |

## Filtros Configurados

O arquivo `config-exemplo-filtros.yaml` tem os seguintes filtros:

```yaml
filters:
  # Filtro 1: Apenas campanha "promo2024"
  - column: "campaign"
    operator: "Equals"
    value: "promo2024"
    caseInsensitive: true
  
  # Filtro 2: Excluir status "cancelado"
  - column: "status"
    operator: "NotEquals"
    value: "cancelado"
    caseInsensitive: true
  
  # Filtro 3: Apenas planos que contenham "premium"
  - column: "plan"
    operator: "Contains"
    value: "premium"
    caseInsensitive: true
```

## Resultado Esperado

Com os filtros acima, apenas **3 linhas** serão processadas:

### ✅ Linhas que PASSAM nos filtros (serão processadas):

- **Linha 1** - João Silva
    - ✓ campaign = "promo2024"
    - ✓ status = "ativo" (diferente de "cancelado")
    - ✓ plan = "premium" (contém "premium")

- **Linha 5** - Carlos Lima
    - ✓ campaign = "promo2024"
    - ✓ status = "ativo" (diferente de "cancelado")
    - ✓ plan = "premium" (contém "premium")

- **Linha 9** - Lucas Pereira
    - ✓ campaign = "promo2024"
    - ✓ status = "ativo" (diferente de "cancelado")
    - ✓ plan = "premium" (contém "premium")

### ❌ Linhas que NÃO PASSAM nos filtros (serão ignoradas):

- **Linha 2** - Maria Santos
    - ✓ campaign = "promo2024"
    - ✓ status = "ativo"
    - ✗ plan = "basic" (não contém "premium")

- **Linha 3** - Pedro Oliveira
    - ✗ campaign = "natal2024" (diferente de "promo2024")
    - ✓ status = "ativo"
    - ✓ plan = "premium"

- **Linha 4** - Ana Costa
    - ✓ campaign = "promo2024"
    - ✗ status = "cancelado" (igual a "cancelado")
    - ✓ plan = "premium"

- **Linha 6** - Julia Ferreira
    - ✓ campaign = "promo2024"
    - ✓ status = "ativo"
    - ✗ plan = "basic" (não contém "premium")

- **Linha 7** - Roberto Alves
    - ✗ campaign = "verao2024" (diferente de "promo2024")
    - ✓ status = "ativo"
    - ✓ plan = "premium"

- **Linha 8** - Fernanda Souza
    - ✓ campaign = "promo2024"
    - ✗ status = "suspenso" (mas o filtro é apenas para "cancelado", então passa)
    - ✗ Mas falha na validação de email (suspenso não é "cancelado", então passaria, mas...)
    - Aguarde, vamos recalcular...
    - ✓ campaign = "promo2024"
    - ✓ status = "suspenso" (diferente de "cancelado")
    - ✓ plan = "premium"
    - ✓ **NA VERDADE ESTA LINHA PASSA!**

- **Linha 10** - Patricia Rocha
    - ✓ campaign = "promo2024"
    - ✓ status = "ativo"
    - ✓ plan = "premium"
    - ✓ **ESTA LINHA TAMBÉM PASSA!**

## Correção: Resultado Real

Linhas processadas: **5 linhas** (1, 5, 8, 9, 10)

- Linha 1: João Silva ✅
- Linha 5: Carlos Lima ✅
- Linha 8: Fernanda Souza ✅ (suspenso ≠ cancelado)
- Linha 9: Lucas Pereira ✅
- Linha 10: Patricia Rocha ✅

Linhas filtradas: **5 linhas** (2, 3, 4, 6, 7)

## Como Executar o Teste

```bash
# Executar com o arquivo de exemplo
dotnet run -- --config config-exemplo-filtros.yaml --input data/exemplo-filtros.csv --dry-run

# Você verá:
# 🔍 Filtros ativos (3):
#   - Coluna 'campaign' igual a 'promo2024' (ignorar maiúsculas/minúsculas)
#   - Coluna 'status' diferente de 'cancelado' (ignorar maiúsculas/minúsculas)
#   - Coluna 'plan' contém 'premium' (ignorar maiúsculas/minúsculas)
#
# 🔍 Total de linhas filtradas: 5
```

## Experimente

Você pode modificar o arquivo `config-exemplo-filtros.yaml` para testar diferentes filtros:

### Teste 1: Processar todas as campanhas "2024"

```yaml
filters:
  - column: "campaign"
    operator: "Contains"
    value: "2024"
```

**Resultado**: 10 linhas (todas têm campanhas com "2024")

### Teste 2: Excluir emails de teste

```yaml
filters:
  - column: "email"
    operator: "NotContains"
    value: "test"
  - column: "email"
    operator: "NotContains"
    value: "example"
```

**Resultado**: 8 linhas (exclui linhas 2 e 10)

### Teste 3: Apenas planos básicos

```yaml
filters:
  - column: "plan"
    operator: "Equals"
    value: "basic"
```

**Resultado**: 2 linhas (linhas 2 e 6)
