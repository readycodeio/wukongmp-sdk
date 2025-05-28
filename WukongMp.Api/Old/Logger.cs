using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;

namespace WukongMp.Api.Old;

public class Logger : IDisposable
{
    private readonly ConcurrentQueue<string> _logQueue = new();
    private readonly AutoResetEvent _logSignal = new(false);
    private readonly Thread _logThread;
    private readonly string _logDirectory;
    private string _currentLogFile;
    private const long MaxLogFileSize = 5 * 1024 * 1024; // 5 MB
    private volatile bool _isRunning = true;
    
    private static Guid SessionId { get; } = Guid.NewGuid();

    public static Logger Instance { get; } = new("wukong-mp-logs");

    private Logger(string logDirectory)
    {
        _logDirectory = logDirectory;
        Directory.CreateDirectory(logDirectory);
        _currentLogFile = GetNewLogFilePath();

        _logThread = new Thread(ProcessLogQueue) { IsBackground = true };
        _logThread.Start();
    }

    public void Log(string messageTemplate, Dictionary<string, object?> properties, string level)
    {
        var logEntry = new
        {
            TimeGenerated = DateTime.UtcNow.ToString("o"),
            Level = level,
            MessageTemplate = messageTemplate,
            Properties = properties,
            Session = SessionId,
        };

        var logJson = JsonSerializer.Serialize(logEntry);
        _logQueue.Enqueue(logJson);
        _logSignal.Set();
    }

    private void ProcessLogQueue()
    {
        while (_isRunning || !_logQueue.IsEmpty)
        {
            _logSignal.WaitOne();
            WriteLogsToFile();
        }
    }

    private void WriteLogsToFile()
    {
        while (_logQueue.TryDequeue(out var logEntry))
        {
            RotateLogFileIfNeeded();

            using var fileStream = new FileStream(_currentLogFile, FileMode.Append, FileAccess.Write, FileShare.Read);
            using var writer = new StreamWriter(fileStream);
            writer.AutoFlush = true;
            writer.WriteLine(logEntry);
        }
    }

    private void RotateLogFileIfNeeded()
    {
        FileInfo fileInfo = new(_currentLogFile);
        if (fileInfo is { Exists: true, Length: >= MaxLogFileSize })
        {
            _currentLogFile = GetNewLogFilePath();
        }
    }

    private string GetNewLogFilePath()
    {
        return Path.Combine(_logDirectory, $"log_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json");
    }

    public void Dispose()
    {
        _isRunning = false;
        _logSignal.Set();
        _logThread.Join();
    }
}