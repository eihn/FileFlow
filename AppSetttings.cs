using Newtonsoft.Json;
using System;
using System.IO;

public class AppSettings
{
    public bool AutoStart { get; set; } = false;
    public bool MinimizedAtStart { get; set; } = true;

    private static string SettingsPath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");

    public static AppSettings Load()
    {
        if (!File.Exists(SettingsPath))
            return new AppSettings();

        try
        {
            string json = File.ReadAllText(SettingsPath);
            return JsonConvert.DeserializeObject<AppSettings>(json);
        }
        catch
        {
            return new AppSettings();
        }
    }

    public void Save()
    {
        string json = JsonConvert.SerializeObject(this, Formatting.Indented);
        File.WriteAllText(SettingsPath, json);
    }
}