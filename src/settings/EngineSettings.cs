public class EngineSettings : IEngineSettings
{
    public const string SHADER_DIR = "/home/ezroot/Repos/Integrity/DefaultEngineAssets/shaders/";
    public const string FILENAME_ENGINE_SETTINGS = "engine.ini";

    [System.Serializable]
    public struct EngineSettingsData
    {
        public string EngineName { get; set; }
        public string EngineVersion { get; set; }
        public int WindowWidth { get; set; }
        public int WindowHeight { get; set; }
        public string WindowTitle { get; set; }
        public EngineSettingsData()
        {
            EngineName = "Integrity2D";
            EngineVersion = "0.1.0";
            WindowWidth = 1280;
            WindowHeight = 720;
            WindowTitle = "Integrity Engine";
        }
    }

    private EngineSettingsData m_SettingsData = new EngineSettingsData();
    public EngineSettingsData Data => m_SettingsData;

    public async Task LoadSettingsAsync()
    {
        string path = Path.Combine(AppContext.BaseDirectory, FILENAME_ENGINE_SETTINGS);
        var tempSettings = m_SettingsData;

        try
        {
            string content = await File.ReadAllTextAsync(path);
            tempSettings.EngineName = ParseSetting(content, "EngineName", tempSettings.EngineName);
            tempSettings.EngineVersion = ParseSetting(content, "EngineVersion", tempSettings.EngineVersion);
            tempSettings.WindowTitle = ParseSetting(content, "WindowTitle", tempSettings.WindowTitle);

            string widthStr = ParseSetting(content, "WindowWidth", tempSettings.WindowWidth.ToString());
            if (int.TryParse(widthStr, out int loadedWidth))
            {
                tempSettings.WindowWidth = loadedWidth;
            }

            string heightStr = ParseSetting(content, "WindowHeight", tempSettings.WindowHeight.ToString());
            if (int.TryParse(heightStr, out int loadedHeight))
            {
                tempSettings.WindowHeight = loadedHeight;
            }

            if (tempSettings.WindowWidth <= 0) tempSettings.WindowWidth = new EngineSettingsData().WindowWidth;
            if (tempSettings.WindowHeight <= 0) tempSettings.WindowHeight = new EngineSettingsData().WindowHeight;

            m_SettingsData = tempSettings;
        }
        catch (FileNotFoundException)
        {
            Logger.Log($"Settings file not found at {path}. Recreating the engine settings.", Logger.LogSeverity.Warning);
            await SaveSettingsAsync();
        }
        catch (Exception ex)
        {
            Logger.Log($"Error loading settings: {ex.Message}. Using defaults.", Logger.LogSeverity.Error);
            await SaveSettingsAsync();
        }
    }

    public async Task SaveSettingsAsync()
    {
        string path = Path.Combine(AppContext.BaseDirectory, FILENAME_ENGINE_SETTINGS);

        var data = m_SettingsData;

        try
        {
            using (var writer = new StreamWriter(path, false))
            {
                await writer.WriteLineAsync($"[Engine]");
                await writer.WriteLineAsync($"EngineName={data.EngineName}");
                await writer.WriteLineAsync($"EngineVersion={data.EngineVersion}");
                await writer.WriteLineAsync($"[Window]");
                await writer.WriteLineAsync($"WindowWidth={data.WindowWidth}");
                await writer.WriteLineAsync($"WindowHeight={data.WindowHeight}");
                await writer.WriteLineAsync($"WindowTitle={data.WindowTitle}");
            }

            Logger.Log($"Settings saved to {path}.", Logger.LogSeverity.Info);
        }
        catch (Exception ex)
        {
            Logger.Log($"Error Saving settings: {ex.Message}", Logger.LogSeverity.Error);
        }
    }


    // Helper method to parse INI key/value pairs
    private string ParseSetting(string content, string key, string defaultValue)
    {
        var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            if (line.StartsWith(";") || line.StartsWith("#") || string.IsNullOrWhiteSpace(line) || line.StartsWith("["))
            {
                continue;
            }

            if (line.StartsWith(key + "=", StringComparison.OrdinalIgnoreCase))
            {
                return line.Substring(key.Length + 1).Trim();
            }
        }
        return defaultValue;
    }
}