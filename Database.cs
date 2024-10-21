using MySql.Data.MySqlClient;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Text.RegularExpressions;

namespace CS2Stats {
    public partial class Database {

        public MySqlConnection? conn;
        public MySqlTransaction? transaction;
        private string ConnString;

        public Database(string server, string db, string username, string password) {
            this.ConnString = $"SERVER={server};" +
                               $"DATABASE={db};" +
                               $"UID={username};" +
                               $"PASSWORD={password};";

            MySqlConnection conn = new MySqlConnection(this.ConnString);
            conn.OpenAsync();
            this.conn = conn;

        }

        public async Task StartTransaction() {
            if (this.conn == null) {
                return;
            }

            if (this.transaction != null) {
                await this.transaction.RollbackAsync();
                await this.transaction.DisposeAsync();
                this.transaction = null;
            }

            this.transaction = this.conn.BeginTransaction();
        }

        public async Task CommitTransaction() {
            if (this.transaction != null) {
                await this.transaction.CommitAsync();
                await this.transaction.DisposeAsync();
                this.transaction = null;
            }
        }

        public async Task<int?> GetTeamAverageELO(string teamID, ILogger Logger) {
            if (string.IsNullOrWhiteSpace(teamID)) {
                Logger.LogInformation("[GetTeamAverageELO] Team ID is null or empty.");
                return null;
            }

            try {
                string query = @"
                SELECT AVG(p.ELO) 
                FROM CS2S_Player p
                INNER JOIN CS2S_Team_Players tp ON p.PlayerID = tp.PlayerID
                WHERE tp.TeamID = @TeamID;
                ";

                using (MySqlCommand cmd = new MySqlCommand(query, this.conn, this.transaction)) {
                    cmd.Parameters.AddWithValue("@TeamID", teamID);
                    object? result = await cmd.ExecuteScalarAsync();

                    if (result != null && double.TryParse(result.ToString(), out double averageELO)) {
                        int roundedELO = (int)Math.Round(averageELO);
                        Logger.LogInformation($"[GetTeamAverageELO] Average ELO for team {teamID} is {roundedELO}.");
                        return roundedELO;
                    }
                    else {
                        Logger.LogInformation($"[GetTeamAverageELO] No players found for team {teamID} or failed to retrieve average ELO.");
                        return null;
                    }
                }
            }
            catch (Exception ex) {
                Logger.LogError(ex, "[GetTeamAverageELO] Error occurred while retrieving average ELO for team.");
                return null;
            }
        }

        public async Task InsertPlayerInfo(PlayerInfo player, ILogger Logger) {
            try {
                string query = @"
                INSERT INTO CS2S_PlayerInfo (PlayerID, Username, AvatarS, AvatarM, AvatarL)
                VALUES (@PlayerID, @Username, @AvatarS, @AvatarM, @AvatarL)
                ON DUPLICATE KEY UPDATE 
                    Username = VALUES(Username),
                    AvatarS = VALUES(AvatarS),
                    AvatarM = VALUES(AvatarM),
                    AvatarL = VALUES(AvatarL)
                ";

                using (MySqlCommand cmd = new MySqlCommand(query, this.conn, this.transaction)) {
                    cmd.Parameters.AddWithValue("@PlayerID", player.PlayerID);
                    cmd.Parameters.AddWithValue("@Username", player.Username);
                    cmd.Parameters.AddWithValue("@AvatarS", player.AvatarS);
                    cmd.Parameters.AddWithValue("@AvatarM", player.AvatarM);
                    cmd.Parameters.AddWithValue("@AvatarL", player.AvatarL);

                    await cmd.ExecuteNonQueryAsync();
                    Logger.LogInformation($"[InsertPlayerInfo] PlayerInfo {player.Username} inserted successfully.");
                }
            }

            catch (Exception ex) {
                Logger.LogError(ex, "[InsertPlayerInfo] Error occurred while inserting players.");
            }
        }

