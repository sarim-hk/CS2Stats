using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace CS2Stats {

    public partial class CS2Stats {

        private TeamInfo? GetTeamInfoByTeamNum(int? teamNum) {
            if (this.Match != null && teamNum != null) {
                foreach (string teamID in this.Match.StartingPlayers.Keys) {
                    TeamInfo teamInfo = this.Match.StartingPlayers[teamID];

                    if (teamInfo.Side == teamNum) {
                        return teamInfo;
                    }
                }
            }

            return null;
        }

        private static LiveData GetLiveMatchData() {

            List<LivePlayer> players = [];

            foreach (CCSPlayerController playerController in Utilities.GetPlayers()) {

                    if ((playerController.ActionTrackingServices != null) &&
                        (!playerController.IsBot && playerController.IsValid &&
                        (playerController.Team == CsTeam.Terrorist || playerController.Team == CsTeam.CounterTerrorist))) {

                    LivePlayer livePlayer = new() {
                        PlayerID = playerController.SteamID,
                        Kills = playerController.ActionTrackingServices.MatchStats.Kills,
                        Assists = playerController.ActionTrackingServices.MatchStats.Assists,
                        Deaths = playerController.ActionTrackingServices.MatchStats.Deaths,
                        ADR = (GetGameRules().TotalRoundsPlayed != 0) ? (playerController.ActionTrackingServices.MatchStats.Damage / GetGameRules().TotalRoundsPlayed) : 0,
                        Health = playerController.PlayerPawn.Value?.Health ?? 0,
                        Money = playerController.InGameMoneyServices?.Account ?? 0,
                        Side = playerController.TeamNum,
                    };
                    Console.WriteLine(livePlayer.PlayerID.ToString());
                    players.Add(livePlayer);

                }
            }

            LiveStatus status = new() {
                BombStatus = GetGameRules().BombPlanted switch { true => 1, false => GetGameRules().BombDefused ? 2 : 0 },
                Map = Server.MapName,
                TScore = GetCSTeamScore(2),
                CTScore = GetCSTeamScore(3)
            };

            LiveData liveData = new() {
                Players = players,
                Status = status
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

        private static CCSTeam? GetCSTeamByTeamNum(int teamNum) {
            // thanks to switz https://discord.com/channels/1160907911501991946/1160925208203493468/1170817201473855619

            IEnumerable<CCSTeam> teamManagers = Utilities.FindAllEntitiesByDesignerName<CCSTeam>("cs_team_manager");

            foreach (CCSTeam teamManager in teamManagers) {
                if (teamNum == teamManager.TeamNum) {
                    return teamManager;
                }
            }

            return null;
        }

        private void SwapTeamsIfNeeded() {
            if (this.Match != null && this.Match.TeamsNeedSwapping) {
                foreach (string teamID in this.Match.StartingPlayers.Keys) {
                    Logger.LogInformation($"[SwapTeamsIfNeeded] Swapping team for teamID {teamID}.");
                    this.Match.StartingPlayers[teamID].SwapSides();
                }
                this.Match.TeamsNeedSwapping = false;
                Logger.LogInformation("[SwapTeamsIfNeeded] Setting teamsNeedSwapping to false.");
            }
        }

        public void StartDemo(ILogger Logger) {

            string demoDirectoryPath = Path.Combine(Server.GameDirectory, "csgo", "CS2Stats");
            if (!Directory.Exists(demoDirectoryPath)) {
                Directory.CreateDirectory(demoDirectoryPath);
            }

            string demoFileName = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss")+ "_" + Server.MapName + ".dem";
            string demoPath = Path.Combine(demoDirectoryPath, demoFileName);

            this.StopDemo(Logger);

            Server.ExecuteCommand($"tv_record \"{demoPath}\"");
            Logger.LogInformation($"[StartDemo] Started recording demo: {demoPath}");
        }

        public void StopDemo(ILogger Logger) {
            Server.ExecuteCommand("tv_stoprecord");
            Logger.LogInformation("[StopDemo] Stopped recording demo.");
        }

    }

    public partial class Database {
        
        private async Task InsertTeam(TeamInfo teamInfo, ILogger Logger) {
            string query = @"
            INSERT INTO CS2S_Team (TeamID, Size, Name)
            VALUES (@TeamID, @Size, @Name)
            ON DUPLICATE KEY UPDATE
                TeamID = TeamID
            ";

            using MySqlCommand cmd = new(query, this.conn, this.transaction);
            cmd.Parameters.AddWithValue("@TeamID", teamInfo.TeamID);
            cmd.Parameters.AddWithValue("@Size", teamInfo.PlayerIDs.Count);
            cmd.Parameters.AddWithValue("@Name", teamInfo.FirstPlayerName);

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

        private async Task InsertLiveStatus(LiveData liveData, ILogger Logger) {
            try {
                string query = @"
                INSERT INTO CS2S_LiveStatus (StaticID, BombStatus, TScore, CTScore, InsertDate)
                VALUES (1, @BombStatus, @TScore, @CTScore, CURRENT_TIMESTAMP)
                ON DUPLICATE KEY UPDATE 
                    BombStatus = VALUES(BombStatus), 
                    TScore = VALUES(TScore), 
                    CTScore = VALUES(CTScore), 
                    InsertDate = CURRENT_TIMESTAMP
                ";

                MySqlConnection tempConn = new(this.connString);
                await tempConn.OpenAsync();

                using MySqlCommand cmd = new(query, tempConn);
                cmd.Parameters.AddWithValue("@BombStatus", liveData.Status.BombStatus);
                cmd.Parameters.AddWithValue("@TScore", liveData.Status.TScore);
                cmd.Parameters.AddWithValue("@CTScore", liveData.Status.CTScore);

                await cmd.ExecuteNonQueryAsync();
                await tempConn.CloseAsync();

                Logger.LogInformation("[InsertLiveStatus] Live status data inserted successfully.");
            }

            catch (Exception ex) {
                Logger.LogError(ex, "[InsertLiveStatus] Error occurred while inserting live status data.");
            }
        }

        private async Task InsertLivePlayers(LiveData liveData, ILogger Logger) {
            try {
                string query = @"
                INSERT INTO CS2S_LivePlayers (PlayerID, Kills, Assists, Deaths, ADR, Health, Money, Side)
                VALUES (@PlayerID, @Kills, @Assists, @Deaths, @ADR, @Health, @Money, @Side)
                ";

                MySqlConnection tempConn = new(this.connString);
                await tempConn.OpenAsync();

                using MySqlCommand cmd = new(query, tempConn);
                foreach (LivePlayer livePlayer in liveData.Players) {
                    cmd.Parameters.Clear();
                    cmd.Parameters.AddWithValue("@PlayerID", livePlayer.PlayerID);
                    cmd.Parameters.AddWithValue("@Kills", livePlayer.Kills);
                    cmd.Parameters.AddWithValue("@Assists", livePlayer.Assists);
                    cmd.Parameters.AddWithValue("@Deaths", livePlayer.Deaths);
                    cmd.Parameters.AddWithValue("@ADR", livePlayer.ADR);
                    cmd.Parameters.AddWithValue("@Health", livePlayer.Health);
                    cmd.Parameters.AddWithValue("@Money", livePlayer.Money);
                    cmd.Parameters.AddWithValue("@Side", livePlayer.Side);
                    await cmd.ExecuteNonQueryAsync();
                    await tempConn.CloseAsync();
                }

                Logger.LogInformation("[InsertLivePlayers] Live player data inserted successfully.");
            }

            catch (Exception ex) {
                Logger.LogError(ex, "[InsertLivePlayers] Error occurred while inserting live status data.");
            }

        }

    }

}
