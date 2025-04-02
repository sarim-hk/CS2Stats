using CounterStrikeSharp.API.Core;
using System.Text.Json.Serialization;

namespace CS2Stats {

    public class Config : BasePluginConfig {
        [JsonPropertyName("MySQLServer")] public string MySQLServer { get; set; } = "";
        [JsonPropertyName("MySQLDatabase")] public string MySQLDatabase { get; set; } = "";
        [JsonPropertyName("MySQLUsername")] public string MySQLUsername { get; set; } = "";
        [JsonPropertyName("MySQLPassword")] public string MySQLPassword { get; set; } = "";
        [JsonPropertyName("SteamAPIKey")] public string SteamAPIKey { get; set; } = "";
        [JsonPropertyName("DemoRecordingEnabled")] public string DemoRecordingEnabled { get; set; } = "1";
    }

}
