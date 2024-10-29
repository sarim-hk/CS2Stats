using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using System.Security.Cryptography;
using System.Text;

namespace CS2Stats {

    public partial class CS2Stats {

        private TeamInfo? GetTeamInfoByTeamNum(int? teamNum) {
            if (this.match != null && teamNum != null) {
                foreach (string teamID in this.match.StartingPlayers.Keys) {
                    TeamInfo teamInfo = this.match.StartingPlayers[teamID];

                    if (teamInfo.Side == teamNum) {
                        return teamInfo;
                    }
                }
            }

            return null;
        }

        private static LiveData GetLiveMatchData(Round round) {
            HashSet<LivePlayer> tPlayers = [];
            HashSet<LivePlayer> ctPlayers = [];

            List<CCSPlayerController> playerControllers = Utilities.GetPlayers();
            foreach (CCSPlayerController playerController in playerControllers) {
                if (playerController.ActionTrackingServices != null) {
                    LivePlayer livePlayer = new() {
                        Kills = playerController.ActionTrackingServices.MatchStats.Kills,
                        Assists = playerController.ActionTrackingServices.MatchStats.Assists,
                        Deaths = playerController.ActionTrackingServices.MatchStats.Deaths,
                        Damage = playerController.ActionTrackingServices.MatchStats.Damage,
                        Health = playerController.Health,
                        MoneySaved = playerController.ActionTrackingServices.MatchStats.MoneySaved
                    };

                    if (playerController.TeamNum == 2) {
                        tPlayers.Add(livePlayer);
                    }
                    else {
                        ctPlayers.Add(livePlayer);
                    }

                }
            }

            int? tScore = GetCSTeamScore(2);
            int? ctScore = GetCSTeamScore(3);
            int roundTick = Server.TickCount - round.StartTick;

            int bombStatus = GetGameRules().BombPlanted switch {
                true => 1,
                false => GetGameRules().BombDefused ? 2 : 0
            };

            LiveData liveData = new() {
                TPlayers = tPlayers,
                CTPlayers = ctPlayers,
                TScore = tScore,
                CTScore = ctScore,
                RoundTick = roundTick,
                BombStatus = bombStatus
            };
                
            return liveData;

        }

        private static CCSGameRules GetGameRules() {
            // thanks to bober https://discord.com/channels/1160907911501991946/1160925208203493468/1173658546387292160

            return Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules").First().GameRules!;
        }

        private static string GenerateTeamID(HashSet<ulong> teamPlayerIDs, ILogger Logger) {
            string teamID = BitConverter.ToString(
                MD5.HashData(
                    Encoding.UTF8.GetBytes(
                        string.Join("", teamPlayerIDs.OrderBy(id => id))
                    )
                )
            ).Replace("-", "");
            Logger.LogInformation($"[GenerateTeamID] Team: {string.Join(", ", teamPlayerIDs)} are {teamID}");
            return teamID;
        }

        private static int GetCSTeamScore(int teamNum) {
            // thanks to switz https://discord.com/channels/1160907911501991946/1160925208203493468/1170817201473855619

            IEnumerable<CCSTeam> teamManagers = Utilities.FindAllEntitiesByDesignerName<CCSTeam>("cs_team_manager");

            foreach (CCSTeam teamManager in teamManagers) {
                if (teamNum == teamManager.TeamNum) {
                    return teamManager.Score;
                }
            }

            return 0;
        }

        private void SwapTeamsIfNeeded() {
            if (this.match != null && this.match.TeamsNeedSwapping) {
                foreach (string teamID in this.match.StartingPlayers.Keys) {
                    Logger.LogInformation($"[SwapTeamsIfNeeded] Swapping team for teamID {teamID}.");
                    this.match.StartingPlayers[teamID].SwapSides();
                }
                this.match.TeamsNeedSwapping = false;
                Logger.LogInformation("[SwapTeamsIfNeeded] Setting teamsNeedSwapping to false.");
            }
        }

    }

    public partial class Database {
        
