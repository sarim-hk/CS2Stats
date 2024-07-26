using CounterStrikeSharp.API.Core;
using System.Text.Json.Serialization;

namespace CS2Stats {

    public class MySQLConfig : BasePluginConfig {
        [JsonPropertyName("MySQLServer")] public string MySQLServer { get; set; } = "";
        [JsonPropertyName("MySQLDatabase")] public string MySQLDatabase { get; set; } = "";
        [JsonPropertyName("MySQLUsername")] public string MySQLUsername { get; set; } = "";
        [JsonPropertyName("MySQLPassword")] public string MySQLPassword { get; set; } = "";
    }

}
