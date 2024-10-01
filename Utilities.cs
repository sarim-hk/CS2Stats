using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API;
using System.Security.Cryptography;
using System.Text;
using MySql.Data.MySqlClient;
using Microsoft.Extensions.Logging;
using Serilog.Core;

namespace CS2Stats {

    public partial class CS2Stats {

        // thanks to switz https://discord.com/channels/1160907911501991946/1160925208203493468/1170817201473855619
        private int? GetCSTeamScore(int teamNum) {
            IEnumerable<CCSTeam> teamManagers = Utilities.FindAllEntitiesByDesignerName<CCSTeam>("cs_team_manager");

            foreach (CCSTeam teamManager in teamManagers) {
                if (teamNum == teamManager.TeamNum) {
                    return teamManager.Score;
                }
            }

            return null;
        }

        // thanks to bober https://discord.com/channels/1160907911501991946/1160925208203493468/1173658546387292160
        private static CCSGameRules GetGameRules() {
            return Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules").First().GameRules!;
        }

        private string GenerateTeamID(List<ulong> teamPlayers, ILogger Logger) {
            string teamID = BitConverter.ToString(
                MD5.Create().ComputeHash(
                    Encoding.UTF8.GetBytes(
                        string.Join("", teamPlayers.OrderBy(id => id))
                    )
                )
            ).Replace("-", "");
            Logger.LogInformation($"[GenerateTeamID] Team: {string.Join(", ", teamPlayers)} are {teamID}");
            return teamID;
        }

        private string? GetTeamIDByTeamNum(int teamNum) {
            if (match != null) {
                foreach (string teamID in match.StartingPlayers.Keys) {
                    TeamInfo teamInfo = match.StartingPlayers[teamID];

                    if (teamInfo.Side == teamNum) {
                        return teamID;
                    }
                }
            }

            return null;
        }

        private void SwapTeamsIfNeeded() {
            if (match != null && match.TeamsNeedSwapping) {
                foreach (string teamID in match.StartingPlayers.Keys) {
                    Logger.LogInformation($"[SwapTeamsIfNeeded] Swapping team for teamID {teamID}.");
                    match.StartingPlayers[teamID].SwapSides();
                }
                match.TeamsNeedSwapping = false;
                Logger.LogInformation("[SwapTeamsIfNeeded] Setting teamsNeedSwapping to false.");
            }
        }

        private (string?, string?, int?, int?, int?) GetMatchWinner() {
            if (this.database == null || this.match == null) {
                return (null, null, null, null, null);
            }

            int? team2Score = GetCSTeamScore((int)CsTeam.Terrorist);
            int? team3Score = GetCSTeamScore((int)CsTeam.CounterTerrorist);

            if (team2Score.HasValue && team3Score.HasValue) {
                if (team2Score != team3Score) {
                    int winningTeamNum = (team2Score > team3Score) ? (int)CsTeam.Terrorist : (int)CsTeam.CounterTerrorist;
                    int losingTeamNum = (winningTeamNum == (int)CsTeam.Terrorist) ? (int)CsTeam.CounterTerrorist : (int)CsTeam.Terrorist;

                    string? winningTeamID = GetTeamIDByTeamNum(winningTeamNum);
                    string? losingTeamID = GetTeamIDByTeamNum(losingTeamNum);

                    int? winningTeamScore = (winningTeamNum == (int)CsTeam.Terrorist) ? team2Score : team3Score;
                    int? losingTeamScore = (losingTeamNum == (int)CsTeam.Terrorist) ? team2Score : team3Score;                 
                    return (winningTeamID, losingTeamID, winningTeamScore, losingTeamScore, winningTeamNum);
                }

                else {
                    Logger.LogInformation("[UpdateMatchWithWinner] Game is a tie - not updating match info");
                }

            }
            return (null, null, null, null, null);

        }

