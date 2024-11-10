using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using System.Transactions;

namespace CS2Stats {

    public partial class Database {

        public MySqlConnection? conn;
        public MySqlTransaction? transaction;
        private readonly string connString;

        public Database(string server, string db, string username, string password) {
            this.connString = $"SERVER={server};" +
                               $"DATABASE={db};" +
                               $"UID={username};" +
                               $"PASSWORD={password};";
        }

        public async Task CreateConnection() {
            if (this.conn != null) {
                await this.conn.CloseAsync();
                this.conn = null;
            }

            MySqlConnection conn = new(this.connString);
            await conn.OpenAsync();
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

        public async Task RollbackTransaction() {
            if (this.transaction != null) {
                await this.transaction.RollbackAsync();
                this.transaction = null;
            }
        }

        public async Task<int> GetLastMatchID(ILogger Logger) {
            try {
                string query = @"
                SELECT MAX(MatchID) 
                FROM CS2S_Match
                ";

                MySqlConnection tempConn = new(this.connString);
                await tempConn.OpenAsync();

                using MySqlCommand cmd = new(query, tempConn);
                object? result = await cmd.ExecuteScalarAsync();
                await tempConn.CloseAsync();

                if (result != null && int.TryParse(result.ToString(), out int matchID)) {
                    Logger.LogInformation($"[GetNextMatchID] Last MatchID is {matchID}.");
                    return matchID;
                }

                else {
                    Logger.LogInformation($"[GetNextMatchID] No previous MatchID found... Defaulting to 0.");
                    return 0;
                }
            }

            catch (Exception ex) {
                Logger.LogError(ex, "[GetNextMatchID] Error occurred while retrieving last MatchID.");
                return 0;
            }
        }

        public async Task<int> GetLastRoundID(ILogger Logger) {
            try {
                string query = @"
                SELECT MAX(RoundID) 
                FROM CS2S_Round
                ";

                MySqlConnection tempConn = new(this.connString);
                await tempConn.OpenAsync();

                using MySqlCommand cmd = new(query, tempConn);
                object? result = await cmd.ExecuteScalarAsync();
                await tempConn.CloseAsync();

                if (result != null && int.TryParse(result.ToString(), out int roundID)) {
                    Logger.LogInformation($"[GetLastRoundID] Last RoundID is {roundID}.");
                    return roundID;
                }

                else {
                    Logger.LogInformation($"[GetLastRoundID] No previous RoundID found... Defaulting to 0.");
                    return 0;
                }
            }

            catch (Exception ex) {
                Logger.LogError(ex, "[GetLastRoundID] Error occurred while retrieving last RoundID.");
                return 0;
            }
        }

        public async Task<int?> GetTeamAverageELO(string teamID, ILogger Logger) {
            try {
                string query = @"
                SELECT AVG(p.ELO) 
                FROM CS2S_PlayerInfo p
                INNER JOIN CS2S_Team_Players tp ON p.PlayerID = tp.PlayerID
                WHERE tp.TeamID = @TeamID;
                ";

                using MySqlCommand cmd = new(query, this.conn, this.transaction);
                cmd.Parameters.AddWithValue("@TeamID", teamID);
                object? result = await cmd.ExecuteScalarAsync();

                if (result != null && double.TryParse(result.ToString(), out double averageELO)) {
                    Logger.LogInformation($"[GetTeamAverageELO] Average ELO for team {teamID} is {averageELO}.");
                    return (int)averageELO;
                }

                else {
                    Logger.LogInformation($"[GetTeamAverageELO] No players found for team {teamID} or failed to retrieve average ELO.");
                    return null;
                }
            }
            catch (Exception ex) {
                Logger.LogError(ex, "[GetTeamAverageELO] Error occurred while retrieving average ELO for team.");
                return null;
            }
        }

        public async Task InsertPlayer(ulong playerID, int side, ILogger Logger) {
            try {
                string query = @"
                INSERT INTO CS2S_PlayerStats (PlayerID, Side)
                VALUES (@PlayerID, @Side)
                ON DUPLICATE KEY UPDATE
                    PlayerID = PlayerID
                ";

                MySqlConnection tempConn = new(this.connString);
                await tempConn.OpenAsync();

                using MySqlCommand cmd = new(query, tempConn);
                cmd.Parameters.AddWithValue("@PlayerID", playerID);
                cmd.Parameters.AddWithValue("@Side", side);

                await cmd.ExecuteNonQueryAsync();
                await tempConn.CloseAsync();

                Logger.LogInformation($"[InsertPlayer] Player {playerID} on side {side} inserted successfully.");
            }

            catch (Exception ex) {
                Logger.LogError(ex, $"[InsertPlayer] Error occurred while inserting player on side {side}.");
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

                MySqlConnection tempConn = new(this.connString);
                await tempConn.OpenAsync();

                using MySqlCommand cmd = new(query, tempConn);
                cmd.Parameters.AddWithValue("@PlayerID", player.PlayerID);
                cmd.Parameters.AddWithValue("@Username", player.Username);
                cmd.Parameters.AddWithValue("@AvatarS", player.AvatarS);
                cmd.Parameters.AddWithValue("@AvatarM", player.AvatarM);
                cmd.Parameters.AddWithValue("@AvatarL", player.AvatarL);

                await cmd.ExecuteNonQueryAsync();
                await tempConn.CloseAsync();

                Logger.LogInformation($"[InsertPlayerInfo] PlayerInfo {player.Username} inserted successfully.");
            }

            catch (Exception ex) {
                Logger.LogError(ex, "[InsertPlayerInfo] Error occurred while inserting players.");
            }
        }

        public async Task InsertTeamsAndTeamPlayers(Match match, ILogger Logger) {
            try {
                foreach (TeamInfo teamInfo in match.StartingPlayers.Values) {
                    await InsertTeam(teamInfo, Logger);
                    await InsertTeamPlayers(teamInfo, Logger);
                    await InsertPlayerMatches(match, teamInfo, Logger);
                }

            }
            catch (Exception ex) {
                Logger.LogError(ex, "[InsertTeamsAndTeamPlayers] Error occurred while inserting teams and team players.");
                return;
            }
        }

        public async Task InsertTeamResult(Match match, TeamInfo teamInfo, ILogger Logger) {
            try {
                string query = @"
                INSERT INTO CS2S_TeamResult (TeamID, MatchID, Score, Result, Side, DeltaELO)
                VALUES (@TeamID, @MatchID, @Score, @Result, @Side, @DeltaELO)
                ";

                using MySqlCommand cmd = new(query, this.conn, this.transaction);
                cmd.Parameters.AddWithValue("@TeamID", teamInfo.TeamID);
                cmd.Parameters.AddWithValue("@MatchID", match.MatchID);
                cmd.Parameters.AddWithValue("@Score", teamInfo.Score);
                cmd.Parameters.AddWithValue("@Result", teamInfo.Result);
                cmd.Parameters.AddWithValue("@Side", teamInfo.Side);
                cmd.Parameters.AddWithValue("@DeltaELO", teamInfo.DeltaELO);

                await cmd.ExecuteNonQueryAsync();
                Logger.LogInformation($"[InsertTeamResult] Match {match.MatchID} added to Team {teamInfo.TeamID}.");
            }

            catch (Exception ex) {
                Logger.LogError(ex, $"[InsertTeamResult] Error occurred while inserting team results for team {teamInfo.TeamID}.");
            }
        }

        public async Task InsertMap(Match match, ILogger Logger) {
            try {
                string query = @"
                INSERT INTO CS2S_Map (MapID)
                VALUES (@MapID)
                ON DUPLICATE KEY UPDATE
                    MapID = MapID
                ";

                using MySqlCommand cmd = new(query, this.conn, this.transaction);
                cmd.Parameters.AddWithValue("@MapID", match.MapName);
                await cmd.ExecuteNonQueryAsync();

                Logger.LogInformation($"[InsertMap] Map {match.MapName} inserted successfully.");
            }

            catch (Exception ex) {
                Logger.LogError(ex, "[InsertMap] Error occurred while inserting the map.");
            }
        }

        public async Task InsertMatch(Match match, ILogger Logger) {
            try {
                string query = @"
                INSERT INTO CS2S_Match (MatchID, MapID, StartTick, EndTick)
                VALUES (@MatchID, @MapID, @StartTick, @EndTick);
                ";

                using MySqlCommand cmd = new(query, this.conn, this.transaction);
                cmd.Parameters.AddWithValue("@MatchID", match.MatchID);
                cmd.Parameters.AddWithValue("@MapID", match.MapName);
                cmd.Parameters.AddWithValue("@StartTick", match.StartTick);
                cmd.Parameters.AddWithValue("@EndTick", match.EndTick);

                await cmd.ExecuteNonQueryAsync();
                Logger.LogInformation("[InsertMatch] Match inserted successfully.");

            }
            catch (Exception ex) {
                Logger.LogError(ex, "[InsertMatch] Error occurred while inserting the match.");
            }
        }

        public async Task InsertRound(Match match, Round round, ILogger Logger) {
            try {
                string query = @"
                INSERT INTO CS2S_Round (RoundID, MatchID, WinnerTeamID, LoserTeamID, WinnerSide, LoserSide, RoundEndReason, StartTick, EndTick)
                VALUES (@RoundID, @MatchID, @WinnerTeamID, @LoserTeamID, @WinnerSide, @LoserSide, @RoundEndReason, @StartTick, @EndTick);
                ";

                using MySqlCommand cmd = new(query, this.conn, this.transaction);
                cmd.Parameters.AddWithValue("@RoundID", round.RoundID);
                cmd.Parameters.AddWithValue("@MatchID", match.MatchID);
                cmd.Parameters.AddWithValue("@WinnerTeamID", round.WinningTeamID);
                cmd.Parameters.AddWithValue("@LoserTeamID", round.LosingTeamID);
                cmd.Parameters.AddWithValue("@WinnerSide", round.WinningTeamNum);
                cmd.Parameters.AddWithValue("@LoserSide", round.LosingTeamNum);
                cmd.Parameters.AddWithValue("@RoundEndReason", round.WinningReason);
                cmd.Parameters.AddWithValue("@StartTick", round.StartTick);
                cmd.Parameters.AddWithValue("@EndTick", round.EndTick);

                await cmd.ExecuteNonQueryAsync();
                Logger.LogInformation("[InsertRound] Round inserted successfully.");

            }
            catch (Exception ex) {
                Logger.LogError(ex, "[InsertRound] Error occurred while inserting the round.");
            }
        }

        public async Task InsertLive(LiveData liveData, ILogger Logger) {
            try {
                string? tPlayersJson = liveData.TPlayers != null
                    ? JsonConvert.SerializeObject(liveData.TPlayers)
                    : null;

                string? ctPlayersJson = liveData.CTPlayers != null
                    ? JsonConvert.SerializeObject(liveData.CTPlayers)
                    : null;

                string query = @"
                INSERT INTO CS2S_Live (StaticID, TPlayers, CTPlayers, TScore, CTScore, BombStatus)
                VALUES (1, @TPlayers, @CTPlayers, @TScore, @CTScore, @BombStatus)
                ON DUPLICATE KEY UPDATE 
                    TPlayers = VALUES(TPlayers), 
                    CTPlayers = VALUES(CTPlayers), 
                    TScore = VALUES(TScore), 
                    CTScore = VALUES(CTScore), 
                    BombStatus = VALUES(BombStatus),
                    InsertDate = CURRENT_TIMESTAMP
                ";

                MySqlConnection tempConn = new(this.connString);
                await tempConn.OpenAsync();

                using MySqlCommand cmd = new(query, tempConn);
                cmd.Parameters.AddWithValue("@TPlayers", tPlayersJson);
                cmd.Parameters.AddWithValue("@CTPlayers", ctPlayersJson);
                cmd.Parameters.AddWithValue("@TScore", liveData.TScore);
                cmd.Parameters.AddWithValue("@CTScore", liveData.CTScore);
                cmd.Parameters.AddWithValue("@BombStatus", liveData.BombStatus);

                await cmd.ExecuteNonQueryAsync();
                await tempConn.CloseAsync();

                Logger.LogInformation("[InsertLive] Live data inserted successfully.");
            }

            catch (Exception ex) {
                Logger.LogError(ex, "[InsertLive] Error occurred while inserting live data.");
            }
        }

        public async Task InsertClutchEvent(Match match, Round round, ILogger Logger) {
            if (round.ClutchEvent == null) {
                Logger.LogInformation("[InsertClutchEvent] Clutch event is null.");
                return;
            }

            try {
                string query = @"
                INSERT INTO CS2S_Clutch (RoundID, MatchID, PlayerID, PlayerSide, EnemyCount, Result)
                VALUES (@RoundID, @MatchID, @PlayerID, @PlayerSide, @EnemyCount, @Result);
                ";

                using MySqlCommand cmd = new(query, this.conn, this.transaction);
                cmd.Parameters.AddWithValue("@RoundID", round.RoundID);
                cmd.Parameters.AddWithValue("@MatchID", match.MatchID);
                cmd.Parameters.AddWithValue("@PlayerID", round.ClutchEvent.ClutcherID);
                cmd.Parameters.AddWithValue("@PlayerSide", round.ClutchEvent.ClutcherSide);
                cmd.Parameters.AddWithValue("@EnemyCount", round.ClutchEvent.EnemyCount);
                cmd.Parameters.AddWithValue("@Result", round.ClutchEvent.Result);
                await cmd.ExecuteNonQueryAsync();

                await IncrementPlayerValue(round.ClutchEvent.ClutcherID, round.ClutchEvent.ClutcherSide, "ClutchAttempts", Logger);

                if (round.ClutchEvent.Result == "Win") {
                    await IncrementPlayerValue(round.ClutchEvent.ClutcherID, round.ClutchEvent.ClutcherSide, "ClutchWins", Logger);
                }

                Logger.LogInformation($"[InsertClutchEvent] Clutch event inserted successfully.");
            }
            catch (Exception ex) {
                Logger.LogError(ex, "[InsertClutchEvent] Error occurred while inserting clutch event.");
            }

        }

        public async Task InsertDuelEvent(Match match, Round round, ILogger Logger) {
            if (round.DuelEvent == null) {
                Logger.LogInformation("[InsertDuelEvent] Duel event is null.");
                return;
            }

            try {
                string query = @"
                INSERT INTO CS2S_Duel (RoundID, MatchID, WinnerID, WinnerSide, LoserID, LoserSide)
                VALUES (@RoundID, @MatchID, @WinnerID, @WinnerSide, @LoserID, @LoserSide);
                ";

                using MySqlCommand cmd = new(query, this.conn, this.transaction);
                cmd.Parameters.AddWithValue("@RoundID", round.RoundID);
                cmd.Parameters.AddWithValue("@MatchID", match.MatchID);
                cmd.Parameters.AddWithValue("@WinnerID", round.DuelEvent.Value.WinnerID);
                cmd.Parameters.AddWithValue("@WinnerSide", round.DuelEvent.Value.WinnerSide);
                cmd.Parameters.AddWithValue("@LoserID", round.DuelEvent.Value.LoserID);
                cmd.Parameters.AddWithValue("@LoserSide", round.DuelEvent.Value.LoserSide);
                await cmd.ExecuteNonQueryAsync();

                await IncrementPlayerValue(round.DuelEvent.Value.WinnerID, round.DuelEvent.Value.WinnerSide, "DuelAttempts", Logger);
                await IncrementPlayerValue(round.DuelEvent.Value.LoserID, round.DuelEvent.Value.LoserSide, "DuelAttempts", Logger);
                await IncrementPlayerValue(round.DuelEvent.Value.WinnerID, round.DuelEvent.Value.WinnerSide, "DuelWins", Logger);

                Logger.LogInformation($"[InsertDuelEvent] Duel event inserted successfully.");
            }
            catch (Exception ex) {
                Logger.LogError(ex, "[InsertDuelEvent] Error occurred while inserting duel event.");
            }

        }

        public async Task InsertBatchedHurtEvents(Match match, Round round, ILogger Logger) {
            if (round.HurtEvents == null || round.HurtEvents.Count == 0) {
                Logger.LogInformation("[InsertBatchedHurtEvents] Hurt events are null.");
                return;
            }

            try {
                string query = @"
                INSERT INTO CS2S_Hurt (RoundID, MatchID, AttackerID, AttackerSide, VictimID, VictimSide, DamageAmount, Weapon, Hitgroup, RoundTick)
                VALUES (@RoundID, @MatchID, @AttackerID, @AttackerSide, @VictimID, @VictimSide, @DamageAmount, @Weapon, @Hitgroup, @RoundTick);
                ";

                using MySqlCommand cmd = new(query, this.conn, this.transaction);
                foreach (HurtEvent hurtEvent in round.HurtEvents) {
                    cmd.Parameters.Clear();

                    cmd.Parameters.AddWithValue("@RoundID", round.RoundID);
                    cmd.Parameters.AddWithValue("@MatchID", match.MatchID);
                    cmd.Parameters.AddWithValue("@AttackerID", hurtEvent.AttackerID);
                    cmd.Parameters.AddWithValue("@AttackerSide", hurtEvent.AttackerSide);
                    cmd.Parameters.AddWithValue("@VictimID", hurtEvent.VictimID);
                    cmd.Parameters.AddWithValue("@VictimSide", hurtEvent.VictimSide);
                    cmd.Parameters.AddWithValue("@DamageAmount", hurtEvent.DamageAmount);
                    cmd.Parameters.AddWithValue("@Weapon", hurtEvent.Weapon);
                    cmd.Parameters.AddWithValue("@Hitgroup", hurtEvent.Hitgroup);
                    cmd.Parameters.AddWithValue("@RoundTick", hurtEvent.RoundTick);

                    await cmd.ExecuteNonQueryAsync();

                    if (hurtEvent.AttackerID != null && hurtEvent.AttackerSide != null) {
                        await IncrementPlayerDamage(hurtEvent.AttackerID.Value, hurtEvent.AttackerSide.Value, hurtEvent.Weapon, hurtEvent.DamageAmount, Logger);
                    }
                }

                Logger.LogInformation($"[InsertBatchedHurtEvents] Batch of hurt events inserted successfully.");
            }
            catch (Exception ex) {
                Logger.LogError(ex, "[InsertBatchedHurtEvents] Error occurred while inserting batch of hurt events.");
            }

        }

        public async Task InsertBatchedDeathEvents(Match match, Round round, ILogger Logger) {
            if (round.DeathEvents == null || round.DeathEvents.Count == 0) {
                Logger.LogInformation("[InsertBatchedDeathEvents] Death events are null.");
                return;
            }

            try {
                string query = @"
                INSERT INTO CS2S_Death (RoundID, MatchID, AttackerID, AttackerSide, AssisterID, AssisterSide, VictimID, VictimSide, Weapon, Hitgroup, OpeningDeath, RoundTick)
                VALUES (@RoundID, @MatchID, @AttackerID, @AttackerSide, @AssisterID, @AssisterSide, @VictimID, @VictimSide, @Weapon, @Hitgroup, @OpeningDeath, @RoundTick);
                ";

                using MySqlCommand cmd = new(query, this.conn, this.transaction);
                foreach (DeathEvent deathEvent in round.DeathEvents) {
                    cmd.Parameters.Clear();

                    cmd.Parameters.AddWithValue("@RoundID", round.RoundID);
                    cmd.Parameters.AddWithValue("@MatchID", match.MatchID);
                    cmd.Parameters.AddWithValue("@AttackerID", deathEvent.AttackerID);
                    cmd.Parameters.AddWithValue("@AttackerSide", deathEvent.AttackerSide);
                    cmd.Parameters.AddWithValue("@AssisterID", deathEvent.AssisterID);
                    cmd.Parameters.AddWithValue("@AssisterSide", deathEvent.AssisterSide);
                    cmd.Parameters.AddWithValue("@VictimID", deathEvent.VictimID);
                    cmd.Parameters.AddWithValue("@VictimSide", deathEvent.VictimSide);
                    cmd.Parameters.AddWithValue("@Weapon", deathEvent.Weapon);
                    cmd.Parameters.AddWithValue("@Hitgroup", deathEvent.Hitgroup);
                    cmd.Parameters.AddWithValue("@OpeningDeath", deathEvent.OpeningDeath);
                    cmd.Parameters.AddWithValue("@RoundTick", deathEvent.RoundTick);
                    await cmd.ExecuteNonQueryAsync();

                    if (deathEvent.AttackerID != null && deathEvent.AttackerSide != null) {
                        await IncrementPlayerValue(deathEvent.AttackerID.Value, deathEvent.AttackerSide.Value, "Kills", Logger);

                        if (deathEvent.Hitgroup == 1) {
                            await IncrementPlayerValue(deathEvent.AttackerID.Value, deathEvent.AttackerSide.Value, "Headshots", Logger);
                        }
                    }

                    if (deathEvent.AssisterID != null && deathEvent.AssisterSide != null) {
                        await IncrementPlayerValue(deathEvent.AssisterID.Value, deathEvent.AssisterSide.Value, "Assists", Logger);
                    }

                    await IncrementPlayerValue(deathEvent.VictimID, deathEvent.VictimSide, "Deaths", Logger);

                }

                Logger.LogInformation($"[InsertBatchedDeathEvents] Batch of death events inserted successfully.");
            }
            catch (Exception ex) {
                Logger.LogError(ex, "[InsertBatchedDeathEvents] Error occurred while inserting batch of death events.");
            }
        }

        public async Task InsertBatchedBlindEvents(Match match, Round round, ILogger Logger) {
            if (round.BlindEvents == null || round.BlindEvents.Count == 0) {
                Logger.LogInformation("[InsertBatchedBlindEvents] Blind events are null.");
                return;
            }

            try {
                string query = @"
                INSERT INTO CS2S_Blind (RoundID, MatchID, ThrowerID, ThrowerSide, BlindedID, BlindedSide, Duration, RoundTick)
                VALUES (@RoundID, @MatchID, @ThrowerID, @ThrowerSide, @BlindedID, @BlindedSide, @Duration, @RoundTick);
                ";

                using MySqlCommand cmd = new(query, this.conn, this.transaction);
                foreach (BlindEvent blindEvent in round.BlindEvents){
                    cmd.Parameters.Clear();

                    cmd.Parameters.AddWithValue("@RoundID", round.RoundID);
                    cmd.Parameters.AddWithValue("@MatchID", match.MatchID);
                    cmd.Parameters.AddWithValue("@ThrowerID", blindEvent.ThrowerID);
                    cmd.Parameters.AddWithValue("@ThrowerSide", blindEvent.ThrowerSide);
                    cmd.Parameters.AddWithValue("@BlindedID", blindEvent.BlindedID);
                    cmd.Parameters.AddWithValue("@BlindedSide", blindEvent.BlindedSide);
                    cmd.Parameters.AddWithValue("@Duration", blindEvent.Duration);
                    cmd.Parameters.AddWithValue("@RoundTick", blindEvent.RoundTick);

                    await cmd.ExecuteNonQueryAsync();
                    await IncrementPlayerValue(blindEvent.ThrowerID, blindEvent.ThrowerSide, "EnemiesFlashed", Logger);
                }

                Logger.LogInformation($"[InsertBatchedBlindEvents] Batch of blind events inserted successfully.");
            }

            catch (Exception ex) {
                Logger.LogError(ex, "[InsertBatchedBlindEvents] Error occurred while inserting batch of blind events.");
            }
        }

        public async Task InsertBatchedGrenadeEvents(Match match, Round round, ILogger Logger) {
            if (round.GrenadeEvents == null || round.GrenadeEvents.Count == 0) {
                Logger.LogInformation("[InsertBatchedGrenadeEvents] Grenade events are null.");
                return;
            }

            try {
                string query = @"
                INSERT INTO CS2S_Grenade (RoundID, MatchID, ThrowerID, ThrowerSide, Weapon, RoundTick)
                VALUES (@RoundID, @MatchID, @ThrowerID, @ThrowerSide, @Weapon, @RoundTick);
                ";

                using MySqlCommand cmd = new(query, this.conn, this.transaction);
                foreach (GrenadeEvent grenadeEvent in round.GrenadeEvents) {
                    cmd.Parameters.Clear();

                    cmd.Parameters.AddWithValue("@RoundID", round.RoundID);
                    cmd.Parameters.AddWithValue("@MatchID", match.MatchID);
                    cmd.Parameters.AddWithValue("@ThrowerID", grenadeEvent.ThrowerID);
                    cmd.Parameters.AddWithValue("@ThrowerSide", grenadeEvent.ThrowerSide);
                    cmd.Parameters.AddWithValue("@Weapon", grenadeEvent.Weapon);
                    cmd.Parameters.AddWithValue("@RoundTick", grenadeEvent.RoundTick);

                    await cmd.ExecuteNonQueryAsync();
                    await IncrementPlayerValue(grenadeEvent.ThrowerID, grenadeEvent.ThrowerSide, "GrenadesThrown", Logger);
                }

                Logger.LogInformation($"[InsertBatchedGrenadeEvents] Batch of grenade events inserted successfully.");
            }

            catch (Exception ex) {
                Logger.LogError(ex, "[InsertBatchedGrenadeEvents] Error occurred while inserting batch of grenade events.");
            }
        }

        public async Task InsertBatchedKAST(Match match, Round round, ILogger Logger) {
            if (round.KASTEvents == null || round.KASTEvents.Count == 0) {
                Logger.LogInformation("[InsertBatchedKAST] KAST events is null.");
                return;
            }

            try {
                string query = @"
                INSERT INTO CS2S_KAST (RoundID, MatchID, PlayerID, PlayerSide)
                VALUES (@RoundID, @MatchID, @PlayerID, @PlayerSide);
                ";

                using MySqlCommand cmd = new(query, this.conn, this.transaction);
                foreach (KASTEvent kastEvent in round.KASTEvents) {
                    cmd.Parameters.Clear();
                    cmd.Parameters.AddWithValue("@RoundID", round.RoundID);
                    cmd.Parameters.AddWithValue("@MatchID", match.MatchID);
                    cmd.Parameters.AddWithValue("@PlayerID", kastEvent.PlayerID);
                    cmd.Parameters.AddWithValue("@PlayerSide", kastEvent.PlayerSide);

                    await cmd.ExecuteNonQueryAsync();
                    await IncrementPlayerValue(kastEvent.PlayerID, kastEvent.PlayerSide, "RoundsKAST", Logger);
                }

                Logger.LogInformation($"[InsertBatchedPlayersKAST] Batch of KAST events inserted successfully.");
            }
            catch (Exception ex) {
                Logger.LogError(ex, "[InsertBatchedPlayersKAST] Error occurred while inserting batch of KAST events.");
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

                using (MySqlCommand cmd = new(updateTeamELOQuery, this.conn, this.transaction)) {
                    cmd.Parameters.AddWithValue("@DeltaELO", teamInfo.DeltaELO);
                    cmd.Parameters.AddWithValue("@TeamID", teamInfo.TeamID);
                    await cmd.ExecuteNonQueryAsync();

                    Logger.LogInformation($"[UpdateELO] Team {teamInfo.TeamID} ELO updated by {teamInfo.DeltaELO}.");
                }

                string updatePlayerELOQuery = @"
                UPDATE CS2S_PlayerInfo p
                JOIN CS2S_Team_Players tp ON p.PlayerID = tp.PlayerID
                SET p.ELO = p.ELO + @DeltaELO
                WHERE tp.TeamID = @TeamID;
                ";

                using (MySqlCommand cmd = new(updatePlayerELOQuery, this.conn, this.transaction)) {
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
