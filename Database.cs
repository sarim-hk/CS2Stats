using MySql.Data.MySqlClient;
using Microsoft.Extensions.Logging;

namespace CS2Stats {
    public class Database {

        public MySqlConnection conn;

        public Database(string server, string db, string username, string password) {
            string constring = $"SERVER={server};" +
                               $"DATABASE={db};" +
                               $"UID={username};" +
                               $"PASSWORD={password};";
            Console.WriteLine(constring);

            MySqlConnection conn = new MySqlConnection(constring);
            this.conn = conn;
        }

        public async Task<int?> InsertTeamAsync(ILogger Logger) {
            int teamID;

            try {
                await using (var conn = new MySqlConnection(this.conn.ConnectionString)) {
                    await conn.OpenAsync();
                    string query = @"
                                   INSERT INTO `Team` ()
                                   VALUES ()";

                    await using (var cmd = new MySqlCommand(query, conn)) {
                        await cmd.ExecuteNonQueryAsync();
                        teamID = (int)cmd.LastInsertedId;
                    }
                }
                Logger.LogInformation($"Successfully inserted Team {teamID} into database.");
                return teamID;
            }

            catch (Exception ex) {
                Logger.LogError(ex, "Error inserting Team into database.");
                return null;
            }
        }

        public async Task InsertPlayerAsync(ulong? playerID, string username, ILogger Logger) {
            try {
                await using (var conn = new MySqlConnection(this.conn.ConnectionString)) {
                    await conn.OpenAsync();
                    string query = @"
                                   INSERT INTO `Player` (PlayerID, Username) VALUES (@PlayerID, @Username)
                                   ON DUPLICATE KEY UPDATE Username = @Username";

                    await using (var cmd = new MySqlCommand(query, conn)) {
                        cmd.Parameters.AddWithValue("@PlayerID", playerID);
                        cmd.Parameters.AddWithValue("@Username", username);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
                Logger.LogInformation($"Successfully inserted Player {playerID} {username} into database.");
            }
            catch (Exception ex) {
                Logger.LogError(ex, "Error inserting Player into database.");
            }
        }

        public async Task InsertTeamPlayerAsync(int? teamID, ulong playerID, ILogger Logger) {
            try {
                await using (var conn = new MySqlConnection(this.conn.ConnectionString)) {
                    await conn.OpenAsync();
                    string query = @"
                                   INSERT INTO `TeamPlayer` (TeamID, PlayerID)
                                   VALUES (@TeamID, @PlayerID)";

                    await using (var cmd = new MySqlCommand(query, conn)) {
                        cmd.Parameters.AddWithValue("@TeamID", teamID);
                        cmd.Parameters.AddWithValue("@PlayerID", playerID);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
                Logger.LogInformation($"Successfully inserted TeamPlayer {teamID} {playerID} into database.");
            }
            catch (Exception ex) {
                Logger.LogError(ex, "Error inserting TeamPlayer into database.");
            }
        }

        public async Task<int?> InsertPlayerStatAsync(ulong? playerID, int kills, int headshots, int assists, int deaths, int damage, int utilityDamage, int roundsPlayed, ILogger Logger) {
            int playerStatID;

            try {
                await using (var conn = new MySqlConnection(this.conn.ConnectionString)) {
                    await conn.OpenAsync();
                    string query = @"
                                   INSERT INTO `PlayerStat` (PlayerID, Kills, Headshots, Assists, Deaths, TotalDamage, UtilityDamage, RoundsPlayed)
                                   VALUES (@PlayerID, @Kills, @Headshots, @Assists, @Deaths, @TotalDamage, @UtilityDamage, @RoundsPlayed)";

                    await using (var cmd = new MySqlCommand(query, conn)) {
                        cmd.Parameters.AddWithValue("@PlayerID", playerID);
                        cmd.Parameters.AddWithValue("@Kills", kills);
                        cmd.Parameters.AddWithValue("@Headshots", headshots);
                        cmd.Parameters.AddWithValue("@Assists", assists);
                        cmd.Parameters.AddWithValue("@Deaths", deaths);
                        cmd.Parameters.AddWithValue("@TotalDamage", damage);
                        cmd.Parameters.AddWithValue("@UtilityDamage", utilityDamage);
                        cmd.Parameters.AddWithValue("@RoundsPlayed", roundsPlayed);
                        await cmd.ExecuteNonQueryAsync();
                        playerStatID = (int)cmd.LastInsertedId;
                    }
                }
                Logger.LogInformation($"Successfully inserted PlayerStat {playerID} into database.");
                return playerStatID;
            }
            catch (Exception ex) {
                Logger.LogError(ex, "Error inserting PlayerStat into the database.");
                return null;
            }
        }

        public async Task<int?> InsertMatchAsync(int? teamTID, int? teamCTID, int? teamTScore, int? teamCTScore, ILogger Logger) {
            int matchID;

            try {
                await using (var conn = new MySqlConnection(this.conn.ConnectionString)) {
                    await conn.OpenAsync();
                    string query = @"
                                   INSERT INTO `Match` (TeamTID, TeamCTID, TeamTScore, TeamCTScore)
                                   VALUES (@TeamTID, @TeamCTID, @TeamTScore, @TeamCTScore)";

                    await using (var cmd = new MySqlCommand(query, conn)) {
                        cmd.Parameters.AddWithValue("@TeamTID", teamTID);
                        cmd.Parameters.AddWithValue("@TeamCTID", teamCTID);
                        cmd.Parameters.AddWithValue("@TeamTScore", teamTScore);
                        cmd.Parameters.AddWithValue("@TeamCTScore", teamCTScore);
                        await cmd.ExecuteNonQueryAsync();
                        matchID = (int)cmd.LastInsertedId;

                    }
                }
                Logger.LogInformation("Successfully inserted Match into database.");
                return matchID;
            }
            catch (Exception ex) {
                Logger.LogError(ex, "Error inserting Match into the database.");
                return null;
            }
        }

        public async Task InsertMatch_PlayerStatAsync(int? matchID, int? playerStatID, ILogger Logger) {
            try {
                await using (var conn = new MySqlConnection(this.conn.ConnectionString)) {
                    await conn.OpenAsync();
                    string query = @"
                                   INSERT INTO `Match_PlayerStat` (MatchID, PlayerStatID)
                                   VALUES (@MatchID, @PlayerStatID)";

                    await using (var cmd = new MySqlCommand(query, conn)) {
                        cmd.Parameters.AddWithValue("@MatchID", matchID);
                        cmd.Parameters.AddWithValue("@PlayerStatID", playerStatID);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
                Logger.LogInformation($"Successfully inserted Match_PlayerStat {matchID} {playerStatID} into database.");
            }
            catch (Exception ex) {
                Logger.LogError(ex, "Error inserting Match_PlayerStat into the database.");
            }
        }

        public async Task InsertPlayer_MatchAsync(ulong playerID, int? matchID, ILogger Logger) {
            try {
                await using (var conn = new MySqlConnection(this.conn.ConnectionString)) {
                    await conn.OpenAsync();
                    string query = @"
                                   INSERT INTO `Player_Match` (PlayerID, MatchID)
                                   VALUES (@PlayerID, @MatchID)";

                    await using (var cmd = new MySqlCommand(query, conn)) {
                        cmd.Parameters.AddWithValue("@PlayerID", playerID);
                        cmd.Parameters.AddWithValue("@MatchID", matchID);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
                Logger.LogInformation($"Successfully inserted Player_Match {playerID} {matchID} into database.");
            }
            catch (Exception ex) {
                Logger.LogError(ex, "Error inserting Player_Match into the database.");
            }
        }

        public async Task InsertPlayer_PlayerStatTaskAsync(ulong playerID, int? playerStatID, ILogger Logger) {
            try {
                await using (var conn = new MySqlConnection(this.conn.ConnectionString)) {
                    await conn.OpenAsync();
                    string query = @"
                                   INSERT INTO `Player_PlayerStat` (PlayerID, PlayerStatID)
                                   VALUES (@PlayerID, @PlayerStatID)";

                    await using (var cmd = new MySqlCommand(query, conn)) {
                        cmd.Parameters.AddWithValue("@PlayerID", playerID);
                        cmd.Parameters.AddWithValue("@PlayerStatID", playerStatID);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
                Logger.LogInformation($"Successfully inserted Player_PlayerStat {playerID} {playerStatID} into database.");
            }
            catch (Exception ex) {
                Logger.LogError(ex, "Error inserting Player_PlayerStat into the database.");
            }
        }

        public async Task<int?> GetPlayerELOFromTeamIDAsync(int? teamID, ILogger Logger) {
            int count = 0, averageELO = 0;

            try {
                await using (var conn = new MySqlConnection(this.conn.ConnectionString)) {
                    await conn.OpenAsync();
                    string query = @"
                                   SELECT p.PlayerID, p.ELO
                                   FROM TeamPlayer tp
                                   JOIN Player p ON tp.PlayerID = p.PlayerID
                                   WHERE tp.TeamID = @TeamID";

                    await using (var cmd = new MySqlCommand(query, conn)) {
                        cmd.Parameters.AddWithValue("@TeamID", teamID);
                        await using (var reader = await cmd.ExecuteReaderAsync()) {
                            while (await reader.ReadAsync()) {
                                ulong playerID = await reader.GetFieldValueAsync<ulong>(0);
                                int elo = await reader.GetFieldValueAsync<int>(1);
                                averageELO += elo;
                                count += 1;

                                Logger.LogInformation($"Player ID: {playerID} ELO: {elo}");
                            }
                        }
                    }
                }
                Logger.LogInformation("Successfully retrieved ELOs for players in the team.");

                if (count == 0) {
                    Logger.LogError("No players found in the team.");
                    return null;
                }

                return averageELO / count;
            }
            catch (Exception ex) {
                Logger.LogError(ex, "Error retrieving ELOs for players in the team.");
                return null;
            }
        }

        public async Task UpdatePlayerELOFromTeamIDAsync(int? teamID, int deltaELO, bool winner, ILogger Logger) {
            try {
                await using (var conn = new MySqlConnection(this.conn.ConnectionString)) {
                    await conn.OpenAsync();

                    string query;
                    if (winner == true) {
                        query = @"UPDATE Player p
                                JOIN TeamPlayer tp ON p.PlayerID = tp.PlayerID
                                SET p.ELO = p.ELO + @DeltaELO
                                WHERE tp.TeamID = @TeamID";
                    } else {
                        query = @"UPDATE Player p
                                JOIN TeamPlayer tp ON p.PlayerID = tp.PlayerID
                                SET p.ELO = p.ELO - @DeltaELO
                                WHERE tp.TeamID = @TeamID";

                    }

                    await using (var cmd = new MySqlCommand(query, conn)) {
                        cmd.Parameters.AddWithValue("@DeltaELO", deltaELO);
                        cmd.Parameters.AddWithValue("@TeamID", teamID);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
                Logger.LogInformation("Successfully updated ELOs for players in the team.");
            }
            catch (Exception ex) {
                Logger.LogError(ex, "Error updating ELOs for players in the team.");
            }
        }



    }

}
