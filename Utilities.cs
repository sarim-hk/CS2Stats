using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API;
using System.Security.Cryptography;
using System.Text;
using MySql.Data.MySqlClient;
using Microsoft.Extensions.Logging;

namespace CS2Stats {
    public partial class CS2Stats {

        // thanks to switz https://discord.com/channels/1160907911501991946/1160925208203493468/1170817201473855619
        private int? GetCSTeamScore(int teamNum) {
            var teamManagers = Utilities.FindAllEntitiesByDesignerName<CCSTeam>("cs_team_manager");

            foreach (var teamManager in teamManagers) {
                if (teamNum == teamManager.TeamNum) {
                    return teamManager.Score;
                }
            }

            return null;
        }

        // thanks to bober https://discord.com/channels/1160907911501991946/1160925208203493468/1173658546387292160
        public static CCSGameRules GetGameRules() {
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
            Logger.LogInformation($"Team: {string.Join(", ", teamPlayers)} are {teamID}");
            return teamID;
        }

        private string? GetTeamIDByTeamNum(int teamNum) {
            if (startingPlayers != null) {
                foreach (var teamInfoKVP in startingPlayers) {
                    string teamID = teamInfoKVP.Key;
                    TeamInfo teamInfo = teamInfoKVP.Value;

                    if (teamInfo.Side == teamNum) {
                        return teamID;
                    }
                }
            }

            return null;
        }

    }


    public partial class Database {

        private async Task InsertOrUpdateTeamAsync(string teamId, ILogger Logger) {
            string query = @"
            INSERT INTO CS2S_Team (TeamID)
            VALUES (@TeamID)
            ";

            using (var cmd = new MySqlCommand(query, this.conn, this.transaction)) {
                cmd.Parameters.AddWithValue("@TeamID", teamId);
                await cmd.ExecuteNonQueryAsync();
                Logger.LogInformation($"Team with ID {teamId} inserted successfully.");
            }
        }

        private async Task InsertTeamPlayersAsync(string teamId, IEnumerable<ulong> playerIds, ILogger Logger) {
            string query = @"
            INSERT INTO CS2S_Team_Players (TeamID, PlayerID)
            VALUES (@TeamID, @PlayerID)
            ";

            using (var cmd = new MySqlCommand(query, this.conn, this.transaction)) {
                foreach (var playerId in playerIds) {
                    cmd.Parameters.Clear();
                    cmd.Parameters.AddWithValue("@TeamID", teamId);
                    cmd.Parameters.AddWithValue("@PlayerID", playerId);
                    await cmd.ExecuteNonQueryAsync();
                    Logger.LogInformation($"Player {playerId} added to Team {teamId}.");
                }
            }


        }

    }
}
