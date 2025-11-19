# 📊 Dashboard de Métricas e Performance

## Visão Geral

O CsvToApi agora inclui um **Dashboard de Performance** completo que exibe métricas em tempo real durante o processamento e um resumo detalhado ao finalizar.

## Funcionalidades

### 1. Métricas em Tempo Real

Durante o processamento, você verá atualizações a cada 5 segundos:

```
⏳ Processadas: 5,234/10,000 | Sucessos: 5,100 | Erros: 134 | 45.2 linhas/seg | 52.3%
```

### 2. Dashboard Final Detalhado

Ao concluir o processamento, um dashboard completo é exibido:

```
═══════════════════════════════════════════════════════════════
                    📊 DASHBOARD DE PERFORMANCE                
═══════════════════════════════════════════════════════════════

📈 PROGRESSO
   Total de Linhas:       10,000
   Linhas Processadas:    10,000 (100.0%)
   Linhas Puladas:        0
   [██████████████████████████████████████████████████] 100.0%

✅ RESULTADOS
   Sucessos:              9,500 (95.0%)
   Erros HTTP:            450 (4.5%)
   Erros de Validação:    50

⏱️  TEMPO
   Tempo Decorrido:       5min 23s
   Velocidade:            31.0 linhas/seg

🌐 PERFORMANCE HTTP
   Tempo Médio:           156 ms
   Tempo Mínimo:          45 ms
   Tempo Máximo:          3,245 ms
   Total de Retries:      23

📦 PROCESSAMENTO EM LOTE
   Batches Processados:   100
   Tempo Médio/Batch:     3,234 ms

📊 CÓDIGOS HTTP
   ✅ 200: 9,250 (92.5%)
   ✅ 201: 250 (2.5%)
   ⚠️ 400: 100 (1.0%)
   ❌ 500: 350 (3.5%)
   ❌ 502: 50 (0.5%)

═══════════════════════════════════════════════════════════════
```

## Métricas Coletadas

### Métricas de Progresso
- **Total de Linhas**: Total de registros no CSV
- **Linhas Processadas**: Registros já processados
- **Linhas Puladas**: Registros ignorados (startLine ou checkpoint)
- **Progresso Percentual**: % de conclusão

### Métricas de Resultado
- **Sucessos**: Requisições HTTP bem-sucedidas (2xx)
- **Erros HTTP**: Falhas de requisição (4xx, 5xx)
- **Erros de Validação**: Registros que falharam na validação antes do envio
- **Taxa de Sucesso**: % de sucessos
- **Taxa de Erro**: % de erros

### Métricas de Tempo
- **Tempo Decorrido**: Duração total do processamento
- **Tempo Restante**: Estimativa de conclusão (durante processamento)
- **Velocidade**: Linhas processadas por segundo

### Métricas HTTP
- **Tempo Médio de Resposta**: Média de todas as requisições
- **Tempo Mínimo**: Requisição mais rápida
- **Tempo Máximo**: Requisição mais lenta
- **Total de Retries**: Quantidade de tentativas de reenvio

### Métricas de Batch
- **Batches Processados**: Quantidade de lotes processados
- **Tempo Médio por Batch**: Duração média de cada lote

### Códigos de Status HTTP
- Distribuição de todos os códigos HTTP recebidos
- Contador e percentual para cada código
- Emoji indicativo (✅ sucesso, ⚠️ warning, ❌ erro)

## Como Funciona

### Coleta Automática

As métricas são coletadas automaticamente durante o processamento:

```csharp
// Sucesso registrado automaticamente
_metricsService.RecordSuccess();

// Erro registrado automaticamente
_metricsService.RecordError();

// Tempo de resposta HTTP registrado
_metricsService.RecordResponseTime(milliseconds);

// Código HTTP registrado
_metricsService.RecordHttpStatusCode(statusCode);
```

### Exibição Progressiva

