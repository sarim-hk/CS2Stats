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

        public async Task<Dictionary<ulong, Player>> GetSteamSummariesAsync(Dictionary<ulong, Player> players) {
            try {
                using (var client = new HttpClient()) {
                    var batchedIds = BatchSteamIds(players.Keys, 100);
                    foreach (var batch in batchedIds) {
                        var idsStr = string.Join(",", batch);
                        var url = $"http://api.steampowered.com/ISteamUser/GetPlayerSummaries/v0002/?key={_steamAPIKey}&steamids={idsStr}";

                        var response = await client.GetAsync(url);
                        if (response.IsSuccessStatusCode) {
                            var jsonData = await response.Content.ReadAsStringAsync();
                            var data = JObject.Parse(jsonData);

                            var playersData = data["response"]?["players"];
                            if (playersData != null) {
                                foreach (var playerData in playersData) {
                                    var steamIdStr = playerData["steamid"]?.ToString();
                                    if (steamIdStr != null && ulong.TryParse(steamIdStr, out ulong steamId) && players.ContainsKey(steamId)) {
                                        var player = players[steamId];

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
                                    }
                                }
                            }

                            else {
                                Console.WriteLine($"Failed to fetch Steam summaries: {response.StatusCode}");
                            }
                        }
                    }
                    return players;
                }
            }
            catch (HttpRequestException e) {
                Console.WriteLine($"Request exception: {e.Message}");
                return new Dictionary<ulong, Player>(); // Return an empty dictionary on error
            }
        }

        private List<List<ulong>> BatchSteamIds(IEnumerable<ulong> steamIds, int batchSize) {
            var batches = new List<List<ulong>>();
            var currentBatch = new List<ulong>();

            foreach (var id in steamIds) {
                if (currentBatch.Count >= batchSize) {
                    batches.Add(new List<ulong>(currentBatch));
                    currentBatch.Clear();
                }
                currentBatch.Add(id);
            }

            if (currentBatch.Count > 0) {
                batches.Add(currentBatch);
            }

            return batches;
        }
    }
}
