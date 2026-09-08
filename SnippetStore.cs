using System.Text.Json;

namespace HotkeyClipboard
{
    /// <summary>Loads/saves the list of saved snippets to %AppData%\HotkeyClipboard\snippets.json.</summary>
    public class SnippetStore
    {
        private readonly string filePath;
        public List<Snippet> Snippets { get; private set; } = new();

        public SnippetStore()
        {
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "HotkeyClipboard");
            Directory.CreateDirectory(dir);
            filePath = Path.Combine(dir, "snippets.json");
            Load();
        }

        public void Load()
        {
            try
            {
                if (File.Exists(filePath))
                {
                    var json = File.ReadAllText(filePath);
                    Snippets = JsonSerializer.Deserialize<List<Snippet>>(json) ?? new List<Snippet>();
                }
            }
            catch
            {
                // Corrupt or unreadable file: start fresh rather than crash.
                Snippets = new List<Snippet>();
            }
        }

        public void Save()
        {
            try
            {
                var json = JsonSerializer.Serialize(Snippets, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(filePath, json);
            }
            catch
            {
                // Best-effort persistence; ignore transient IO failures.
            }
        }

        public Snippet? FindByCombo(HotkeyCombo combo) =>
            Snippets.FirstOrDefault(s => s.Combo.Equals(combo));

        public void AddOrUpdate(Snippet snippet)
        {
            var existing = FindByCombo(snippet.Combo);
            if (existing != null) Snippets.Remove(existing);
            Snippets.Add(snippet);
            Save();
        }

        public void Remove(Snippet snippet)
        {
            Snippets.Remove(snippet);
            Save();
        }
    }
}
