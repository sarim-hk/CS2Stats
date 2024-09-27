using MySql.Data.MySqlClient;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;
using Serilog.Core;
using CounterStrikeSharp.API.Modules.Entities;
using System.Numerics;
using Org.BouncyCastle.Security;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using Newtonsoft.Json;

namespace CS2Stats {
    public partial class Database {

        public MySqlConnection? conn;
        public MySqlTransaction? transaction;

        public Database(string server, string db, string username, string password) {
            string constring = $"SERVER={server};" +
                               $"DATABASE={db};" +
                               $"UID={username};" +
                               $"PASSWORD={password};";

            MySqlConnection conn = new MySqlConnection(constring);
            conn.OpenAsync();
            this.conn = conn;

        }

        public void StartTransaction() {
            if (this.conn == null) {
                return;
            }

            if (this.transaction != null) {
                this.transaction.Rollback();
                this.transaction.Dispose();
                this.transaction = null;
            }

            this.transaction = this.conn.BeginTransaction();
        }

        public void CommitTransaction() {
            if (this.transaction != null) {
                this.transaction.Commit();
                this.transaction.Dispose();
                this.transaction = null;
            }
        }

        public async Task InsertPlayer(PlayerInfo player, ILogger Logger) {
            try {
                string query = @"
                INSERT INTO CS2S_Player (PlayerID, Username, AvatarS, AvatarM, AvatarL)
                VALUES (@PlayerID, @Username, @AvatarS, @AvatarM, @AvatarL)
                ON DUPLICATE KEY UPDATE 
                Username = VALUES(Username),
                AvatarS = VALUES(AvatarS),
                AvatarM = VALUES(AvatarM),
                AvatarL = VALUES(AvatarL);
                ";

                using (MySqlCommand cmd = new MySqlCommand(query, this.conn, this.transaction)) {
                    cmd.Parameters.AddWithValue("@PlayerID", player.PlayerID);
                    cmd.Parameters.AddWithValue("@Username", player.Username);
                    cmd.Parameters.AddWithValue("@AvatarS", player.AvatarS);
                    cmd.Parameters.AddWithValue("@AvatarM", player.AvatarM);
                    cmd.Parameters.AddWithValue("@AvatarL", player.AvatarL);

                    await cmd.ExecuteNonQueryAsync();
                    Logger.LogInformation($"Player {player.Username} inserted successfully.");
                }
            }

            catch (Exception ex) {
                Logger.LogInformation(ex, "Error occurred while inserting players.");
            }
        }

        public async Task InsertTeamsAndTeamPlayers(Dictionary<string, TeamInfo> startingPlayers, ILogger Logger) {
            try {
                foreach (string teamID in startingPlayers.Keys) {
                    await InsertOrUpdateTeamAsync(teamID, Logger);
                    await InsertTeamPlayersAsync(teamID, startingPlayers[teamID].PlayerIDs, Logger);
                } 

            }
            catch (Exception ex) {
                Logger.LogInformation(ex, "Error occurred while inserting teams and team players.");
                return;
            }
        }

        public async Task InsertMap(string mapName, ILogger Logger) {
            try {
                string query = @"
                INSERT INTO CS2S_Map (MapID)
                VALUES (@MapID)
                ";

                using (MySqlCommand cmd = new MySqlCommand(query, this.conn, this.transaction)) {
                    cmd.Parameters.AddWithValue("@MapID", mapName);
                    await cmd.ExecuteNonQueryAsync();

                    Logger.LogInformation($"Map {mapName} inserted successfully.");
                }
            }

            catch (Exception ex) {
                Logger.LogInformation(ex, "Error occurred while inserting the map.");
            }
        }

