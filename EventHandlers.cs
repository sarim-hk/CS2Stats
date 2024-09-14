using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;

namespace CS2Stats
{

    public partial class CS2Stats {

        /*
        public HookResult EventCsWinPanelMatchHandler(EventCsWinPanelMatch @event, GameEventInfo info) {
            if (!matchInProgress) {
                Logger.LogInformation($"EventCsWinPanelMatch but matchInProgress = {matchInProgress}. Ignoring...");
                return HookResult.Continue;
            }

            // Ignore null reference warning here... Plugin would be unloaded if database is null so you can ignore :)
            // Insert T & CT Team
            var insertTTeamTask = database.InsertTeamAsync(Logger);
            int? teamTID = insertTTeamTask.GetAwaiter().GetResult();

            var insertCTTeamTask = database.InsertTeamAsync(Logger);
            int? teamCTID = insertCTTeamTask.GetAwaiter().GetResult();

            // Insert Match
            int? teamTScore = GetCSTeamScore(CsTeam.Terrorist);
            int? teamCTScore = GetCSTeamScore(CsTeam.CounterTerrorist);

            var insertMatchTask = database.InsertMatchAsync(Server.MapName, teamTID, teamCTID, teamTScore, teamCTScore, Logger);
            int? matchID = insertMatchTask.GetAwaiter().GetResult();

            if (startingPlayers != null) {
                foreach (var playerKey in startingPlayers.Keys) {
                    Player player = startingPlayers[playerKey];

                    Logger.LogInformation($"PlayerID: {playerKey}");
                    Logger.LogInformation($"Kills: {player.Kills}");
                    Logger.LogInformation($"Headshots: {player.Headshots}");
                    Logger.LogInformation($"Assists: {player.Assists}");
                    Logger.LogInformation($"Deaths: {player.Deaths}");
                    Logger.LogInformation($"Total Damage: {player.TotalDamage}");
                    Logger.LogInformation($"Utility Damage: {player.UtilityDamage}");
                    Logger.LogInformation($"Rounds Played: {player.RoundsPlayed}");

                    Logger.LogInformation(player.Username);
                    Logger.LogInformation(player.AvatarS);
                    Logger.LogInformation(player.AvatarM);
                    Logger.LogInformation(player.AvatarL);


                    Logger.LogInformation("--------------------------------");

                    try {
                        // Insert Player
                        var insertPlayerTask = database.InsertPlayerAsync(playerKey, player, Logger);
                        insertPlayerTask.GetAwaiter().GetResult();

                        // Insert Player_Match
                        var insertPlayer_MatchTask = database.InsertPlayer_MatchAsync(playerKey, matchID, Logger);
                        insertPlayer_MatchTask.GetAwaiter().GetResult();

                        // Insert T TeamPlayer
                        if (player.Team == CsTeam.Terrorist) {
                            var insertTeamPlayerTask = database.InsertTeamPlayerAsync(teamTID, playerKey, Logger);
                            insertTeamPlayerTask.GetAwaiter().GetResult();
                        }

                        // Insert CT TeamPlayer
                        else if (player.Team == CsTeam.CounterTerrorist) {
                            var insertTeamPlayerTask = database.InsertTeamPlayerAsync(teamCTID, playerKey, Logger);
                            insertTeamPlayerTask.GetAwaiter().GetResult();
                        }

                        // Insert PlayerStat
                        var insertPlayerStatTask = database.InsertPlayerStatAsync(playerKey, player.Kills, player.Headshots, player.Assists, player.Deaths, player.TotalDamage, player.UtilityDamage, player.RoundsPlayed, Logger);
                        insertPlayerStatTask.GetAwaiter().GetResult();
                        int? playerStatID = insertPlayerStatTask.GetAwaiter().GetResult();

                        // Insert Player_PlayerStat
                        var insertPlayer_PlayerStatTask = database.InsertPlayer_PlayerStatTaskAsync(playerKey, playerStatID, Logger);
                        insertPlayer_PlayerStatTask.GetAwaiter().GetResult();

                        // Insert Match_PlayerStat
                        var insertMatch_PlayerStatTask = database.InsertMatch_PlayerStatAsync(matchID, playerStatID, Logger);
                        insertPlayerStatTask.GetAwaiter().GetResult();
                    }

                    catch (Exception ex) {
                        Logger.LogError(ex, "Error handling player data.");
                    }
                }
            }

            if (teamTScore != teamCTScore) {
                var getAverageTELOTask = database.GetPlayerELOFromTeamIDAsync(teamTID, Logger);
                int? averageTELO = getAverageTELOTask.GetAwaiter().GetResult();

                var getAverageCTELOTask = database.GetPlayerELOFromTeamIDAsync(teamCTID, Logger);
                int? averageCTELO = getAverageCTELOTask.GetAwaiter().GetResult();

                if (averageTELO != null && averageCTELO != null) {
                    // Calculate the probability of team T winning
                    double winProbabilityT = 1 / (1.0 + Math.Pow(10, ((double)averageCTELO - (double)averageTELO) / 400.0));
                    double winProbabilityCT = 1 - winProbabilityT; // Probability of team CT winning

                    // Actual outcome: 1 if team T won, 0 if team CT won
                    int actualOutcomeT = teamTScore > teamCTScore ? 1 : 0;
                    int actualOutcomeCT = 1 - actualOutcomeT; // Actual outcome for team CT

                    // Calculate ELO change for each team
                    int deltaTELO = (int)(50 * (actualOutcomeT - winProbabilityT));
                    int deltaCTELO = (int)(50 * (actualOutcomeCT - winProbabilityCT));

                    // Update ELO for team T and team CT
                    var updatePlayerELOFromTeamIDAsyncTask = database.UpdatePlayerELOFromTeamIDAsync(teamTID, deltaTELO, Logger);
                    updatePlayerELOFromTeamIDAsyncTask.GetAwaiter().GetResult();

                    updatePlayerELOFromTeamIDAsyncTask = database.UpdatePlayerELOFromTeamIDAsync(teamCTID, deltaCTELO, Logger);
                    updatePlayerELOFromTeamIDAsyncTask.GetAwaiter().GetResult();
                }
            }

            matchInProgress = false;
            Logger.LogInformation("Match ended.");
            return HookResult.Continue;
        }

        public HookResult EventRoundEndHandler(EventRoundEnd @event, GameEventInfo info) {

            if (!matchInProgress) {
                Logger.LogInformation($"EventRoundEnd but matchInProgress = {matchInProgress}. Ignoring...");
                return HookResult.Continue;
            }

            List<CCSPlayerController> playerControllers = Utilities.GetPlayers();
            foreach (var playerController in playerControllers) {
                if (playerController.IsValid && playerController.ActionTrackingServices != null && startingPlayers != null && startingPlayers.ContainsKey(playerController.SteamID) && !playerController.IsBot) {

                    var player = startingPlayers[playerController.SteamID];
                    player.Kills = playerController.ActionTrackingServices.MatchStats.Kills;
                    player.Headshots = playerController.ActionTrackingServices.MatchStats.HeadShotKills;
                    player.Assists = playerController.ActionTrackingServices.MatchStats.Assists;
                    player.Deaths = playerController.ActionTrackingServices.MatchStats.Deaths;
                    player.TotalDamage = playerController.ActionTrackingServices.MatchStats.Damage;
                    player.UtilityDamage = playerController.ActionTrackingServices.MatchStats.UtilityDamage;
                    startingPlayers[playerController.SteamID] = player;

                    Logger.LogInformation($"Player: {playerController.SteamID}");
                    Logger.LogInformation($"Kills: {player.Kills}");
                    Logger.LogInformation($"Headshots: {player.Headshots}");
                    Logger.LogInformation($"Assists: {player.Assists}");
                    Logger.LogInformation($"Deaths: {player.Deaths}");
                    Logger.LogInformation($"TotalDamage: {player.TotalDamage}");
                    Logger.LogInformation($"UtilityDamage: {player.UtilityDamage}");
                    Logger.LogInformation("--------------------------------");

                }

            }
            return HookResult.Continue;
        }

        public HookResult EventRoundAnnounceLastRoundHalfHandler(EventRoundAnnounceLastRoundHalf @event, GameEventInfo info) {

            if (!matchInProgress) {
                Logger.LogInformation($"EventRoundAnnounceLastRoundHalf but matchInProgress = {matchInProgress}. Ignoring...");
                return HookResult.Continue;
            }

            teamsNeedSwapping = true;
            Logger.LogInformation("Setting teamsNeedSwapping to true.");

            return HookResult.Continue;
        }
        */