        private async Task InsertTeam(TeamInfo teamInfo, ILogger Logger) {
            string query = @"
            INSERT INTO CS2S_Team (TeamID, Size)
            VALUES (@TeamID, @Size)
            ON DUPLICATE KEY UPDATE
                TeamID = TeamID
            ";

            using MySqlCommand cmd = new(query, this.conn, this.transaction);
            cmd.Parameters.AddWithValue("@TeamID", teamInfo.TeamID);
            cmd.Parameters.AddWithValue("@Size", teamInfo.PlayerIDs.Count);

            await cmd.ExecuteNonQueryAsync();
            Logger.LogInformation($"[InsertOrUpdateTeamAsync] Team with ID {teamInfo.TeamID} inserted successfully.");
        }

        private async Task InsertTeamPlayers(TeamInfo teamInfo, ILogger Logger) {
            string query = @"
            INSERT INTO CS2S_Team_Players (TeamID, PlayerID)
            VALUES (@TeamID, @PlayerID)
            ON DUPLICATE KEY UPDATE
                TeamID = TeamID
            ";

            using MySqlCommand cmd = new(query, this.conn, this.transaction);
            foreach (ulong playerID in teamInfo.PlayerIDs) {
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@TeamID", teamInfo.TeamID);
                cmd.Parameters.AddWithValue("@PlayerID", playerID);

                await cmd.ExecuteNonQueryAsync();
                Logger.LogInformation($"[InsertTeamPlayers] Player {playerID} added to Team {teamInfo.TeamID}.");
            }
        }

        private async Task InsertPlayerMatches(Match match, TeamInfo teamInfo, ILogger Logger) {
            string query = @"
            INSERT INTO CS2S_Player_Matches (PlayerID, MatchID)
            VALUES (@PlayerID, @MatchID)
            ";

            using MySqlCommand cmd = new(query, this.conn, this.transaction);
            foreach (ulong playerID in teamInfo.PlayerIDs) {
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@MatchID", match.MatchID);
                cmd.Parameters.AddWithValue("@PlayerID", playerID);

                await cmd.ExecuteNonQueryAsync();
                Logger.LogInformation($"[InsertPlayerMatches] Match {match.MatchID} added to Player {playerID}.");
            }
        }

        private async Task IncrementPlayerKills(ulong? playerID, ILogger Logger) {
            if (playerID == null) {
                return;
            }

            try {
                string query = @"
                UPDATE CS2S_Player
                SET Kills = Kills + 1
                WHERE PlayerID = @PlayerID;
                ";

                using MySqlCommand cmd = new(query, this.conn, this.transaction);
                cmd.Parameters.AddWithValue("@PlayerID", playerID);

                int rowsAffected = await cmd.ExecuteNonQueryAsync();

                if (rowsAffected > 0) {
                    Logger.LogInformation($"[IncrementPlayerKills] Successfully incremented Kills for player {playerID}.");
                }
                else {
                    Logger.LogInformation($"[IncrementPlayerKills] No rows were updated. Player {playerID} might not exist.");
                }
            }
            catch (Exception ex) {
                Logger.LogError(ex, $"[IncrementPlayerKills] Error occurred while incrementing Kills for player {playerID}.");
            }
        }

        private async Task IncrementPlayerAssists(ulong? playerID, ILogger Logger) {
            if (playerID == null) {
                return;
            }

            try {
                string query = @"
                UPDATE CS2S_Player
                SET Assists = Assists + 1
                WHERE PlayerID = @PlayerID;
                ";

                using MySqlCommand cmd = new(query, this.conn, this.transaction);
                cmd.Parameters.AddWithValue("@PlayerID", playerID);

                int rowsAffected = await cmd.ExecuteNonQueryAsync();

                if (rowsAffected > 0) {
                    Logger.LogInformation($"[IncrementPlayerAssists] Successfully incremented Assists for player {playerID}.");
                }
                else {
                    Logger.LogInformation($"[IncrementPlayerAssists] No rows were updated. Player {playerID} might not exist.");
                }
            }
            catch (Exception ex) {
                Logger.LogError(ex, $"[IncrementPlayerAssists] Error occurred while incrementing Assists for player {playerID}.");
            }
        }