        public async Task InsertBatchedHurtEvents(List<HurtEvent> hurtEvents, ILogger Logger) {
            if (hurtEvents == null || hurtEvents.Count == 0) {
                Logger.LogInformation("Hurt events are null.");
                return;
            }

            try {
                string query = @"
                INSERT INTO CS2S_Hurt (AttackerID, VictimID, DamageAmount, Weapon, Hitgroup)
                VALUES (@AttackerID, @VictimID, @DamageAmount, @Weapon, @Hitgroup);
                ";

                using (MySqlCommand cmd = new MySqlCommand(query, this.conn, this.transaction)) {
                    foreach (HurtEvent hurtEvent in hurtEvents) {
                        cmd.Parameters.Clear();

                        cmd.Parameters.AddWithValue("@AttackerID", hurtEvent.AttackerID);
                        cmd.Parameters.AddWithValue("@VictimID", hurtEvent.VictimID);
                        cmd.Parameters.AddWithValue("@DamageAmount", hurtEvent.DamageAmount);
                        cmd.Parameters.AddWithValue("@Weapon", hurtEvent.Weapon);
                        cmd.Parameters.AddWithValue("@Hitgroup", hurtEvent.Hitgroup);
                        await cmd.ExecuteNonQueryAsync();
                        await IncrementPlayerDamage(hurtEvent.AttackerID, hurtEvent.Weapon, hurtEvent.DamageAmount, Logger);

                    }

                    Logger.LogInformation($"Batch of hurt events inserted successfully.");
                }
            }
            catch (Exception ex) {
                Logger.LogInformation(ex, "Error occurred while inserting batch of hurt events.");
            }

        }

        public async Task InsertBatchedDeathEvents(List<DeathEvent> deathEvents, ILogger Logger) {
            if (deathEvents == null || deathEvents.Count == 0) {
                Logger.LogInformation("Hurt events are null.");
                return;
            }

            try {
                string query = @"
                INSERT INTO CS2S_Death (RoundID, AttackerID, AssisterID, VictimID, Weapon, Hitgroup)
                VALUES (@RoundID, @AttackerID, @AssisterID, @VictimID, @Weapon, @Hitgroup);
                ";

                using (MySqlCommand cmd = new MySqlCommand(query, this.conn, this.transaction)) {
                    foreach (DeathEvent deathEvent in deathEvents) {
                        cmd.Parameters.Clear();

                        cmd.Parameters.AddWithValue("@RoundID", deathEvent.RoundID);
                        cmd.Parameters.AddWithValue("@AttackerID", deathEvent.AttackerID);
                        cmd.Parameters.AddWithValue("@AssisterID", deathEvent.AssisterID);
                        cmd.Parameters.AddWithValue("@VictimID", deathEvent.VictimID);
                        cmd.Parameters.AddWithValue("@Weapon", deathEvent.Weapon);
                        cmd.Parameters.AddWithValue("@Hitgroup", deathEvent.Hitgroup);
                        
                        await cmd.ExecuteNonQueryAsync();
                        await IncrementPlayerKills(deathEvent.AttackerID, Logger);
                        await IncrementPlayerAssists(deathEvent.AssisterID, Logger);
                        await IncrementPlayerDeaths(deathEvent.VictimID, Logger);

                        if (deathEvent.Hitgroup == 1) {
                            await IncrementPlayerHeadshots(deathEvent.AttackerID, Logger);
                        }


                    }
                    
                    Logger.LogInformation($"Batch of death events inserted successfully.");
                }
            }
            catch (Exception ex) {
                Logger.LogInformation(ex, "Error occurred while inserting batch of death events.");
            }
        }

