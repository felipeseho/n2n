using System.Collections.Concurrent;
using n2n.Core;
using n2n.Models;

namespace n2n.Services;

/// <summary>
///     ViewModel para gerenciar estado e dados do Dashboard
/// </summary>
public class DashboardViewModel
{
    private readonly object _lock = new();
    private const int MaxLogEntries = 15;

    public string ApplicationName { get; set; } = string.Empty;
    public string ApplicationVersion { get; set; } = string.Empty;
    public string ConfigFileName { get; set; } = string.Empty;
    public string ExecutionName { get; set; } = string.Empty;
    public string ExecutionDescription { get; set; } = string.Empty;
    public string ExecutionId { get; set; } = string.Empty;
    public DashboardStatus Status { get; set; } = DashboardStatus.Running;

    public PipelineConfiguration Configuration { get; set; } = null!;
    public IDataSource? Source { get; set; }
    public IDataDestination? Destination { get; set; }
    public long? EstimatedTotal { get; set; }

    public Queue<string> SourceLogs { get; } = new();
    public Queue<string> DestinationLogs { get; } = new();
    public Queue<string> GlobalLogs { get; } = new();

    public void AddSourceLog(string message, LogLevel level = LogLevel.Info)
    {
        var formattedLog = FormatLog(message, level);
        lock (_lock)
        {
            SourceLogs.Enqueue(formattedLog);
            if (SourceLogs.Count > MaxLogEntries)
                SourceLogs.Dequeue();
        }
    }

    public void AddDestinationLog(string message, LogLevel level = LogLevel.Info)
    {
        var formattedLog = FormatLog(message, level);
        lock (_lock)
        {
            DestinationLogs.Enqueue(formattedLog);
            if (DestinationLogs.Count > MaxLogEntries)
                DestinationLogs.Dequeue();
        }
    }

    public void AddGlobalLog(string message, LogLevel level = LogLevel.Info)
    {
        var formattedLog = FormatLog(message, level);
        lock (_lock)
        {
            GlobalLogs.Enqueue(formattedLog);
            if (GlobalLogs.Count > MaxLogEntries)
                GlobalLogs.Dequeue();
        }
    }

    public string[] GetSourceLogs()
    {
        lock (_lock)
        {
            return SourceLogs.ToArray();
        }
    }

    public string[] GetDestinationLogs()
    {
        lock (_lock)
        {
            return DestinationLogs.ToArray();
        }
    }

    public string[] GetGlobalLogs()
    {
        lock (_lock)
        {
            return GlobalLogs.ToArray();
        }
    }

    private string FormatLog(string message, LogLevel level)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        var (color, icon) = level switch
        {
            LogLevel.Error => ("red", "❌"),
            LogLevel.Warning => ("yellow", "⚠️"),
            LogLevel.Success => ("green", "✅"),
            LogLevel.Info => ("cyan1", "ℹ️"),
            _ => ("grey", "•")
        };

        return $"[grey]{timestamp}[/] [{color}]{icon}[/] {message}";
    }
}

public enum DashboardStatus
{
    Running,
    Completed,
    Error,
    Paused
}

public enum LogLevel
{
    Error,
    Warning,
    Success,
    Info,
    Debug
}
