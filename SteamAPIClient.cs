using Newtonsoft.Json.Linq;
using System.IO;

namespace CS2Stats {

    public class SteamAPIClient {
        private readonly string steamAPIKey;

        public SteamAPIClient(string steamAPIKey) {
            this.steamAPIKey = steamAPIKey;
        }

        public async Task<PlayerInfo?> GetSteamSummaryAsync(ulong steamID) {
            try {
                using HttpClient client = new();
                string url = $"http://api.steampowered.com/ISteamUser/GetPlayerSummaries/v0002/?key={steamAPIKey}&steamids={steamID}";

                HttpResponseMessage response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode) {
                    string jsonData = await response.Content.ReadAsStringAsync();
                    JObject data = JObject.Parse(jsonData);

                    JToken? playersData = data["response"]?["players"];
                    if (playersData != null) {
                        foreach (JToken playerData in playersData) {
                            string? personaname = playerData["personaname"]?.ToString();
                            string? avatarUrl = playerData["avatar"]?.ToString();

                            string? avatarHash = null;
                            if (!string.IsNullOrEmpty(avatarUrl)) {
                                avatarHash = Path.GetFileNameWithoutExtension(avatarUrl);
                            }

                            PlayerInfo player = new() {
                                PlayerID = steamID,
                                Username = personaname,
                                AvatarHash = avatarHash,
                            };

                            return player;
                        }
                    }

                    Console.WriteLine($"Failed to fetch Steam summary: {response.StatusCode}");
                }
            }
            catch (HttpRequestException e) {
                Console.WriteLine($"Request exception: {e.Message}");
            }

            return null;
        }
    }

    public struct PlayerInfo {
        public ulong PlayerID;
        public string? Username;
        public string? AvatarHash;
    }

}