        public async Task InsertLive(List<LivePlayer>? tPlayers, List<LivePlayer>? ctPlayers, int? tScore, int? ctScore, int? bombStatus, float? roundTime, ILogger Logger) {
            try {
                string tPlayersJson = JsonConvert.SerializeObject(tPlayers);
                string ctPlayersJson = JsonConvert.SerializeObject(ctPlayers);

                string query = @"
                INSERT INTO CS2S_Live (StaticID, TPlayers, CTPlayers, TScore, CTScore, BombStatus, RoundTime)
                VALUES (1, @TPlayers, @CTPlayers, @TScore, @CTScore, @BombStatus, @RoundTime)
                ON DUPLICATE KEY UPDATE 
                    TPlayers = @TPlayers, 
                    CTPlayers = @CTPlayers, 
                    TScore = @TScore, 
                    CTScore = @CTScore, 
                    BombStatus = @BombStatus, 
                    RoundTime = @RoundTime";


                using (MySqlCommand cmd = new MySqlCommand(query, this.conn)) {
                    cmd.Parameters.AddWithValue("@TPlayers", tPlayersJson);
                    cmd.Parameters.AddWithValue("@CTPlayers", ctPlayersJson);
                    cmd.Parameters.AddWithValue("@TScore", tScore);
                    cmd.Parameters.AddWithValue("@CTScore", ctScore);
                    cmd.Parameters.AddWithValue("@BombStatus", bombStatus);
                    cmd.Parameters.AddWithValue("@RoundTime", roundTime);

                    await cmd.ExecuteNonQueryAsync();
                    Logger.LogInformation("Live data inserted successfully.");
                }
            }

            catch (Exception ex) {
                Logger.LogInformation(ex, "Error occurred while inserting live data.");
            }
        }

        public async Task<int?> BeginInsertMatch(string mapName, ILogger Logger) {
            try {
                string query = @"
                INSERT INTO CS2S_Match (MapID, WinningTeamID, LosingTeamID, WinningTeamScore, LosingTeamScore, WinningSide)
                VALUES (@MapID, NULL, NULL, NULL, NULL, NULL);
                SELECT LAST_INSERT_ID();
                ";

                using (MySqlCommand cmd = new MySqlCommand(query, this.conn, this.transaction)) {
                    cmd.Parameters.AddWithValue("@MapID", mapName);
                    object? result = await cmd.ExecuteScalarAsync();

                    if (result != null && int.TryParse(result.ToString(), out int matchID)) {
                        Logger.LogInformation($"New match inserted successfully with MatchID {matchID}.");
                        return matchID;
                    }
                    else {
                        Logger.LogInformation("Failed to retrieve the MatchID.");
                        return null;
                    }
                }
            }
            catch (Exception ex) {
                Logger.LogInformation(ex, "Error occurred while inserting the match.");
                return null;
            }
        }

        public async Task<int?> BeginInsertRound(int? matchID, ILogger Logger) {
            if (matchID == null) {
                Logger.LogInformation("Match ID is null.");
                return null;
            }

            try {
                string query = @"
                INSERT INTO CS2S_Round (MatchID, WinningTeamID, LosingTeamID, WinningSide, RoundEndReason)
                VALUES (@MatchID, NULL, NULL, NULL, NULL);
                SELECT LAST_INSERT_ID();
                ";

                using (MySqlCommand cmd = new MySqlCommand(query, this.conn, this.transaction)) {
                    cmd.Parameters.AddWithValue("@MatchID", matchID);
                    object? result = await cmd.ExecuteScalarAsync();

                    if (result != null && int.TryParse(result.ToString(), out int roundID)) {
                        Logger.LogInformation($"New round inserted successfully with RoundID {roundID}.");
                        return roundID;
                    }
                    else {
                        Logger.LogInformation("Failed to retrieve the RoundID.");
                        return null;
                    }
                }
            }
            catch (Exception ex) {
                Logger.LogInformation(ex, "Error occurred while inserting the round.");
                return null;
            }
        }

        public async Task FinishRoundInsert(int? roundID, string winningTeamID, string losingTeamID, int winningSide, int roundEndReason, ILogger Logger) {
            try {
                string query = @"
                UPDATE CS2S_Round
                SET
                WinningTeamID = @WinningTeamID,
                LosingTeamID = @LosingTeamID,
                WinningSide = @WinningSide,
                RoundEndReason = @RoundEndReason
                WHERE RoundID = @RoundID
                ";

                using (MySqlCommand cmd = new MySqlCommand(query, this.conn, this.transaction)) {
                    cmd.Parameters.AddWithValue("@WinningTeamID", winningTeamID);
                    cmd.Parameters.AddWithValue("@LosingTeamID", losingTeamID);
                    cmd.Parameters.AddWithValue("@WinningSide", winningSide);
                    cmd.Parameters.AddWithValue("@RoundEndReason", roundEndReason);
                    cmd.Parameters.AddWithValue("@RoundID", roundID);

                    await cmd.ExecuteNonQueryAsync();
                    Logger.LogInformation($"Round {roundID} updated successfully.");
                }
            }

            catch (Exception ex) {
                Logger.LogInformation(ex, "Error occurred while updating round.");
            }
        }

