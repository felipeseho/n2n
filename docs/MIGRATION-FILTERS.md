# 🔍 Guia de Migração - Múltiplos Filtros

## ⚠️ Mudança Importante (Breaking Change)

A partir da versão **0.11.0**, o formato de filtros foi **simplificado** e a compatibilidade com o formato antigo foi removida.

## 🔄 Como Migrar

### ❌ Formato Antigo (NÃO funciona mais)

```yaml
- column: "Status"
  type: "string"
  filter:                    # ← Singular
    operator: "Equals"
    value: "ativo"
```

### ✅ Formato Novo (Obrigatório)

```yaml
- column: "Status"
  type: "string"
  filters:                   # ← Plural
    - operator: "Equals"     # ← Note o hífen
      value: "ativo"
```

## ✨ Vantagens do Novo Formato

### 1. Múltiplos Filtros na Mesma Coluna

Agora você pode aplicar vários filtros na mesma coluna:

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

### 2. Lógica AND Automática

Todos os filtros são aplicados com lógica **AND** - a linha só é processada se passar em **TODOS** os filtros.

### 3. Combinação com Filtros em Outras Colunas

```yaml
columns:
  # Filtros múltiplos na coluna Status
  - column: "Status"
    type: "string"
    filters:
      - operator: "NotEquals"
        value: "cancelado"
      - operator: "NotEquals"
        value: "inativo"
  
  # Filtro em outra coluna
  - column: "Plan"
    type: "string"
    filters:
      - operator: "Contains"
        value: "premium"
```

## 📋 Checklist de Migração

1. ✅ Abra seus arquivos de configuração YAML
2. ✅ Encontre todas as ocorrências de `filter:` (singular)
3. ✅ Substitua por `filters:` (plural)
4. ✅ Adicione um hífen `-` antes de `operator:`
5. ✅ Indente corretamente (operator e value devem estar alinhados)
6. ✅ Teste com `--dry-run` antes de executar

## 🎯 Exemplo Completo

### Antes (não funciona mais):

```yaml
file:
  columns:
    - column: "Status"
      type: "string"
      filter:
        operator: "Equals"
        value: "ativo"
```

### Depois (novo formato):

```yaml
file:
  columns:
    - column: "Status"
      type: "string"
      filters:
        - operator: "Equals"
          value: "ativo"
```

## 📚 Mais Informações

- Consulte `docs/FILTERS.md` para documentação completa
- Veja `config-exemplo-filtros.yaml` para um exemplo funcional
- Use `--dry-run` para testar suas configurações sem processar dados

## ❓ Dúvidas?

Todos os exemplos na documentação foram atualizados. Consulte:
- `docs/FILTERS.md` - Documentação completa de filtros
- `docs/EXAMPLES.md` - Exemplos práticos
- `docs/CHANGELOG.md` - Histórico de mudanças
