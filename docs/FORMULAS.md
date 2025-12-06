# Fórmulas

Este documento descreve as fórmulas disponíveis para uso no mapeamento de API.

## O que são Fórmulas?

Fórmulas são expressões dinâmicas que geram valores calculados em tempo de execução. Ao contrário de `fixedValue` (estático) ou `csvColumn` (vindo do CSV), as fórmulas permitem gerar valores como data/hora atual, identificadores únicos, números aleatórios, etc.

## Como usar

No arquivo de configuração YAML, use a propriedade `formula` no mapeamento:

```yaml
mapping:
  - attribute: "processedAt"
    formula: "now()"
  
  - attribute: "requestId"
    formula: "uuid()"
  
  - attribute: "createdDate"
    formula: "today()"
```

## Fórmulas Disponíveis

### Data e Hora

#### `now()`
Retorna a data e hora atual no formato ISO 8601.
- **Formato padrão**: `yyyy-MM-ddTHH:mm:ssZ`
- **Exemplo**: `2024-12-06T14:30:45Z`

```yaml
- attribute: "processedAt"
  formula: "now()"
```

#### `now('formato')`
Retorna a data e hora atual em um formato customizado.
- **Parâmetro**: formato de data (usar sintaxe .NET DateTime)
- **Exemplo**: `now('dd/MM/yyyy HH:mm:ss')` → `06/12/2024 14:30:45`

```yaml
- attribute: "timestamp"
  formula: "now('yyyy-MM-dd HH:mm:ss')"
```

#### `utcnow()`
Retorna a data e hora atual em UTC.
- **Formato padrão**: `yyyy-MM-ddTHH:mm:ssZ`
- **Exemplo**: `2024-12-06T17:30:45Z`

```yaml
- attribute: "utcTimestamp"
  formula: "utcnow()"
```

#### `utcnow('formato')`
Retorna a data e hora UTC em um formato customizado.

```yaml
- attribute: "utcDate"
  formula: "utcnow('yyyy-MM-dd')"
```

#### `today()`
Retorna a data atual (sem hora).
- **Formato padrão**: `yyyy-MM-dd`
- **Exemplo**: `2024-12-06`

```yaml
- attribute: "createdDate"
  formula: "today()"
```

#### `today('formato')`
Retorna a data atual em um formato customizado.

```yaml
- attribute: "dateFormatted"
  formula: "today('dd/MM/yyyy')"
```

#### `adddays(dias)`
Adiciona ou subtrai dias da data atual.
- **Parâmetro**: número de dias (negativo para subtrair)
- **Formato**: `yyyy-MM-dd`
- **Exemplo**: `adddays(7)` → data daqui a 7 dias
- **Exemplo**: `adddays(-30)` → data de 30 dias atrás

```yaml
- attribute: "expirationDate"
  formula: "adddays(30)"

- attribute: "startDate"
  formula: "adddays(-7)"
```

#### `addhours(horas)`
Adiciona ou subtrai horas da data/hora atual.
- **Parâmetro**: número de horas (negativo para subtrair)
- **Formato**: `yyyy-MM-ddTHH:mm:ssZ`
- **Exemplo**: `addhours(24)` → 24 horas no futuro

```yaml
- attribute: "validUntil"
  formula: "addhours(48)"
```

#### `timestamp()`
Retorna o timestamp Unix atual (segundos desde 1970-01-01).
- **Exemplo**: `1701874245`

```yaml
- attribute: "unixTime"
  formula: "timestamp()"
```

#### `timestamp_ms()`
Retorna o timestamp Unix atual em milissegundos.
- **Exemplo**: `1701874245123`

```yaml
- attribute: "unixTimeMs"
  formula: "timestamp_ms()"
```

### Identificadores Únicos

#### `uuid()` ou `guid()`
Gera um identificador único universal (UUID/GUID).
- **Formato**: `xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx`
- **Exemplo**: `550e8400-e29b-41d4-a716-446655440000`

```yaml
- attribute: "requestId"
  formula: "uuid()"

- attribute: "transactionId"
  formula: "guid()"
```

### Números Aleatórios

#### `random(min, max)`
Gera um número aleatório entre min e max (inclusive).
- **Parâmetros**: 
  - `min`: valor mínimo
  - `max`: valor máximo
- **Exemplo**: `random(1, 100)` → número entre 1 e 100

```yaml
- attribute: "randomCode"
  formula: "random(1000, 9999)"

- attribute: "priority"
  formula: "random(1, 5)"
```

## Combinando Fórmulas com Transformações

Você pode aplicar transformações ao resultado de uma fórmula usando a propriedade `transform` ou `transforms`:

```yaml
- attribute: "processedAtUppercase"
  formula: "now()"
  transform: "uppercase"

- attribute: "requestIdShort"
  formula: "uuid()"
  transforms:
    - "uppercase"
    - "remove-all-spaces"
```

## Exemplos Práticos

### Auditoria de Processamento
```yaml
- attribute: "audit.processedAt"
  formula: "now()"

- attribute: "audit.processingId"
  formula: "uuid()"

- attribute: "audit.batchDate"
  formula: "today()"
```

### Vencimentos e Datas Futuras
```yaml
- attribute: "contract.startDate"
  formula: "today()"

- attribute: "contract.endDate"
  formula: "adddays(365)"

- attribute: "trial.expiresAt"
  formula: "addhours(72)"
```

### Códigos e Identificadores
```yaml
- attribute: "ticket.id"
  formula: "uuid()"

- attribute: "order.confirmationCode"
  formula: "random(100000, 999999)"

- attribute: "transaction.timestamp"
  formula: "timestamp_ms()"
```

## Notas Importantes

1. **Unicidade**: Fórmulas como `uuid()` e `random()` geram valores diferentes a cada execução.

2. **Formato de Data**: Use a sintaxe de formatação .NET DateTime:
   - `yyyy`: ano com 4 dígitos
   - `MM`: mês com 2 dígitos
   - `dd`: dia com 2 dígitos
   - `HH`: hora (24h) com 2 dígitos
   - `mm`: minutos com 2 dígitos
   - `ss`: segundos com 2 dígitos
   - `fff`: milissegundos

3. **Exclusividade**: Um mapeamento deve ter apenas uma fonte de valor:
   - `fixedValue` OU
   - `csvColumn` OU
   - `formula`

4. **Validação**: A configuração será validada ao carregar. Fórmulas inválidas retornarão a string original.

## Diferença entre FixedValue e Formula

| Característica | fixedValue | formula |
|---------------|-----------|---------|
| Valor | Estático | Dinâmico |
| Exemplo | `"BR"` | `now()` |
| Avaliação | Uma vez (na carga) | A cada linha processada |
| Uso | Constantes | Valores que mudam |

### Exemplo de uso combinado:
```yaml
mapping:
  # Valor fixo - sempre o mesmo
  - attribute: "country"
    fixedValue: "BR"
  
  # Valor do CSV - diferente por linha
  - attribute: "customerId"
    csvColumn: "customer_id"
  
  # Fórmula - calculado dinamicamente
  - attribute: "processedAt"
    formula: "now()"
```