        private LiveData GetLiveMatchData() {
            List<LivePlayer> tPlayers = new List<LivePlayer>();
            List<LivePlayer> ctPlayers = new List<LivePlayer>();

            List<CCSPlayerController> playerControllers = Utilities.GetPlayers();
            foreach (var playerController in playerControllers) {
                if (playerController.ActionTrackingServices != null) {
                    var livePlayer = new LivePlayer(playerController.ActionTrackingServices.MatchStats.Kills,
                        playerController.ActionTrackingServices.MatchStats.Assists,
                        playerController.ActionTrackingServices.MatchStats.Deaths,
                        playerController.ActionTrackingServices.MatchStats.Damage,
                        playerController.Health,
                        playerController.ActionTrackingServices.MatchStats.MoneySaved);

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
            float roundTime = (GetGameRules().RoundStartTime + GetGameRules().RoundTime) - Server.CurrentTime;

            int bombStatus = GetGameRules().BombPlanted switch {
                true => 1,
                false => GetGameRules().BombDefused ? 2 : 0
            };

            LiveData liveData = new LiveData(tPlayers, ctPlayers, tScore, ctScore, roundTime, bombStatus);
            return liveData;

        }

    }

    public partial class Database {

        private async Task InsertOrUpdateTeamAsync(string teamId, ILogger Logger) {
            string query = @"
            INSERT INTO CS2S_Team (TeamID)
            VALUES (@TeamID)
            ";

            using (MySqlCommand cmd = new MySqlCommand(query, this.conn, this.transaction)) {
                cmd.Parameters.AddWithValue("@TeamID", teamId);
                await cmd.ExecuteNonQueryAsync();
                Logger.LogInformation($"[InsertOrUpdateTeamAsync] Team with ID {teamId} inserted successfully.");
            }
        }

        private async Task InsertTeamPlayersAsync(string teamId, IEnumerable<ulong> playerIds, ILogger Logger) {
            string query = @"
            INSERT INTO CS2S_Team_Players (TeamID, PlayerID)
            VALUES (@TeamID, @PlayerID)
            ";

            using (MySqlCommand cmd = new MySqlCommand(query, this.conn, this.transaction)) {
                foreach (ulong playerId in playerIds) {
                    cmd.Parameters.Clear();
                    cmd.Parameters.AddWithValue("@TeamID", teamId);
                    cmd.Parameters.AddWithValue("@PlayerID", playerId);
                    await cmd.ExecuteNonQueryAsync();
                    Logger.LogInformation($"[InsertTeamPlayersAsync] Player {playerId} added to Team {teamId}.");
                }
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

                using (MySqlCommand cmd = new MySqlCommand(query, this.conn, this.transaction)) {
                    cmd.Parameters.AddWithValue("@PlayerID", playerID);

                    int rowsAffected = await cmd.ExecuteNonQueryAsync();

                    if (rowsAffected > 0) {
                        Logger.LogInformation($"[IncrementPlayerKills] Successfully incremented Kills for player {playerID}.");
                    }
                    else {
                        Logger.LogInformation($"[IncrementPlayerKills] No rows were updated. Player {playerID} might not exist.");
                    }
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

                using (MySqlCommand cmd = new MySqlCommand(query, this.conn, this.transaction)) {
                    cmd.Parameters.AddWithValue("@PlayerID", playerID);

                    int rowsAffected = await cmd.ExecuteNonQueryAsync();

                    if (rowsAffected > 0) {
                        Logger.LogInformation($"[IncrementPlayerAssists] Successfully incremented Assists for player {playerID}.");
                    }
                    else {
                        Logger.LogInformation($"[IncrementPlayerAssists] No rows were updated. Player {playerID} might not exist.");
                    }
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

                using (MySqlCommand cmd = new MySqlCommand(query, this.conn, this.transaction)) {
                    cmd.Parameters.AddWithValue("@PlayerID", playerID);

                    int rowsAffected = await cmd.ExecuteNonQueryAsync();

                    if (rowsAffected > 0) {
                        Logger.LogInformation($"[IncrementPlayerDeaths] Successfully incremented Deaths for player {playerID}.");
                    }
                    else {
                        Logger.LogInformation($"[IncrementPlayerDeaths] No rows were updated. Player {playerID} might not exist.");
                    }
                }
            }
            catch (Exception ex) {
                Logger.LogError(ex, $"[IncrementPlayerDeaths] Error occurred while incrementing Deaths for player {playerID}.");
            }
        }

        private async Task IncrementPlayerDamage(ulong playerID, string weapon, int damageAmount, ILogger Logger) {
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
                    SET TotalDamage = TotalDamage + @DamageAmount
                    WHERE PlayerID = @PlayerID;
                    ";
                }

                using (MySqlCommand cmd = new MySqlCommand(query, this.conn, this.transaction)) {
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

                using (MySqlCommand cmd = new MySqlCommand(query, this.conn, this.transaction)) {
                    cmd.Parameters.AddWithValue("@PlayerID", playerID);

                    int rowsAffected = await cmd.ExecuteNonQueryAsync();

                    if (rowsAffected > 0) {
                        Logger.LogInformation($"[IncrementPlayerDamage] Successfully incremented Headshots for player {playerID}.");
                    }
                    else {
                        Logger.LogInformation($"[IncrementPlayerDamage] No rows were updated. Player {playerID} might not exist.");
                    }
                }
            }
            catch (Exception ex) {
                Logger.LogError(ex, $"[IncrementPlayerDamage] Error occurred while incrementing Headshots for player {playerID}.");
            }
        }

    }

}
