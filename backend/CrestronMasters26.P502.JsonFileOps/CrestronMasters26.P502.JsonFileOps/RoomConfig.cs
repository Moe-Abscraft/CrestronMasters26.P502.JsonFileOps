
using Newtonsoft.Json;

namespace CrestronMasters26.P502.JsonFileOps
{
    public class RoomConfig
    {
        // JSON uses  "roomName"  (camelCase).
        // C# uses     RoomName   (PascalCase).
        // The [JsonProperty] attribute is the bridge.
        [JsonProperty("roomName")]
        public string RoomName { get; set; } = "New Room";

        [JsonProperty("autoShutdown")]
        public bool AutoShutdown { get; set; } = true;

        [JsonProperty("xPanelIpId")]
        public uint XPanelIpId { get; set; }

        // A JSON array of OBJECTS maps to a List of a nested class.
        // Initialized to an empty list so it's never null.
        [JsonProperty("sources")]
        public List<SourceInfo> Sources { get; set; } = new List<SourceInfo>();

        [JsonProperty("displays")]
        public List<DisplayInfo> Displays { get; set; } = new List<DisplayInfo>();

        // A JSON array of STRINGS maps to a List<string>.
        [JsonProperty("presets")]
        public List<string> Presets { get; set; } = new List<string>();

        [JsonIgnore]
        public DateTime LastReadTime { get; set; } = DateTime.Now;
    }

    public class SourceInfo
    {
        [JsonProperty("name")]
        public string Name { get; set; } = "";

        // JSON has no hex literal, so these are plain decimal — 0x1A is 26.
        [JsonProperty("nvxIpid")]
        public int NvxIpid { get; set; }
    }

    public class DisplayInfo
    {
        [JsonProperty("name")]
        public string Name { get; set; } = "";

        // Which display this is — "NEC", "Sharp", "Samsung" to load the correct driver.
        [JsonProperty("model")]
        public string Model { get; set; } = "";

        // The decoder's IPID
        [JsonProperty("nvxIpid")]
        public int NvxIpid { get; set; }

        [JsonIgnore]
        public string RoutedSource { get; set; } = "";
    }

}
