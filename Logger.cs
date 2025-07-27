using System;
using System.IO;

public static class Logger
{
    private static string LogFile => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "FileFlow.log");

    public static void Log(string message)
    {
        string line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";
        File.AppendAllText(LogFile, line + Environment.NewLine);
    }

    public static void Info(string msg) => Log($"INFO: {msg}");
    public static void Error(string msg) => Log($"ERROR: {msg}");
}