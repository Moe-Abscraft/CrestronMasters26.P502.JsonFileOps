using System.Text.Json;

namespace CrestronMasters26.P502.JsonFileOps
{
    // One saved preset: routes and audio.
    // No [JsonPropertyName] needed: CamelCase policy below names them all.
    public class Preset
    {
        // Display id -> source id (0 = cleared).
        public Dictionary<int, int> Routes { get; set; } = new Dictionary<int, int>();
        public ushort SourceLevel { get; set; }
        public bool SourceMuted { get; set; }
        public ushort MicLevel { get; set; }
        public bool MicMuted { get; set; }
    }

    // Reads and writes preset.json. Key = preset button (1-5).
    // The file is created on the first save.
    internal class PresetStore
    {
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        private readonly string _filePath;

        public PresetStore(string filePath)
        {
            _filePath = filePath;
        }

        public Dictionary<int, Preset> Load()
        {
            if (!File.Exists(_filePath))
                return new Dictionary<int, Preset>();

            return JsonSerializer.Deserialize<Dictionary<int, Preset>>(File.ReadAllText(_filePath), JsonOptions)
                ?? new Dictionary<int, Preset>();
        }

        public void Save(Dictionary<int, Preset> presets)
        {
            File.WriteAllText(_filePath, JsonSerializer.Serialize(presets, JsonOptions));
        }
    }
}