        public async Task InsertTeamsAndTeamPlayers(Match match, ILogger Logger) {
            try {
                foreach (TeamInfo teamInfo in match.StartingPlayers.Values) {
                    await InsertTeamAsync(teamInfo, Logger);
                    await InsertTeamPlayersAsync(teamInfo, Logger);
                    await InsertPlayerMatchesAsync(match, teamInfo, Logger);
                    await BeginInsertTeamResultAsync(match, teamInfo, Logger);
                }

            }
            catch (Exception ex) {
                Logger.LogError(ex, "[InsertTeamsAndTeamPlayers] Error occurred while inserting teams and team players.");
                return;
            }
        }

        public async Task InsertMap(string mapName, ILogger Logger) {
            try {
                string query = @"
                INSERT INTO CS2S_Map (MapID)
                VALUES (@MapID)
                ON DUPLICATE KEY UPDATE
                    MapID = MapID
                ";

                using (MySqlCommand cmd = new MySqlCommand(query, this.conn, this.transaction)) {
                    cmd.Parameters.AddWithValue("@MapID", mapName);
                    await cmd.ExecuteNonQueryAsync();

                    Logger.LogInformation($"[InsertMap] Map {mapName} inserted successfully.");
                }
            }

            catch (Exception ex) {
                Logger.LogError(ex, "[InsertMap] Error occurred while inserting the map.");
            }
        }

        public async Task InsertBatchedHurtEvents(Match match, ILogger Logger) {
            if (match.Round.hurtEvents == null || match.Round.hurtEvents.Count == 0) {
                Logger.LogInformation("[InsertBatchedHurtEvents] Hurt events are null.");
                return;
            }

            try {
                string query = @"
                INSERT INTO CS2S_Hurt (RoundID, MatchID, AttackerID, VictimID, DamageAmount, Weapon, Hitgroup, ServerTick)
                VALUES (@RoundID, @MatchID, @AttackerID, @VictimID, @DamageAmount, @Weapon, @Hitgroup, @ServerTick);
                ";

                using (MySqlCommand cmd = new MySqlCommand(query, this.conn, this.transaction)) {
                    foreach (HurtEvent hurtEvent in match.Round.hurtEvents) {
                        cmd.Parameters.Clear();

                        cmd.Parameters.AddWithValue("@RoundID", match.Round.RoundID);
                        cmd.Parameters.AddWithValue("@MatchID", match.MatchID);
                        cmd.Parameters.AddWithValue("@AttackerID", hurtEvent.AttackerID);
                        cmd.Parameters.AddWithValue("@VictimID", hurtEvent.VictimID);
                        cmd.Parameters.AddWithValue("@DamageAmount", hurtEvent.DamageAmount);
                        cmd.Parameters.AddWithValue("@Weapon", hurtEvent.Weapon);
                        cmd.Parameters.AddWithValue("@Hitgroup", hurtEvent.Hitgroup);
                        cmd.Parameters.AddWithValue("@ServerTick", hurtEvent.ServerTick);

                        await cmd.ExecuteNonQueryAsync();
                        await IncrementPlayerDamage(hurtEvent.AttackerID, hurtEvent.Weapon, hurtEvent.DamageAmount, Logger);
                    }

                    Logger.LogInformation($"[InsertBatchedHurtEvents] Batch of hurt events inserted successfully.");
                }
            }
            catch (Exception ex) {
                Logger.LogError(ex, "[InsertBatchedHurtEvents] Error occurred while inserting batch of hurt events.");
            }

        }