        private async Task IncrementPlayerDeaths(ulong playerID, ILogger Logger) {
            try {
                string query = @"
                UPDATE CS2S_Player
                SET Deaths = Deaths + 1
                WHERE PlayerID = @PlayerID;
                ";

                using MySqlCommand cmd = new(query, this.conn, this.transaction);
                cmd.Parameters.AddWithValue("@PlayerID", playerID);

                int rowsAffected = await cmd.ExecuteNonQueryAsync();

                if (rowsAffected > 0) {
                    Logger.LogInformation($"[IncrementPlayerDeaths] Successfully incremented Deaths for player {playerID}.");
                }
                else {
                    Logger.LogInformation($"[IncrementPlayerDeaths] No rows were updated. Player {playerID} might not exist.");
                }
            }
            catch (Exception ex) {
                Logger.LogError(ex, $"[IncrementPlayerDeaths] Error occurred while incrementing Deaths for player {playerID}.");
            }
        }

        private async Task IncrementPlayerDamage(ulong? playerID, string weapon, int damageAmount, ILogger Logger) {
            try {
                string query;

                if (weapon == "smokegrenade" || weapon == "hegrenade" || weapon == "flashbang" ||
                    weapon == "molotov" || weapon == "inferno" || weapon == "decoy") {
                    query = @"
                    UPDATE CS2S_Player
                    SET UtilityDamage = UtilityDamage + @DamageAmount
                    WHERE PlayerID = @PlayerID;
                    ";
                }
                else {
                    query = @"
                    UPDATE CS2S_Player
                    SET Damage = Damage + @DamageAmount
                    WHERE PlayerID = @PlayerID;
                    ";
                }

                using MySqlCommand cmd = new(query, this.conn, this.transaction);
                cmd.Parameters.AddWithValue("@PlayerID", playerID);
                cmd.Parameters.AddWithValue("@DamageAmount", damageAmount);

                int rowsAffected = await cmd.ExecuteNonQueryAsync();

                if (rowsAffected > 0) {
                    Logger.LogInformation($"[IncrementPlayerDamage] Successfully incremented damage for player {playerID} using {weapon}. Damage: {damageAmount}");
                }
                else {
                    Logger.LogInformation($"[IncrementPlayerDamage] No rows were updated. Player {playerID} might not exist.");
                }
            }
            catch (Exception ex) {
                Logger.LogError(ex, $"[IncrementPlayerDamage] Error occurred while incrementing damage for player {playerID} with weapon {weapon}.");
            }
        }

        private async Task IncrementPlayerHeadshots(ulong? playerID, ILogger Logger) {
            if (playerID == null) {
                return;
            }

            try {
                string query = @"
                UPDATE CS2S_Player
                SET Headshots = Headshots + 1
                WHERE PlayerID = @PlayerID;
                ";

                using MySqlCommand cmd = new(query, this.conn, this.transaction);
                cmd.Parameters.AddWithValue("@PlayerID", playerID);

                int rowsAffected = await cmd.ExecuteNonQueryAsync();

                if (rowsAffected > 0) {
                    Logger.LogInformation($"[IncrementPlayerDamage] Successfully incremented Headshots for player {playerID}.");
                }
                else {
                    Logger.LogInformation($"[IncrementPlayerDamage] No rows were updated. Player {playerID} might not exist.");
                }
            }
            catch (Exception ex) {
                Logger.LogError(ex, $"[IncrementPlayerDamage] Error occurred while incrementing Headshots for player {playerID}.");
            }
        }

        private async Task IncrementPlayerEnemiesFlashed(ulong playerID, ILogger Logger) {
            try {
                string query = @"
                UPDATE CS2S_Player
                SET EnemiesFlashed = EnemiesFlashed + 1
                WHERE PlayerID = @PlayerID;
                ";

                using MySqlCommand cmd = new(query, this.conn, this.transaction);
                cmd.Parameters.AddWithValue("@PlayerID", playerID);

                int rowsAffected = await cmd.ExecuteNonQueryAsync();

                if (rowsAffected > 0) {
                    Logger.LogInformation($"[IncrementPlayerEnemiesFlashed] Successfully incremented enemies flashed for player {playerID}.");
                }
                else {
                    Logger.LogInformation($"[IncrementPlayerEnemiesFlashed] No rows were updated. Player {playerID} might not exist.");
                }
            }
            catch (Exception ex) {
                Logger.LogError(ex, $"[IncrementPlayerEnemiesFlashed] Error occurred while incrementing enemies flashed for player {playerID}.");
            }
        }

    }

}