Durante o processamento:
- **A cada 5 segundos**: Atualização em uma única linha
- **A cada batch**: Contadores são atualizados
- **Ao finalizar**: Dashboard completo é exibido

## Interpretando as Métricas

### Velocidade de Processamento

```
Velocidade: 31.0 linhas/seg
```

**Análise:**
- **< 10 linhas/seg**: API lenta ou rate limiting muito restritivo
- **10-50 linhas/seg**: Velocidade normal para APIs externas
- **50-100 linhas/seg**: Boa performance
- **> 100 linhas/seg**: Excelente performance (API rápida ou local)

### Taxa de Sucesso

```
Sucessos: 9,500 (95.0%)
```

**Análise:**
- **> 95%**: Excelente! Processo estável
- **90-95%**: Bom, mas investigar erros
- **80-90%**: Problemas moderados na API ou dados
- **< 80%**: Problemas graves - revisar configuração

### Tempo de Resposta HTTP

```
Tempo Médio:  156 ms
Tempo Mínimo: 45 ms
Tempo Máximo: 3,245 ms
```

**Análise:**
- **Médio < 200ms**: API rápida
- **Médio 200-500ms**: Performance normal
- **Médio 500-1000ms**: API lenta
- **Médio > 1000ms**: Problemas de performance
- **Máximo muito alto**: Investigar timeouts ou picos de latência

### Códigos HTTP

```
✅ 200: 9,250 (92.5%)
⚠️ 400: 100 (1.0%)
❌ 500: 350 (3.5%)
```

**Análise:**
- **2xx (200, 201)**: Sucesso
- **4xx (400, 401, 404)**: Problemas nos dados ou autenticação
- **5xx (500, 502, 503)**: Problemas no servidor da API

### Total de Retries

```
Total de Retries: 23
```

**Análise:**
- **0 retries**: API estável, sem problemas
- **< 5% das requisições**: Alguns erros temporários normais
- **> 10% das requisições**: API instável, considerar aumentar `retryAttempts`

## Exemplos Práticos

### Exemplo 1: Importação Bem-Sucedida

```
═══════════════════════════════════════════════════════════════
                    📊 DASHBOARD DE PERFORMANCE                
═══════════════════════════════════════════════════════════════

📈 PROGRESSO
   Total de Linhas:       50,000
   Linhas Processadas:    50,000 (100.0%)
   [██████████████████████████████████████████████████] 100.0%

✅ RESULTADOS
   Sucessos:              49,850 (99.7%)
   Erros HTTP:            150 (0.3%)
   Erros de Validação:    0

⏱️  TEMPO
   Tempo Decorrido:       12min 45s
   Velocidade:            65.4 linhas/seg

🌐 PERFORMANCE HTTP
   Tempo Médio:           142 ms
   Tempo Mínimo:          38 ms
   Tempo Máximo:          892 ms
   Total de Retries:      5

📊 CÓDIGOS HTTP
   ✅ 201: 49,850 (99.7%)
   ❌ 500: 150 (0.3%)

═══════════════════════════════════════════════════════════════
```

**Análise**: Processo excelente! Taxa de sucesso de 99.7%, velocidade boa (65 linhas/seg), poucos retries.

### Exemplo 2: Problemas de Validação

```
═══════════════════════════════════════════════════════════════
                    📊 DASHBOARD DE PERFORMANCE                
═══════════════════════════════════════════════════════════════

📈 PROGRESSO
   Total de Linhas:       10,000
   Linhas Processadas:    8,500 (85.0%)

✅ RESULTADOS
   Sucessos:              8,400 (98.8%)
   Erros HTTP:            100 (1.2%)
   Erros de Validação:    1,500

⏱️  TEMPO
   Tempo Decorrido:       3min 12s
   Velocidade:            44.3 linhas/seg

📊 CÓDIGOS HTTP
   ✅ 200: 8,400 (98.8%)
   ⚠️ 400: 100 (1.2%)

═══════════════════════════════════════════════════════════════
```

