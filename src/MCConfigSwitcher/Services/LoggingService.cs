using System;
using System.Collections.ObjectModel;

namespace MCConfigSwitcher.Services;

public class LogEntry
{
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public string Level { get; set; } = "INFO";
    public string Message { get; set; } = string.Empty;
}

public class LoggingService
{
    public ObservableCollection<LogEntry> Entries { get; } = new();

    public void Info(string msg) => Add("INFO", msg);
    public void Warn(string msg) => Add("WARN", msg);
    public void Error(string msg) => Add("ERROR", msg);

    private void Add(string level, string msg)
    {
        Entries.Add(new LogEntry { Level = level, Message = msg });
    }
}
