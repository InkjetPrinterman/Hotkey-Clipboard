using System.Text.Json;

namespace HotkeyClipboard
{
    /// <summary>Loads/saves app settings to %AppData%\HotkeyClipboard\settings.json.</summary>
    public class SettingsStore
    {
        private readonly string filePath;
        public AppSettings Settings { get; private set; } = new();

        public SettingsStore()
        {
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "HotkeyClipboard");
            Directory.CreateDirectory(dir);
            filePath = Path.Combine(dir, "settings.json");
            Load();
        }

        public void Load()
        {
            try
            {
                if (File.Exists(filePath))
                {
                    var json = File.ReadAllText(filePath);
                    Settings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                }
            }
            catch
            {
                Settings = new AppSettings();
            }
        }

        public void Save()
        {
            try
            {
                var json = JsonSerializer.Serialize(Settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(filePath, json);
            }
            catch
            {
                // Best-effort; ignore transient IO failures.
            }
        }
    }
}
