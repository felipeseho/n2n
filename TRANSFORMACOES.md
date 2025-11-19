# 🔄 Transformações de Dados

## Visão Geral

A funcionalidade de **Transformações de Dados** permite modificar valores de colunas CSV antes de enviá-los para a API. Você pode aplicar transformações como converter para maiúsculas, minúsculas, remover espaços, formatar CPF/CNPJ, e muito mais.

## Como Usar

Adicione a propriedade `transform` no mapeamento do endpoint no arquivo `config.yaml`:

```yaml
endpoints:
  - name: "api-principal"
    endpointUrl: "https://api.exemplo.com/upload"
    method: "POST"
    mapping:
      - attribute: "name"
        csvColumn: "Name"
        transform: "uppercase"
      
      - attribute: "email"
        csvColumn: "Email"
        transform: "lowercase"
      
      - attribute: "address.street"
        csvColumn: "Street"
        transform: "title-case"
```

## Transformações Disponíveis

### Transformações de Texto

- **`uppercase`**: Converte texto para MAIÚSCULAS
  ```yaml
  transform: "uppercase"
  # "João Silva" → "JOÃO SILVA"
  ```

- **`lowercase`**: Converte texto para minúsculas
  ```yaml
  transform: "lowercase"
  # "João Silva" → "joão silva"
  ```

- **`capitalize`**: Primeira letra maiúscula, restante minúscula
  ```yaml
  transform: "capitalize"
  # "joão SILVA" → "João silva"
  ```

- **`title-case`**: Primeira Letra De Cada Palavra Maiúscula
  ```yaml
  transform: "title-case"
  # "joão silva" → "João Silva"
  ```

### Limpeza de Dados

- **`trim`**: Remove espaços no início e fim
  ```yaml
  transform: "trim"
  # "  João  " → "João"
  ```

- **`remove-spaces`**: Remove todos os espaços
  ```yaml
  transform: "remove-spaces"
  # "João Silva" → "JoãoSilva"
  ```

- **`remove-all-spaces`**: Remove todos os espaços em branco (incluindo tabs, quebras de linha)
  ```yaml
  transform: "remove-all-spaces"
  # "João  Silva\n" → "JoãoSilva"
  ```

- **`remove-accents`**: Remove acentos e caracteres especiais
  ```yaml
  transform: "remove-accents"
  # "João José" → "Joao Jose"
  ```

- **`remove-non-numeric`**: Remove todos os caracteres não numéricos
  ```yaml
  transform: "remove-non-numeric"
  # "123.456.789-00" → "12345678900"
  ```

- **`remove-non-alphanumeric`**: Remove caracteres especiais, mantém letras e números
  ```yaml
  transform: "remove-non-alphanumeric"
  # "João-Silva_123!" → "JoãoSilva123"
  ```

### Formatações Brasileiras

- **`format-cpf`**: Formata CPF (000.000.000-00)
  ```yaml
  transform: "format-cpf"
  # "12345678900" → "123.456.789-00"
  ```

- **`format-cnpj`**: Formata CNPJ (00.000.000/0000-00)
  ```yaml
  transform: "format-cnpj"
  # "12345678000190" → "12.345.678/0001-90"
  ```

- **`format-phone-br`**: Formata telefone brasileiro
  ```yaml
  transform: "format-phone-br"
  # "11987654321" → "(11) 98765-4321"
  # "1134567890" → "(11) 3456-7890"
  ```

- **`format-cep`**: Formata CEP (00000-000)
  ```yaml
  transform: "format-cep"
  # "01310100" → "01310-100"
  ```

### Outras Transformações

- **`slugify`**: Converte para formato slug (URL-friendly)
  ```yaml
  transform: "slugify"
  # "João José da Silva!" → "joao-jose-da-silva"
  ```

- **`reverse`**: Inverte a string
  ```yaml
  transform: "reverse"
  # "ABC123" → "321CBA"
  ```

- **`base64-encode`**: Codifica em Base64
  ```yaml
  transform: "base64-encode"
  # "Hello" → "SGVsbG8="
  ```

- **`url-encode`**: Codifica para URL
  ```yaml
  transform: "url-encode"
  # "João Silva" → "Jo%C3%A3o%20Silva"
  ```

## Exemplos Práticos

### Exemplo 1: E-commerce - Normalização de Produtos