        private void OnClientAuthorizedHandler(int playerSlot, SteamID playerID) {
            if (this.database == null || this.database.conn == null || this.steamAPIClient == null) {
                Logger.LogInformation("One or more required components are null. Ignoring...");
            }

            else {
                Player? player = this.steamAPIClient.GetSteamSummaryAsync(playerID.SteamId64).GetAwaiter().GetResult();
                if (player == null) {
                    Logger.LogInformation("Steam API Player is null.");
                }

                else {
                    this.database.StartTransaction();
                    this.database.InsertPlayer(player, playerID.SteamId64, Logger).GetAwaiter().GetResult();
                    this.database.CommitTransaction();
                }
            }
        }

        public HookResult EventRoundAnnounceLastRoundHalfHandler(EventRoundAnnounceLastRoundHalf @event, GameEventInfo info) {

            if (this.database == null || this.database.conn == null || this.database.transaction == null) {
                Logger.LogInformation($"EventRoundAnnounceLastRoundHalf but database conn/transaction is null. Ignoring...");
                return HookResult.Continue;
            }

            /*
            teamsNeedSwapping = true;
            Logger.LogInformation("Setting teamsNeedSwapping to true.");
            */

            return HookResult.Continue;
        }

        public HookResult EventRoundStartHandler(EventRoundStart @event, GameEventInfo info) {
            if (this.database == null || this.database.conn == null || this.database.transaction == null) {
                Logger.LogInformation($"EventRoundStart but database conn/transaction is null. Ignoring...");
                return HookResult.Continue;
            }

            /*
            if (startingPlayers != null) {
                foreach (var playerKey in startingPlayers.Keys) {
                    Logger.LogInformation($"Adding round for {playerKey}");
                    // check if theyre still connected
                }
            }*/

            /*
            if (teamsNeedSwapping) {
                if (startingPlayers != null) {
                    foreach (var playerKey in startingPlayers.Keys) {
                        Logger.LogInformation($"Swapping team for player {playerKey}.");
                        startingPlayers[playerKey].SwapTeam();
                    }
                }
                teamsNeedSwapping = false;
                Logger.LogInformation("Setting teamsNeedSwapping to false.");
            }
            */

            roundID = this.database.InsertRound(matchID, Logger).GetAwaiter().GetResult();

            return HookResult.Continue;
        }

