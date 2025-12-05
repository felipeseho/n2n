using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using CsvHelper;
using CsvHelper.Configuration;
using n2n.Abstractions;

namespace n2n.Plugins.Csv;

public sealed class CsvDestinationConfig
{
    public required string Path { get; init; }
    public string Delimiter { get; init; } = ",";
    public bool AppendMode { get; init; }
    public string Encoding { get; init; } = "UTF-8";
}

public sealed class CsvDestination : IDestinationPlugin
{
    private readonly SemaphoreSlim _writeLock = new(1, 1);

    public async Task Load(JsonNode data, JsonNode pluginConfig, CancellationToken ct)
    {
        var config = JsonSerializer.Deserialize<CsvDestinationConfig>(pluginConfig) 
            ?? throw new InvalidOperationException("Invalid CsvDestination configuration");

        await _writeLock.WaitAsync(ct);
        
        try
        {
            var csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = config.Delimiter
            };

            var encoding = Encoding.GetEncoding(config.Encoding);
            var fileExists = File.Exists(config.Path);
            var shouldWriteHeader = !fileExists || !config.AppendMode;

            var fileMode = config.AppendMode && fileExists ? FileMode.Append : FileMode.Create;
            
            await using var stream = new FileStream(config.Path, fileMode, FileAccess.Write, FileShare.None);
            await using var writer = new StreamWriter(stream, encoding);
            await using var csv = new CsvWriter(writer, csvConfig);

            if (data is not JsonObject jsonObject)
                throw new InvalidOperationException("CSV destination expects JsonObject data");

            if (shouldWriteHeader)
            {
                foreach (var property in jsonObject)
                {
                    csv.WriteField(property.Key);
                }
                await csv.NextRecordAsync();
            }

            foreach (var property in jsonObject)
            {
                var value = property.Value?.ToString() ?? string.Empty;
                csv.WriteField(value);
            }
            
            await csv.NextRecordAsync();
            await csv.FlushAsync();
        }
        finally
        {
            _writeLock.Release();
        }
    }
}