        public async Task FinishMatchInsert(int? matchID, string winningTeamID, string losingTeamID, int? winningTeamScore, int? losingTeamScore, int? winningSide, int deltaELO, ILogger Logger) {
            try {
                string query = @"
                UPDATE CS2S_Match
                SET
                WinningTeamID = @WinningTeamID,
                LosingTeamID = @LosingTeamID,
                WinningTeamScore = @WinningTeamScore,
                LosingTeamScore = @LosingTeamScore,
                WinningSide = @WinningSide,
                DeltaELO = @DeltaELO
                WHERE MatchID = @MatchID
                ";

                using (MySqlCommand cmd = new MySqlCommand(query, this.conn, this.transaction)) {
                    cmd.Parameters.AddWithValue("@WinningTeamID", winningTeamID);
                    cmd.Parameters.AddWithValue("@LosingTeamID", losingTeamID);
                    cmd.Parameters.AddWithValue("@WinningTeamScore", winningTeamScore);
                    cmd.Parameters.AddWithValue("@LosingTeamScore", losingTeamScore);
                    cmd.Parameters.AddWithValue("@WinningSide", winningSide);
                    cmd.Parameters.AddWithValue("@DeltaELO", deltaELO);
                    cmd.Parameters.AddWithValue("@MatchID", matchID);

                    await cmd.ExecuteNonQueryAsync();
                    Logger.LogInformation($"Match {matchID} updated successfully.");
                }
            }

            catch (Exception ex) {
                Logger.LogInformation(ex, "Error occurred while updating match.");
            }
        }

        public async Task IncrementPlayerRoundsPlayed(ulong playerID, ILogger Logger) {
            try {
                string query = @"
                UPDATE CS2S_Player
                SET RoundsPlayed = RoundsPlayed + 1
                WHERE PlayerID = @PlayerID;
                ";

                using (MySqlCommand cmd = new MySqlCommand(query, this.conn, this.transaction)) {
                    cmd.Parameters.AddWithValue("@PlayerID", playerID);

                    int rowsAffected = await cmd.ExecuteNonQueryAsync();

                    if (rowsAffected > 0) {
                        Logger.LogInformation($"Successfully incremented RoundsPlayed for player {playerID}.");
                    }
                    else {
                        Logger.LogInformation($"No rows were updated. Player {playerID} might not exist.");
                    }
                }
            }
            catch (Exception ex) {
                Logger.LogInformation(ex, $"Error occurred while incrementing RoundsPlayed for player {playerID}.");
            }
        }

        public async Task IncrementPlayerMatchesPlayed(ulong playerID, ILogger Logger) {
            try {
                string query = @"
                UPDATE CS2S_Player
                SET MatchesPlayed = MatchesPlayed + 1
                WHERE PlayerID = @PlayerID;
                ";

                using (MySqlCommand cmd = new MySqlCommand(query, this.conn, this.transaction)) {
                    cmd.Parameters.AddWithValue("@PlayerID", playerID);

                    int rowsAffected = await cmd.ExecuteNonQueryAsync();

                    if (rowsAffected > 0) {
                        Logger.LogInformation($"Successfully incremented MatchesPlayed for player {playerID}.");
                    }
                    else {
                        Logger.LogInformation($"No rows were updated. Player {playerID} might not exist.");
                    }
                }
            }
            catch (Exception ex) {
                Logger.LogInformation(ex, $"Error occurred while incrementing MatchesPlayed for player {playerID}.");
            }
        }

    }

}