        public HookResult EventCsWinPanelMatchHandler(EventCsWinPanelMatch @event, GameEventInfo info) {
            if (this.database == null || this.database.conn == null || this.database.transaction == null) {
                Logger.LogInformation($"EventCsWinPanelMatch but database conn/transaction is null. Ignoring...");
                return HookResult.Continue;
            }

            roundID = null;
            matchID = null;
            teamNum2ID = null;
            teamNum3ID = null;

            this.database.CommitTransaction();
            Logger.LogInformation("Match ended.");
            return HookResult.Continue;
        }

        public HookResult EventPlayerHurtHandler(EventPlayerHurt @event, GameEventInfo info) {
            if (this.database == null || this.database.conn == null || this.database.transaction == null) {
                Logger.LogInformation($"EventPlayerHurt but database conn/transaction is null. Ignoring...");
                return HookResult.Continue;
            }

            if (@event.Attacker != null && @event.Userid != null) {
                this.database.InsertHurt(@event.Attacker.SteamID, @event.Userid.SteamID, @event.Attacker.TeamNum,
                    @event.Userid.TeamNum, @event.DmgHealth, @event.Weapon, @event.Hitgroup, Logger)
                    .GetAwaiter().GetResult();
            }

            return HookResult.Continue;
        }

        public HookResult EventPlayerDeathHandler(EventPlayerDeath @event, GameEventInfo info) {
            if (this.database == null || this.database.conn == null || this.database.transaction == null) {
                Logger.LogInformation($"EventPlayerDeath but database conn/transaction is null. Ignoring...");
                return HookResult.Continue;
            }

            if (@event.Userid != null) {

                ulong? assisterID;
                if (@event.Assister != null) {
                    assisterID = @event.Assister.SteamID;
                }

                else {
                    assisterID = null;
                }

                ulong? attackerID;
                if (@event.Attacker != null) {
                    attackerID = @event.Attacker.SteamID;
                }

                else {
                    attackerID = null;
                }

                this.database.InsertDeath(roundID, attackerID, assisterID, @event.Userid.SteamID, @event.Attacker.TeamNum,
                    @event.Userid.TeamNum, @event.Weapon, @event.Hitgroup, Logger)
                    .GetAwaiter().GetResult();
            }

            return HookResult.Continue;
        }

        public HookResult EventRoundEndHandler(EventRoundEnd @event, GameEventInfo info) {
            if (this.database == null || this.database.conn == null || this.database.transaction == null) {
                Logger.LogInformation($"EventRoundAnnounceLastRoundHalf but database conn/transaction is null. Ignoring...");
                return HookResult.Continue;
            }

            // this.database.UpdateRound(roundID, "", "", @event.Winner, @event.Reason).GetAwaiter().GetResult();
            return HookResult.Continue;
        }
        }
}
