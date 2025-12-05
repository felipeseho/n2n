using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using CsvHelper;
using CsvHelper.Configuration;
using n2n.Abstractions;
using n2n.Plugins;

namespace n2n.Plugins.Csv;

public sealed class CsvSourceConfig
{
    public required string Path { get; init; }
    public string Delimiter { get; init; } = ",";
    public bool HasHeaderRecord { get; init; } = true;
    public string Encoding { get; init; } = "UTF-8";
}

public sealed class CsvSource : ISourcePlugin
{
    public async IAsyncEnumerable<JsonNode> Extract(
        JsonNode pluginConfig, 
        [EnumeratorCancellation] CancellationToken ct)
    {
        var config = JsonSerializer.Deserialize<CsvSourceConfig>(pluginConfig) 
            ?? throw new InvalidOperationException("Invalid CsvSource configuration");

        var csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = config.Delimiter,
            HasHeaderRecord = config.HasHeaderRecord
        };

        var encoding = Encoding.GetEncoding(config.Encoding);
        
        using var reader = new StreamReader(config.Path, encoding);
        using var csv = new CsvReader(reader, csvConfig);

        await csv.ReadAsync();
        csv.ReadHeader();
        var headers = csv.HeaderRecord ?? throw new InvalidOperationException("CSV has no headers");

        while (await csv.ReadAsync())
        {
            ct.ThrowIfCancellationRequested();

            var jsonObject = new JsonObject();
            
            foreach (var header in headers)
            {
                var value = csv.GetField(header);
                jsonObject[header] = value != null ? JsonValue.Create(value) : null;
            }

            yield return jsonObject;
        }
    }
}