```yaml
endpoints:
  - name: "produtos"
    endpointUrl: "https://api.loja.com/produtos"
    method: "POST"
    mapping:
      - attribute: "title"
        csvColumn: "Nome Produto"
        transform: "title-case"
      
      - attribute: "sku"
        csvColumn: "SKU"
        transform: "uppercase"
      
      - attribute: "slug"
        csvColumn: "Nome Produto"
        transform: "slugify"
      
      - attribute: "description"
        csvColumn: "Descricao"
        transform: "trim"
```

**CSV:**
```
Código,Nome do Produto,Descrição
abc123,camiseta básica branca,  Camiseta 100% algodão  
```

**Payload enviado:**
```json
{
  "sku": "ABC123",
  "name": "Camiseta Básica Branca",
  "slug": "camiseta-basica-branca",
  "description": "Camiseta 100% algodão"
}
```

### Exemplo 2: CRM - Normalização de Clientes

```yaml
endpoints:
  - name: "clientes"
    endpointUrl: "https://api.crm.com/clientes"
    method: "POST"
    mapping:
      - attribute: "name"
        csvColumn: "Nome"
        transform: "title-case"
      
      - attribute: "email"
        csvColumn: "Email"
        transform: "lowercase"
      
      - attribute: "cpf"
        csvColumn: "CPF"
        transform: "format-cpf"
      
      - attribute: "phone"
        csvColumn: "Telefone"
        transform: "format-phone-br"
      
      - attribute: "zipcode"
        csvColumn: "CEP"
        transform: "format-cep"
```

**CSV:**
```
Nome,Email,CPF,Telefone,CEP
joão silva,JOAO@EMAIL.COM,12345678900,11987654321,01310100
```

**Payload enviado:**
```json
{
  "name": "João Silva",
  "email": "joao@email.com",
  "cpf": "123.456.789-00",
  "phone": "(11) 98765-4321",
  "zipcode": "01310-100"
}
```

### Exemplo 3: RH - Importação de Funcionários

```yaml
endpoints:
  - name: "funcionarios"
    endpointUrl: "https://api.rh.com/funcionarios"
    method: "POST"
    mapping:
      - attribute: "fullName"
        csvColumn: "Nome Completo"
        transform: "title-case"
      
      - attribute: "department"
        csvColumn: "Departamento"
        transform: "uppercase"
      
      - attribute: "email"
        csvColumn: "Email Corporativo"
        transform: "lowercase"
      
      - attribute: "badge"
        csvColumn: "Matrícula"
        transform: "remove-non-numeric"
```

**CSV:**
```
Nome Completo,Departamento,Email Corporativo,Matrícula
maria josé santos,tecnologia,MARIA.SANTOS@EMPRESA.COM,EMP-001234
```

**Payload enviado:**
```json
{
  "fullName": "Maria José Santos",
  "department": "TECNOLOGIA",
  "email": "maria.santos@empresa.com",
  "badge": "001234"
}
```

## Combinando Transformações com Validações

Você pode usar transformações junto com validações. A transformação é aplicada **antes** do envio para a API, mas as validações continuam sendo feitas com o valor original do CSV:

```yaml
file:
  mapping:
    - column: "Email"
      type: "string"
      regex: "^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\\.[a-zA-Z]{2,}$"

endpoints:
  - name: "api-principal"
    mapping:
      - attribute: "email"
        csvColumn: "Email"
        transform: "lowercase"  # Será enviado em minúsculas
```

## Observações Importantes

1. **Case-insensitive**: O nome da transformação não diferencia maiúsculas/minúsculas
2. **Valores vazios**: Se o valor for vazio/nulo, a transformação é ignorada
3. **Transformação opcional**: Se não especificar `transform`, o valor é enviado como está no CSV
4. **Valores inválidos**: Se a formatação falhar (ex: CPF com tamanho errado), retorna o valor original
5. **Combinações**: Não é possível aplicar múltiplas transformações em sequência (escolha uma por campo)

## Adicionando Novas Transformações

Para adicionar uma nova transformação, edite o arquivo `Utils/DataTransformer.cs` e adicione um novo caso no switch:

```csharp
return transform.ToLower() switch
{
    // ... transformações existentes ...
    "minha-transformacao" => MinhaFuncaoDeTransformacao(value),
    _ => value
};
```

## Performance

As transformações são aplicadas durante o processamento de cada linha, antes do envio para a API. O impacto na performance é mínimo, mas considere:

- Transformações simples (uppercase, lowercase, trim): < 1ms por registro
- Formatações complexas (regex, CPF/CNPJ): 1-5ms por registro
- Base64/URL encoding: 1-3ms por registro

Para arquivos muito grandes (> 1 milhão de linhas), monitore o tempo total de processamento.
