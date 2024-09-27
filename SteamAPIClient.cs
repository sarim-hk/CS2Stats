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
                using (HttpClient client = new HttpClient()) {
                    string url = $"http://api.steampowered.com/ISteamUser/GetPlayerSummaries/v0002/?key={_steamAPIKey}&steamids={steamId}";

                    HttpResponseMessage response = await client.GetAsync(url);
                    if (response.IsSuccessStatusCode) {
                        string jsonData = await response.Content.ReadAsStringAsync();
                        JObject data = JObject.Parse(jsonData);

                        JToken? playersData = data["response"]?["players"];
                        if (playersData != null) {
                            foreach (JToken playerData in playersData) {
                                string? steamIdStr = playerData["steamid"]?.ToString();
                                if (steamIdStr != null && ulong.TryParse(steamIdStr, out ulong parsedSteamId) && parsedSteamId == steamId) {
                                    PlayerInfo player = new PlayerInfo();

                                    string? personaname = playerData["personaname"]?.ToString();
                                    if (!string.IsNullOrEmpty(personaname)) {
                                        player.Username = personaname;
                                    }

                                    string? avatar = playerData["avatar"]?.ToString();
                                    if (!string.IsNullOrEmpty(avatar)) {
                                        player.AvatarS = avatar;
                                    }

                                    string? avatarM = playerData["avatarmedium"]?.ToString();
                                    if (!string.IsNullOrEmpty(avatarM)) {
                                        player.AvatarM = avatarM;
                                    }

                                    string? avatarL = playerData["avatarfull"]?.ToString();
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
