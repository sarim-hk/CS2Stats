using Newtonsoft.Json.Linq;

namespace CS2Stats {

    public class SteamAPIClient {
        private readonly string steamAPIKey;

        public SteamAPIClient(string steamAPIKey) {
            this.steamAPIKey = steamAPIKey;
        }

        public async Task<PlayerInfo?> GetSteamSummaryAsync(ulong steamID) {
            try {
                using (HttpClient client = new HttpClient()) {
                    string url = $"http://api.steampowered.com/ISteamUser/GetPlayerSummaries/v0002/?key={steamAPIKey}&steamids={steamID}";

                    HttpResponseMessage response = await client.GetAsync(url);
                    if (response.IsSuccessStatusCode) {
                        string jsonData = await response.Content.ReadAsStringAsync();
                        JObject data = JObject.Parse(jsonData);

                        JToken? playersData = data["response"]?["players"];
                        if (playersData != null) {
                            foreach (JToken playerData in playersData) {
                                string? steamIDStr = playerData["steamid"]?.ToString();
                                if (steamIDStr != null) {
                                    string? personaname = playerData["personaname"]?.ToString();
                                    string? avatar = playerData["avatar"]?.ToString();
                                    string? avatarM = playerData["avatarmedium"]?.ToString();
                                    string? avatarL = playerData["avatarfull"]?.ToString();

                                    PlayerInfo player = new PlayerInfo(steamID, personaname, avatar, avatarM, avatarL);
                                    return player;
                                }
                            }
                        }

                        Console.WriteLine($"Failed to fetch Steam summary: {response.StatusCode}");
                    }
                }
            }
            catch (HttpRequestException e) {
                Console.WriteLine($"Request exception: {e.Message}");
            }

            return null;
        }
    }

    public class PlayerInfo {
        public ulong PlayerID;
        public string? Username;
        public string? AvatarS;
        public string? AvatarM;
        public string? AvatarL;

        public PlayerInfo(ulong playerID, string? username, string? avatarS,  string? avatarM, string? avatarL) {
            this.PlayerID = playerID;
            this.Username = username;
            this.AvatarS = avatarS;
            this.AvatarM = avatarM;
            this.AvatarL = avatarL;
        }

    }

}