**Análise**: 1.500 erros de validação! Revisar o CSV ou as regras de validação. Das linhas válidas, 98.8% foram enviadas com sucesso.

### Exemplo 3: API Instável

```
═══════════════════════════════════════════════════════════════
                    📊 DASHBOARD DE PERFORMANCE                
═══════════════════════════════════════════════════════════════

📈 PROGRESSO
   Total de Linhas:       5,000
   Linhas Processadas:    5,000 (100.0%)

✅ RESULTADOS
   Sucessos:              4,200 (84.0%)
   Erros HTTP:            800 (16.0%)
   Erros de Validação:    0

⏱️  TEMPO
   Tempo Decorrido:       25min 34s
   Velocidade:            3.3 linhas/seg

🌐 PERFORMANCE HTTP
   Tempo Médio:           2,456 ms
   Tempo Mínimo:          120 ms
   Tempo Máximo:          30,000 ms
   Total de Retries:      450

📊 CÓDIGOS HTTP
   ✅ 200: 4,200 (84.0%)
   ❌ 500: 350 (7.0%)
   ❌ 502: 250 (5.0%)
   ❌ 503: 200 (4.0%)

═══════════════════════════════════════════════════════════════
```

**Análise**: API muito instável! Taxa de sucesso baixa (84%), muitos retries (450), tempo médio alto (2.4s). Considerar:
- Aumentar `retryAttempts` e `retryDelaySeconds`
- Reduzir `maxRequestsPerSecond` (rate limiting)
- Contatar responsável pela API

## Otimizando com Base nas Métricas

### Se Velocidade Está Baixa

```yaml
# Aumentar paralelismo do batch
file:
    batchLines: 200  # Era 100

# Remover rate limiting se não houver limite
api:
    # maxRequestsPerSecond: 10  # Comentar ou remover
```

### Se Muitos Erros 5xx

```yaml
# Aumentar retries e delay
api:
    retryAttempts: 5      # Era 3
    retryDelaySeconds: 10 # Era 5
```

### Se API Está Sobrecarregada

```yaml
# Adicionar rate limiting
api:
    maxRequestsPerSecond: 5  # Reduzir taxa
    
# Reduzir batch
file:
    batchLines: 50  # Era 100
```

## Exportando Métricas

### Para Arquivo

Redirecione a saída para um arquivo:

```bash
dotnet run -- --config config.yaml > metrics_report.txt
```

### Para Análise

```bash
# Extrair apenas o dashboard
dotnet run -- --config config.yaml 2>&1 | grep -A 50 "DASHBOARD DE PERFORMANCE"

# Ver apenas códigos HTTP
dotnet run -- --config config.yaml 2>&1 | grep -A 10 "CÓDIGOS HTTP"
```

## Integração com Monitoramento

### Prometheus (Futuro)

As métricas estão estruturadas para fácil exportação:

```
csv_to_api_lines_total 10000
csv_to_api_success_total 9500
csv_to_api_error_total 500
csv_to_api_duration_seconds 323
csv_to_api_lines_per_second 31
```

### JSON Export (Futuro)

```json
{
  "totalLines": 10000,
  "processedLines": 10000,
  "successCount": 9500,
  "errorCount": 500,
  "validationErrors": 0,
  "elapsedSeconds": 323,
  "linesPerSecond": 31.0,
  "averageResponseTimeMs": 156,
  "httpStatusCodes": {
    "200": 9250,
    "201": 250,
    "400": 100,
    "500": 350,
    "502": 50
  }
}
```

## Conclusão

O Dashboard de Métricas fornece visibilidade completa do processamento, permitindo:

✅ Monitorar progresso em tempo real  
✅ Identificar problemas rapidamente  
✅ Otimizar configurações com base em dados  
✅ Validar qualidade do processo  
✅ Gerar relatórios de performance  

Use essas métricas para garantir importações eficientes e confiáveis!

---

**Última atualização**: 18 de Novembro de 2025
