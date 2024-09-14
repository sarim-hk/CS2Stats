using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace CS2Stats {
    public class SteamAPIClient {
        public string _steamAPIKey;

        public SteamAPIClient(string steamAPIKey) {
            _steamAPIKey = steamAPIKey;
        }

        public async Task<PlayerInfo?> GetSteamSummaryAsync(ulong steamId) {
            try {
                using (var client = new HttpClient()) {
                    var url = $"http://api.steampowered.com/ISteamUser/GetPlayerSummaries/v0002/?key={_steamAPIKey}&steamids={steamId}";

                    var response = await client.GetAsync(url);
                    if (response.IsSuccessStatusCode) {
                        var jsonData = await response.Content.ReadAsStringAsync();
                        var data = JObject.Parse(jsonData);

                        var playersData = data["response"]?["players"];
                        if (playersData != null) {
                            foreach (var playerData in playersData) {
                                var steamIdStr = playerData["steamid"]?.ToString();
                                if (steamIdStr != null && ulong.TryParse(steamIdStr, out ulong parsedSteamId) && parsedSteamId == steamId) {
                                    var player = new PlayerInfo();

                                    var personaname = playerData["personaname"]?.ToString();
                                    if (!string.IsNullOrEmpty(personaname)) {
                                        player.Username = personaname;
                                    }

                                    var avatar = playerData["avatar"]?.ToString();
                                    if (!string.IsNullOrEmpty(avatar)) {
                                        player.AvatarS = avatar;
                                    }

                                    var avatarM = playerData["avatarmedium"]?.ToString();
                                    if (!string.IsNullOrEmpty(avatarM)) {
                                        player.AvatarM = avatarM;
                                    }

                                    var avatarL = playerData["avatarfull"]?.ToString();
                                    if (!string.IsNullOrEmpty(avatarL)) {
                                        player.AvatarL = avatarL;
                                    }

                                    player.PlayerID = steamId;

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
}