        public async Task InsertBatchedDeathEvents(Match match, ILogger Logger) {
            if (match.Round.deathEvents == null || match.Round.deathEvents.Count == 0) {
                Logger.LogInformation("[InsertBatchedDeathEvents] Hurt events are null.");
                return;
            }

            try {
                string query = @"
                INSERT INTO CS2S_Death (RoundID, MatchID, AttackerID, AssisterID, VictimID, Weapon, Hitgroup, OpeningDeath, ServerTick)
                VALUES (@RoundID, @MatchID, @AttackerID, @AssisterID, @VictimID, @Weapon, @Hitgroup, @OpeningDeath, @ServerTick);
                ";

                using (MySqlCommand cmd = new MySqlCommand(query, this.conn, this.transaction)) {
                    foreach (DeathEvent deathEvent in match.Round.deathEvents) {
                        cmd.Parameters.Clear();

                        cmd.Parameters.AddWithValue("@RoundID", match.Round.RoundID);
                        cmd.Parameters.AddWithValue("@MatchID", match.MatchID);
                        cmd.Parameters.AddWithValue("@AttackerID", deathEvent.AttackerID);
                        cmd.Parameters.AddWithValue("@AssisterID", deathEvent.AssisterID);
                        cmd.Parameters.AddWithValue("@VictimID", deathEvent.VictimID);
                        cmd.Parameters.AddWithValue("@Weapon", deathEvent.Weapon);
                        cmd.Parameters.AddWithValue("@Hitgroup", deathEvent.Hitgroup);
                        cmd.Parameters.AddWithValue("@OpeningDeath", deathEvent.OpeningDeath);
                        cmd.Parameters.AddWithValue("@ServerTick", deathEvent.ServerTick);

                        await cmd.ExecuteNonQueryAsync();
                        await IncrementPlayerKills(deathEvent.AttackerID, Logger);
                        await IncrementPlayerAssists(deathEvent.AssisterID, Logger);
                        await IncrementPlayerDeaths(deathEvent.VictimID, Logger);

                        if (deathEvent.Hitgroup == 1) {
                            await IncrementPlayerHeadshots(deathEvent.AttackerID, Logger);
                        }


                    }
                    
                    Logger.LogInformation($"[InsertBatchedDeathEvents] Batch of death events inserted successfully.");
                }
            }
            catch (Exception ex) {
                Logger.LogError(ex, "[InsertBatchedDeathEvents] Error occurred while inserting batch of death events.");
            }
        }

        public async Task InsertBatchedKAST(Match match, ILogger Logger) {
            if (match.Round.KASTEvents == null || match.Round.KASTEvents.Count == 0) {
                Logger.LogInformation("[InsertBatchedKAST] Players KAST is null.");
                return;
            }

            try {
                string query = @"
                INSERT INTO CS2S_KAST (RoundID, MatchID, PlayerID)
                VALUES (@RoundID, @MatchID, @PlayerID);
                ";

                using (MySqlCommand cmd = new MySqlCommand(query, this.conn, this.transaction)) {
                    foreach (ulong playerID in match.Round.KASTEvents) {
                        cmd.Parameters.Clear();
                        cmd.Parameters.AddWithValue("@RoundID", match.Round.RoundID);
                        cmd.Parameters.AddWithValue("@MatchID", match.MatchID);
                        cmd.Parameters.AddWithValue("@PlayerID", playerID);

                        await cmd.ExecuteNonQueryAsync();
                    }

                    Logger.LogInformation($"[InsertBatchedPlayersKAST] Batch of players KAST inserted successfully.");
                }
            }
            catch (Exception ex) {
                Logger.LogError(ex, "[InsertBatchedPlayersKAST] Error occurred while inserting batch of players KAST.");
            }

        }

