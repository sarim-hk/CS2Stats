using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
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
            HashSet<LivePlayer> tPlayers = [];
            HashSet<LivePlayer> ctPlayers = [];

            foreach (CCSPlayerController playerController in Utilities.GetPlayers()) {
                if (playerController.ActionTrackingServices != null) {
                    LivePlayer livePlayer = new() {
                        Username = playerController.PlayerName,
                        Kills = playerController.ActionTrackingServices.MatchStats.Kills,
                        Assists = playerController.ActionTrackingServices.MatchStats.Assists,
                        Deaths = playerController.ActionTrackingServices.MatchStats.Deaths,
                        Damage = playerController.ActionTrackingServices.MatchStats.Damage,
                        Health = playerController.Pawn.Value?.Health,
                        MoneySaved = playerController.InGameMoneyServices?.Account
                    };

                    if (playerController.TeamNum == 2) {
                        tPlayers.Add(livePlayer);
                    }
                    else if (playerController.TeamNum == 3) {
                        ctPlayers.Add(livePlayer);
                    }

                }
            }

            int? tScore = GetCSTeamScore(2);
            int? ctScore = GetCSTeamScore(3);

            int bombStatus = GetGameRules().BombPlanted switch {
                true => 1,
                false => GetGameRules().BombDefused ? 2 : 0
            };

            LiveData liveData = new() {
                TPlayers = tPlayers,
                CTPlayers = ctPlayers,
                TScore = tScore,
                CTScore = ctScore,
                BombStatus = bombStatus,
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
        
    }

}
