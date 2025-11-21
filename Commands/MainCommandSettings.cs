using System.ComponentModel;
using Spectre.Console.Cli;

namespace n2n;

public class CommandSettings : Spectre.Console.Cli.CommandSettings
{
    [CommandOption("-c|--config")]
    [Description("Caminho do arquivo de configuração YAML")]
    [DefaultValue("config.yaml")]
    public string ConfigPath { get; set; } = "config.yaml";

    [CommandOption("-i|--input")]
    [Description("Caminho do arquivo CSV de entrada (sobrescreve config)")]
    public string? InputPath { get; set; }

    [CommandOption("-b|--batch-lines")]
    [Description("Número de linhas por lote (sobrescreve config)")]
    public int? BatchLines { get; set; }

    [CommandOption("-l|--log-dir")]
    [Description("Diretório onde os logs serão salvos (sobrescreve config)")]
    public string? LogDirectory { get; set; }

    [CommandOption("-d|--delimiter")]
    [Description("Delimitador do CSV (sobrescreve config)")]
    public string? Delimiter { get; set; }

    [CommandOption("-s|--start-line")]
    [Description("Linha inicial para começar o processamento (sobrescreve config)")]
    public int? StartLine { get; set; }

    [CommandOption("-n|--max-lines")]
    [Description("Número máximo de linhas a processar (sobrescreve config)")]
    public int? MaxLines { get; set; }

    [CommandOption("--exec-id|--execution-id")]
    [Description("UUID da execução para continuar de um checkpoint existente")]
    public string? ExecutionId { get; set; }

    [CommandOption("--endpoint-name")]
    [Description("Nome do endpoint configurado a ser usado (sobrescreve CSV)")]
    public string? EndpointName { get; set; }

    [CommandOption("-v|--verbose")]
    [Description("Exibir logs detalhados")]
    [DefaultValue(false)]
    public bool Verbose { get; set; }

    [CommandOption("--dry-run|--test")]
    [Description("Modo de teste: não faz requisições reais")]
    [DefaultValue(false)]
    public bool DryRun { get; set; }
}