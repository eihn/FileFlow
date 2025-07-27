using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

public class FileRule
{
    public List<string> Extensions { get; set; } = new List<string>();
    public string Source { get; set; } = ""; // New: Source folder for this rule
    public string Destination { get; set; } = "";
    public string Action { get; set; } = "move"; // "move" or "copy"
}

public class AppConfig
{
    public string WatchFolder { get; set; } = "";
    public List<FileRule> Rules { get; set; } = new List<FileRule>();
    public bool CheckOnStartup { get; set; } = true;
    public bool AutoStart { get; set; } = false;
}

public class ConfigManager
{
    public AppConfig Config { get; private set; }
    private readonly string ConfigPath;

    public ConfigManager()
    {
        ConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");
        LoadConfig();
    }

    public void LoadConfig()
    {
        if (!File.Exists(ConfigPath))
            CreateDefaultConfig();

        string json = File.ReadAllText(ConfigPath);
        Config = JsonConvert.DeserializeObject<AppConfig>(json) ?? new AppConfig();
    }

    public void SaveConfig()
    {
        string json = JsonConvert.SerializeObject(Config, Formatting.Indented);
        File.WriteAllText(ConfigPath, json);
    }

    private void CreateDefaultConfig()
    {
        string user = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        Config = new AppConfig
        {
            WatchFolder = Path.Combine(user, "Downloads"),
            Rules = new List<FileRule>
            {
                new FileRule
                {
                    Extensions = new List<string> { ".pdf" },
                    Source = Path.Combine(user, "Downloads"), // Example source folder
                    Destination = Path.Combine(user, "Documents", "PDFs"),
                    Action = "move"
                },
                new FileRule
                {
                    Extensions = new List<string> { ".jpg", ".png", ".jpeg" },
                    Source = Path.Combine(user, "Downloads"), // Example source folder
                    Destination = Path.Combine(user, "Pictures", "FromDownloads"),
                    Action = "move"
                }
            },
            CheckOnStartup = true,
            AutoStart = false
        };

        SaveConfig();
    }
}