//using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace CrestronMasters26.P502.JsonFileOps
{
    // The room settings, loaded from roomConfig.json.
    public class RoomConfig
    {
        // [JsonProperty] (Newtonsoft) links the JSON name (roomName) to the C# name (RoomName).
        // With System.Text.Json the attribute is [JsonPropertyName("roomName")].
        [JsonProperty("roomName")]
        public string RoomName { get; set; } = "New Room";

        [JsonProperty("autoShutdown")]
        public bool AutoShutdown { get; set; } = true;

        [JsonProperty("xPanelIpId")]
        public uint XPanelIpId { get; set; }

        // A JSON list of objects. Starts empty so it is never null.
        [JsonProperty("sources")]
        public List<SourceInfo> Sources { get; set; } = new List<SourceInfo>();

        [JsonProperty("displays")]
        public List<DisplayInfo> Displays { get; set; } = new List<DisplayInfo>();

        // [JsonIgnore] = not saved in the file.
        [JsonIgnore]
        public DateTime LastReadTime { get; set; } = DateTime.Now;
    }

    // One video source (NVX encoder).
    public class SourceInfo
    {
        // Presets use the id. You can rename, but never change the id.
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; } = "";

        // JSON has no hex, so use decimal (0x1A = 26).
        [JsonProperty("nvxIpid")]
        public int NvxIpid { get; set; }
    }

    // One display (NVX decoder).
    public class DisplayInfo
    {
        // Same as SourceInfo: rename freely, never change the id.
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; } = "";

        // Display brand, e.g. "NEC" or "Sharp".
        [JsonProperty("model")]
        public string Model { get; set; } = "";

        // The decoder's IPID.
        [JsonProperty("nvxIpid")]
        public int NvxIpid { get; set; }
    }
}
