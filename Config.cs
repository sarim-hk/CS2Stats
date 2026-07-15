using CounterStrikeSharp.API.Core;
using System.Text.Json.Serialization;

namespace CS2Stats {

    public class Config : BasePluginConfig {
        [JsonPropertyName("APIBaseURL")] public string APIBaseURL { get; set; } = "";
        [JsonPropertyName("APIAuthKey")] public string APIAuthKey { get; set; } = "";
    }

}