        public async Task InsertLive(LiveData liveData, ILogger Logger) {
            try {
                string tPlayersJson = JsonConvert.SerializeObject(liveData.TPlayers);
                string ctPlayersJson = JsonConvert.SerializeObject(liveData.CTPlayers);

                string query = @"
                INSERT INTO CS2S_Live (StaticID, TPlayers, CTPlayers, TScore, CTScore, BombStatus, RoundTime)
                VALUES (1, @TPlayers, @CTPlayers, @TScore, @CTScore, @BombStatus, @RoundTime)
                ON DUPLICATE KEY UPDATE 
                    TPlayers = VALUES(TPlayers), 
                    CTPlayers = VALUES(CTPlayers), 
                    TScore = VALUES(TScore), 
                    CTScore = VALUES(CTScore), 
                    BombStatus = VALUES(BombStatus), 
                    RoundTime = VALUES(RoundTime)
                ";

                MySqlConnection tempConn = new MySqlConnection(this.ConnString);
                await tempConn.OpenAsync();
                using (MySqlCommand cmd = new MySqlCommand(query, tempConn)) {
                    cmd.Parameters.AddWithValue("@TPlayers", tPlayersJson);
                    cmd.Parameters.AddWithValue("@CTPlayers", ctPlayersJson);
                    cmd.Parameters.AddWithValue("@TScore", liveData.TScore);
                    cmd.Parameters.AddWithValue("@CTScore", liveData.CTScore);
                    cmd.Parameters.AddWithValue("@BombStatus", liveData.BombStatus);
                    cmd.Parameters.AddWithValue("@RoundTime", liveData.RoundTime);

                    await cmd.ExecuteNonQueryAsync();
                    Logger.LogInformation("[InsertLive] Live data inserted successfully.");
                }

            }

            catch (Exception ex) {
                Logger.LogError(ex, "[InsertLive] Error occurred while inserting live data.");
            }
        }

        public async Task<int?> BeginInsertMatch(Match match, ILogger Logger) {
            try {
                string query = @"
                INSERT INTO CS2S_Match (MapID, BeginServerTick, FinishServerTick)
                VALUES (@MapID, @BeginServerTick, NULL);
                SELECT LAST_INSERT_ID();
                ";

                using (MySqlCommand cmd = new MySqlCommand(query, this.conn, this.transaction)) {
                    cmd.Parameters.AddWithValue("@MapID", match.MapName);
                    cmd.Parameters.AddWithValue("@BeginServerTick", match.beginServerTick);

                    object? result = await cmd.ExecuteScalarAsync();

                    if (result != null && int.TryParse(result.ToString(), out int matchID)) {
                        Logger.LogInformation($"[BeginInsertMatch] New match inserted successfully with MatchID {matchID}.");
                        return matchID;
                    }
                    else {
                        Logger.LogInformation("[BeginInsertMatch] Failed to retrieve MatchID.");
                        return null;
                    }
                }
            }
            catch (Exception ex) {
                Logger.LogError(ex, "[BeginInsertMatch] Error occurred while inserting the match.");
                return null;
            }
        }

        public async Task<int?> BeginInsertRound(Match match, ILogger Logger) {
            try {
                string query = @"
                INSERT INTO CS2S_Round (MatchID, WinningTeamID, LosingTeamID, WinningSide, RoundEndReason, ServerTick)
                VALUES (@MatchID, NULL, NULL, NULL, NULL, @ServerTick);
                SELECT LAST_INSERT_ID();
                ";

                using (MySqlCommand cmd = new MySqlCommand(query, this.conn, this.transaction)) {
                    cmd.Parameters.AddWithValue("@MatchID", match.MatchID);
                    cmd.Parameters.AddWithValue("@ServerTick", match.Round.serverTick);
                    object? result = await cmd.ExecuteScalarAsync();

                    if (result != null && int.TryParse(result.ToString(), out int roundID)) {
                        Logger.LogInformation($"[BeginInsertRound] New round inserted successfully with RoundID {roundID}.");
                        return roundID;
                    }
                    else {
                        Logger.LogInformation("[BeginInsertRound] Failed to retrieve the RoundID.");
                        return null;
                    }
                }
            }
            catch (Exception ex) {
                Logger.LogError(ex, "[BeginInsertRound] Error occurred while inserting the round.");
                return null;
            }
        }

        public async Task FinishInsertRound(int? roundID, string winningTeamID, string losingTeamID, int winningSide, int roundEndReason, ILogger Logger) {
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
                    Logger.LogInformation($"[FinishInsertRound] Round {roundID} updated successfully.");
                }
            }

