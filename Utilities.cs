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
        private int? GetCSTeamScore(CsTeam team) {
            var teamManagers = Utilities.FindAllEntitiesByDesignerName<CCSTeam>("cs_team_manager");

            foreach (var teamManager in teamManagers) {
                if ((int)team == teamManager.TeamNum) {
                    return teamManager.Score;
                }
            }

            return null;
        }

    }

    public partial class Database {

        private string GenerateTeamID(Dictionary<ulong, Player> teamPlayers, ILogger Logger) {
            string teamID = BitConverter.ToString(
                MD5.Create().ComputeHash(
                    Encoding.UTF8.GetBytes(
                        string.Join("", teamPlayers.Keys.OrderBy(id => id))
                    )
                )
            ).Replace("-", "");
            Logger.LogInformation($"Team: {string.Join(", ", teamPlayers.Keys)} are {teamID}");
            return teamID;
        }

        private async Task InsertOrUpdateTeamAsync(string teamId, ILogger Logger) {
            string query = @"
            INSERT INTO CS2S_Team (TeamID)
            VALUES (@TeamID)
            ON DUPLICATE KEY UPDATE 
            TeamID = VALUES(TeamID);
            ";

            using (var cmd = new MySqlCommand(query, this.conn, this.transaction)) {
                cmd.Parameters.AddWithValue("@TeamID", teamId);
                await cmd.ExecuteNonQueryAsync();
                Logger.LogInformation($"Team with ID {teamId} inserted/updated successfully.");
            }
        }

        private async Task InsertTeamPlayersAsync(string teamId, IEnumerable<ulong> playerIds, ILogger Logger) {
            string query = @"
            INSERT INTO CS2S_Team_Players (TeamID, PlayerID)
            VALUES (@TeamID, @PlayerID)
            ON DUPLICATE KEY UPDATE 
            PlayerID = VALUES(PlayerID);
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
