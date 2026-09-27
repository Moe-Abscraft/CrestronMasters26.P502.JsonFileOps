using System.Text.Json;

namespace CrestronMasters26.P502.JsonFileOps
{
    // No [JsonPropertyName] here: the CamelCase policy below turns SourceLevel into
    // "sourceLevel" for every property at once.
    public class Preset
    {
        // Key = display id, value = source id (0 = cleared). Ids, not names, so renaming
        // a source or display in roomConfig.json doesn't break a saved preset.
        public Dictionary<int, int> Routes { get; set; } = new Dictionary<int, int>();
        public ushort SourceLevel { get; set; }
        public bool SourceMuted { get; set; }
        public ushort MicLevel { get; set; }
        public bool MicMuted { get; set; }
    }

    // preset.json is written by the program, never by hand, so it lives apart from
    // roomConfig.json. It doesn't exist until the first preset is saved.
    // Key = preset button number (1-5).
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
