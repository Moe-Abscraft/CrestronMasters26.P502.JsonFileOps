//using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace CrestronMasters26.P502.JsonFileOps
{
    #region Read me first: what is JSON?
    // JSON stands for JavaScript Object Notation. 
    //
    // The whole idea is key and value. A name, a colon, and the value.
    //
    //     {
    //       "roomName": "Boardroom A",
    //       "autoShutdown": true,
    //       "xPanelIpId": 48,
    //       "sources": [
    //         { "id": 1, "name": "Laptop HDMI", "nvxIpid": 3 }
    //       ]
    //     }
    #endregion

    #region Read me: the four symbols and the six kinds of value
    // Four symbols:
    //   { }   curly braces hold an object
    //   " "   double quotes wrap every key and every text value
    //   :     a colon separates the key from the value
    //   ,     a comma separates one pair from the next
    //
    // Six kinds of value:
    //   string     "Boardroom A"
    //   number     48
    //   boolean    true or false
    //   array      [ ... ]   a list, in square brackets
    //   object     { ... }   in curly braces
    //   null       no value
    #endregion

    #region Read me: the rules that catch everyone
    // Double quotes only. Single quotes are not allowed.
    // true and false are lowercase.
    // No trailing comma after the last item.
    // No comments.
    // No hex. XPanel IPID 30 hex is written as 48.
    #endregion

    #region Read me: how JSON maps to C#
    // Four rules, one to one:
    //   JSON object                ->  class
    //   key                        ->  property
    //   array                      ->  List<T>
    //   object inside an object    ->  nested class   (SourceInfo, DisplayInfo)
    //
    // The fast way to build the classes:
    //   Copy the JSON. In Visual Studio: Edit > Paste Special > Paste JSON As Classes.
    //
    // JSON uses camelCase (roomName). C# uses PascalCase (RoomName).
    // The attribute links the two: JSON name in the attribute, C# name on the property.
    //   Newtonsoft.Json:    [JsonProperty("roomName")]
    //   System.Text.Json:   [JsonPropertyName("roomName")]
    //
    // Two words you'll see in ConfigManager.cs:
    //   Deserialize = JSON text -> object   (reading in)
    //   Serialize   = object -> JSON text   (writing out)
    #endregion
    // The room settings, loaded from roomConfig.json.
    public class RoomConfig
    {
        // [JsonProperty] (Newtonsoft) links the JSON name (roomName) to the C# name (RoomName).
        // With System.Text.Json the attribute is [JsonPropertyName("roomName")].
        [JsonProperty("roomName")]
        public string RoomName { get; set; } = "New Room";

        [JsonProperty("autoShutdown")]
        public bool AutoShutdown { get; set; } = true;

        // 48 in the file is 30 hex. JSON has no hex, so the file uses decimal.
        [JsonProperty("xPanelIpId")]
        public uint XPanelIpId { get; set; }

        // A JSON list of objects. Starts empty so it is never null.
        [JsonProperty("sources")]
        public List<SourceInfo> Sources { get; set; } = new List<SourceInfo>();

        [JsonProperty("displays")]
        public List<DisplayInfo> Displays { get; set; } = new List<DisplayInfo>();

        // [JsonIgnore] = not saved in the file
        [JsonIgnore]
        public DateTime LastReadTime { get; set; } = DateTime.Now;
    }

    // Video source (NVX encoder)
    public class SourceInfo
    {
        // Presets use the id.
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; } = "";

        // JSON has no hex, so use decimal (0x1A = 26).
        [JsonProperty("nvxIpid")]
        public int NvxIpid { get; set; }
    }

    // Display (NVX decoder)
    public class DisplayInfo
    {
        // Same as SourceInfo
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; } = "";

        // Display brand, e.g. "NEC" or "Sharp"
        [JsonProperty("model")]
        public string Model { get; set; } = "";

        // The decoder's IPID.
        [JsonProperty("nvxIpid")]
        public int NvxIpid { get; set; }
    }
}