            catch (Exception ex) {
                Logger.LogError(ex, "[FinishInsertRound] Error occurred while updating round.");
            }
        }

        public async Task FinishInsertMatch(Match match, ILogger Logger) {
            try {
                string query = @"
                UPDATE CS2S_Match
                SET
                FinishServerTick = @FinishServerTick
                WHERE MatchID = @MatchID
                ";

                using (MySqlCommand cmd = new MySqlCommand(query, this.conn, this.transaction)) {
                    cmd.Parameters.AddWithValue("@FinishServerTick", match.finishServerTick);
                    cmd.Parameters.AddWithValue("@MatchID", match.MatchID);

                    await cmd.ExecuteNonQueryAsync();
                    Logger.LogInformation($"[FinishInsertMatch] Match {match.MatchID} updated successfully.");
                }
            }

            catch (Exception ex) {
                Logger.LogError(ex, "[FinishInsertMatch] Error occurred while updating match.");
            }
        }

        public async Task FinishInsertTeamResult(Match match, TeamInfo teamInfo, ILogger Logger) {
            string query = @"
            UPDATE CS2S_TeamResult
            SET
            Score = @Score,
            Result = @Result,
            DeltaELO = @DeltaELO
            WHERE MatchID = @MatchID AND TeamID = @TeamID
            ";

            using (MySqlCommand cmd = new MySqlCommand(query, this.conn, this.transaction)) {
                cmd.Parameters.AddWithValue("@Score", teamInfo.Score);
                cmd.Parameters.AddWithValue("@Result", teamInfo.Result);
                cmd.Parameters.AddWithValue("@DeltaELO", teamInfo.DeltaELO);
                cmd.Parameters.AddWithValue("@MatchID", match.MatchID);
                cmd.Parameters.AddWithValue("@TeamID", teamInfo.TeamID);

                await cmd.ExecuteNonQueryAsync();
                Logger.LogInformation($"[FinishInsertTeamResult] Match {match.MatchID} TeamResult added to Team {teamInfo.TeamID}.");
            }
        }

        public async Task InsertMultiplePlayers(HashSet<ulong> playerIDs, ILogger Logger) {
            try {
                string query = @"
                INSERT INTO CS2S_Player (PlayerID)
                VALUES (@PlayerID)
                ON DUPLICATE KEY UPDATE
                    PlayerID = PlayerID
                ";

                using (MySqlCommand cmd = new MySqlCommand(query, this.conn, this.transaction)) {
                    foreach (ulong playerID in playerIDs) {
                        cmd.Parameters.Clear();

                        cmd.Parameters.AddWithValue("@PlayerID", playerID);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
            }

            catch (Exception ex) {
                Logger.LogError(ex, "[InsertMultiplePlayers] Error occurred while inserting player.");
            }
        }

        public async Task IncrementMultiplePlayerRoundsPlayed(HashSet<ulong> playerIDs, ILogger Logger) {
            if (playerIDs == null || playerIDs.Count == 0) {
                Logger.LogInformation("[IncrementMultiplePlayerRoundsPlayed] Player IDs list is null or empty.");
                return;
            }

            try {
                string query = @"
                UPDATE CS2S_Player
                SET RoundsPlayed = RoundsPlayed + 1
                WHERE PlayerID = @PlayerID;
                ";

                using (MySqlCommand cmd = new MySqlCommand(query, this.conn, this.transaction)) {
                    foreach (ulong playerID in playerIDs) {
                        cmd.Parameters.Clear();

                        cmd.Parameters.AddWithValue("@PlayerID", playerID);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                Logger.LogInformation($"[IncrementMultiplePlayerRoundsPlayed] Successfully incremented RoundsPlayed for {playerIDs.Count} players.");
            }
            catch (Exception ex) {
                Logger.LogError(ex, "[IncrementMultiplePlayerRoundsPlayed] Error occurred while incrementing RoundsPlayed for batch of players.");
            }
        }
        
        public async Task IncrementMultiplePlayerRoundsKAST(HashSet<ulong> playerIDs, ILogger Logger) {
            if (playerIDs == null || playerIDs.Count == 0) {
                Logger.LogInformation("[IncrementMultiplePlayerRoundsKAST] Player IDs list is null or empty.");
                return;
            }

            try {
                string query = @"
                UPDATE CS2S_Player
                SET RoundsKAST = RoundsKAST + 1
                WHERE PlayerID = @PlayerID;
                ";

                using (MySqlCommand cmd = new MySqlCommand(query, this.conn, this.transaction)) {
                    foreach (ulong playerID in playerIDs) {
                        cmd.Parameters.Clear();

                        cmd.Parameters.AddWithValue("@PlayerID", playerID);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                Logger.LogInformation($"[IncrementMultiplePlayerRoundsKAST] Successfully incremented RoundsKAST for {playerIDs.Count} players.");
            }
            catch (Exception ex) {
                Logger.LogError(ex, "[IncrementMultiplePlayerRoundsKAST] Error occurred while incrementing RoundsKAST for batch of players.");
            }
        }

        public async Task IncrementMultiplePlayerMatchesPlayed(HashSet<ulong> playerIDs, ILogger Logger) {
            if (playerIDs == null || playerIDs.Count == 0) {
                Logger.LogInformation("[IncrementMultiplePlayerMatchesPlayed] Player IDs list is null or empty.");
                return;
            }

            try {
                string query = @"
                UPDATE CS2S_Player
                SET MatchesPlayed = MatchesPlayed + 1
                WHERE PlayerID = @PlayerID;
                ";

                using (MySqlCommand cmd = new MySqlCommand(query, this.conn, this.transaction)) {
                    foreach (ulong playerID in playerIDs) {
                        cmd.Parameters.Clear();

                        cmd.Parameters.AddWithValue("@PlayerID", playerID);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                Logger.LogInformation($"[IncrementMultiplePlayerMatchesPlayed] Successfully incremented MatchesPlayed for {playerIDs.Count} players.");
            }
            catch (Exception ex) {
                Logger.LogError(ex, "[IncrementMultiplePlayerMatchesPlayed] Error occurred while incrementing MatchesPlayed for batch of players.");
            }
        }

        public async Task UpdateELO(TeamInfo teamInfo, ILogger Logger) {
            if (string.IsNullOrWhiteSpace(teamInfo.TeamID)) {
                Logger.LogInformation("[IncrementTeamELO] Team ID is null or empty.");
                return;
            }

            try {
                string updateTeamELOQuery = @"
                UPDATE CS2S_Team
                SET ELO = ELO + @DeltaELO
                WHERE TeamID = @TeamID;
                ";

                using (MySqlCommand cmd = new MySqlCommand(updateTeamELOQuery, this.conn, this.transaction)) {
                    cmd.Parameters.AddWithValue("@DeltaELO", teamInfo.DeltaELO);
                    cmd.Parameters.AddWithValue("@TeamID", teamInfo.TeamID);
                    await cmd.ExecuteNonQueryAsync();

                    Logger.LogInformation($"[UpdateELO] Team {teamInfo.TeamID} ELO updated by {teamInfo.DeltaELO}.");
                }

                string updatePlayerELOQuery = @"
                UPDATE CS2S_Player p
                JOIN CS2S_Team_Players tp ON p.PlayerID = tp.PlayerID
                SET p.ELO = p.ELO + @DeltaELO
                WHERE tp.TeamID = @TeamID;
                ";

                using (MySqlCommand cmd = new MySqlCommand(updatePlayerELOQuery, this.conn, this.transaction)) {
                    cmd.Parameters.AddWithValue("@DeltaELO", teamInfo.DeltaELO);
                    cmd.Parameters.AddWithValue("@TeamID", teamInfo.TeamID);
                    await cmd.ExecuteNonQueryAsync();
                    Logger.LogInformation($"[UpdateELO] Players in team {teamInfo.TeamID} ELO updated by {teamInfo.DeltaELO}.");
                }
            }
            catch (Exception ex) {
                Logger.LogError(ex, "[UpdateELO] Error occurred while updating team and players ELO.");
            }
        }

    }

}
