using System.Text.Json;
using GrassMidi.Models;

namespace GrassMidi.Services;

public class ConfigService
{
    private readonly string _filePath = "bindings.json";
    private AppConfig _config;

    public ConfigService()
    {
        _config = LoadConfig();
    }

    public AppConfig GetConfig()
    {
        return _config;
    }

    public void SaveConfig(AppConfig config)
    {
        _config = config;
        try
        {
            var json = JsonSerializer.Serialize(_config, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_filePath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving config: {ex.Message}");
        }
    }

    private AppConfig LoadConfig()
    {
        if (!File.Exists(_filePath))
        {
            return new AppConfig();
        }

        try
        {
            var json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading config: {ex.Message}");
            return new AppConfig();
        }
    }
}
