# Dashboard em Tempo Real

## Visão Geral

O CSV to API agora conta com um **dashboard interativo em tempo real** que exibe todas as informações importantes
durante o processamento dos arquivos CSV. O dashboard é dividido em seções organizadas para facilitar o acompanhamento
do progresso.

## Estrutura do Dashboard

### 📋 Seções Principais

O dashboard é dividido em **4 seções principais**:

#### 1. ⚙️ IMPORTAÇÃO

Exibe informações sobre a execução atual:

- **Execution ID**: Identificador único da execução (UUID)
- **Checkpoint**: Status do checkpoint (Ativo/Não configurado)
- **Start Line**: Linha inicial do processamento
- **Batch Size**: Número de linhas processadas por lote
- **Max Lines**: Limite máximo de linhas a processar (ou Ilimitado)

#### 2. 📄 ARQUIVO

Informações sobre o arquivo CSV sendo processado:

- **Arquivo**: Nome do arquivo CSV
- **Tamanho**: Tamanho do arquivo (formatado em B, KB, MB ou GB)
- **Total Linhas**: Número total de linhas no arquivo
- **Filtros**: Resumo dos filtros aplicados (se houver)
- **Filtradas**: Quantidade de linhas que foram filtradas

#### 3. 🌐 ENDPOINT

Detalhes do endpoint da API:

- **Endereço**: URL do endpoint
- **Método**: Método HTTP (POST, PUT, etc.)
- **Timeout**: Tempo limite de requisição (em segundos)
- **Retry**: Número de tentativas em caso de falha

#### 4. 📊 PROGRESSO

Acompanhamento em tempo real do processamento:

**Barra de Progresso Visual**

- Barra gráfica mostrando o percentual de conclusão
- Percentual exato do progresso

**Estatísticas de Processamento**

- **Processadas**: Linhas processadas / Total de linhas
- **✓ Sucessos**: Quantidade e percentual de sucessos
- **✗ Erros**: Quantidade e percentual de erros
- **⚠ Validação**: Erros de validação (se houver)
- **⏭️ Puladas**: Linhas puladas (se houver)

**Tempo**

- **⏱️ Decorrido**: Tempo total desde o início
- **⏳ Estimado**: Tempo estimado restante
- **🚀 Velocidade**: Linhas processadas por segundo

**Performance HTTP**

- **Tempo Médio**: Tempo médio de resposta das requisições
- **Min / Max**: Menor e maior tempo de resposta
- **Batches**: Número de batches processados
- **Retries**: Total de tentativas de retry

### 📊 CÓDIGOS HTTP (Rodapé)

Exibe a distribuição dos códigos HTTP de status recebidos:

- Códigos coloridos por categoria (2xx verde, 4xx amarelo, 5xx vermelho)
- Quantidade e percentual de cada código
- Organizado em múltiplas colunas para facilitar visualização

## Características

### ✨ Atualização em Tempo Real

- O dashboard atualiza automaticamente a cada **500ms**
- Não é necessário interação do usuário
- Todas as métricas são atualizadas dinamicamente

### 🎨 Interface Visual

- Utiliza **Spectre.Console** para renderização
- **Cores** para facilitar identificação rápida:
    - 🟦 Azul ciano: Informações gerais
    - 🟩 Verde: Sucessos e valores positivos
    - 🟥 Vermelho: Erros
    - 🟨 Amarelo: Avisos e validações
    - ⬜ Cinza: Informações secundárias
- **Emojis** para melhor visualização
- **Layout organizado** em painéis com bordas

### 📈 Dashboard Final

Ao término do processamento:

- O dashboard em tempo real é parado
- Um **snapshot final** é exibido
- **Métricas detalhadas** são apresentadas em tabelas
- Incluindo gráficos de barras e distribuição de status HTTP

## Exemplo de Uso

```bash
# Executar processamento normal (dashboard será exibido automaticamente)
dotnet run -- --config config.yaml

# Com parâmetros personalizados
dotnet run -- --config config.yaml --batch-lines 100 --max-lines 1000
```

## Benefícios

1. **Visibilidade Total**: Todas as informações importantes em um único lugar
2. **Acompanhamento em Tempo Real**: Veja o progresso acontecendo
3. **Identificação Rápida de Problemas**: Erros e métricas destacadas
4. **Estimativas Precisas**: Tempo restante calculado dinamicamente
5. **Performance Monitoring**: Métricas de performance HTTP em tempo real
6. **Organização**: Layout dividido em seções lógicas

## Informações Técnicas

### Arquivos Modificados/Criados

- `Services/DashboardService.cs` - Novo serviço para gerenciar o dashboard
- `Services/CsvProcessorService.cs` - Integração com o dashboard
- `Services/MetricsService.cs` - Adicionado método para linhas filtradas
- `Models/ProcessingMetrics.cs` - Adicionada propriedade FilteredLines
- `Models/ExecutionPaths.cs` - Adicionada propriedade ExecutionId

### Tecnologias

- **Spectre.Console**: Para renderização do dashboard
- **Layout API**: Para organização em seções
- **Live Display**: Para atualização em tempo real
- **Task.Run**: Para execução em background

## Notas

- O dashboard funciona melhor em terminais com suporte a cores ANSI
- A atualização é assíncrona e não bloqueia o processamento
- Em caso de erros, o dashboard é parado gracefully
- Todas as informações continuam sendo salvas nos logs normalmente
