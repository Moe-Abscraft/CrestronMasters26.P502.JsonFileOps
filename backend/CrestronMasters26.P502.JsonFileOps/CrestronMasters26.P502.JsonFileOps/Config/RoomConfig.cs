using System.Text.Json.Serialization;

namespace CrestronMasters26.P502.JsonFileOps
{
    public class RoomConfig
    {
        // JSON uses  "roomName"  (camelCase).
        // C# uses     RoomName   (PascalCase).
        // The [JsonPropertyName] attribute is the bridge.
        [JsonPropertyName("roomName")]
        public string RoomName { get; set; } = "New Room";

        [JsonPropertyName("autoShutdown")]
        public bool AutoShutdown { get; set; } = true;

        [JsonPropertyName("xPanelIpId")]
        public uint XPanelIpId { get; set; }

        // A JSON array of OBJECTS maps to a List of a nested class.
        // Initialized to an empty list so it's never null.
        [JsonPropertyName("sources")]
        public List<SourceInfo> Sources { get; set; } = new List<SourceInfo>();

        [JsonPropertyName("displays")]
        public List<DisplayInfo> Displays { get; set; } = new List<DisplayInfo>();

        [JsonIgnore]
        public DateTime LastReadTime { get; set; } = DateTime.Now;
    }

    public class SourceInfo
    {
        // What presets remember. The name is for people and can be edited freely; the id
        // must never change once presets have been saved, or they lose track of it.
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        // JSON has no hex literal, so these are plain decimal — 0x1A is 26.
        [JsonPropertyName("nvxIpid")]
        public int NvxIpid { get; set; }
    }

    public class DisplayInfo
    {
        // Same rule as SourceInfo.Id: rename the display all you like, keep the id.
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        // Which display this is — "NEC", "Sharp", "Samsung" to load the correct driver.
        [JsonPropertyName("model")]
        public string Model { get; set; } = "";

        // The decoder's IPID
        [JsonPropertyName("nvxIpid")]
        public int NvxIpid { get; set; }

        // The id of the source on this display, 0 when cleared.
        [JsonIgnore]
        public int RoutedSourceId { get; set; }
    }
}
