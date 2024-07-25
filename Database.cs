using MySql.Data.MySqlClient;
using Microsoft.Extensions.Logging;
using CounterStrikeSharp.API;

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

        public async Task InsertPlayerStatAsync(ulong? playerID, int kills, int headshots, int assists, int deaths, int damage, int utilityDamage, int roundsPlayed, ILogger Logger) {
            try {
                await using (var conn = new MySqlConnection(this.conn.ConnectionString)) {
                    await conn.OpenAsync();
                    string query = "INSERT INTO PlayerStat (PlayerID, Kills, Headshots, Assists, Deaths, TotalDamage, UtilityDamage, RoundsPlayed)" +
                                   "VALUES (@PlayerID, @Kills, @Headshots, @Assists, @Deaths, @TotalDamage, @UtilityDamage, @RoundsPlayed)";

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
                    }
                }
                Logger.LogInformation("Successfully inserted PlayerStat into database.");
            }
            catch (Exception ex) {
                Logger.LogError(ex, "Error inserting PlayerStat into the database.");
            }
        }

        public async Task InsertPlayerAsync(ulong? playerID, string username, ILogger Logger) {
            try {
                await using (var conn = new MySqlConnection(this.conn.ConnectionString)) {
                    await conn.OpenAsync();
                    string query = "INSERT INTO Player (PlayerID, Username, ELO) VALUES (@PlayerID, @Username, @ELO) " +
                                    "ON DUPLICATE KEY UPDATE Username = @Username";

                    await using (var cmd = new MySqlCommand(query, conn)) {
                        cmd.Parameters.AddWithValue("@PlayerID", playerID);
                        cmd.Parameters.AddWithValue("@Username", username);
                        cmd.Parameters.AddWithValue("@ELO", 25);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
                Logger.LogInformation("Successfully inserted Player into database.");
            }
            catch (Exception ex) {
                Logger.LogError(ex, "Error inserting Player into database.");
            }
        }

    }
}
