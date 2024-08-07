using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using CS2Stats.Structs;
using Microsoft.Extensions.Logging;

namespace CS2Stats {

    public partial class CS2Stats {

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
                    Logger.LogInformation("--------------------------------");

                    try {
                        // Insert Player
                        var insertPlayerTask = database.InsertPlayerAsync(playerKey, Logger);
                        insertPlayerTask.GetAwaiter().GetResult();

                        // Insert Player_Match
                        var insertPlayer_MatchTask = database.InsertPlayer_MatchAsync(playerKey, matchID, Logger);
                        insertPlayer_MatchTask.GetAwaiter().GetResult();

                        // Insert T TeamPlayer
                        if (player.Team == CsTeam.Terrorist) {
                            var insertTeamPlayerTask = database.InsertTeamPlayerAsync(teamTID, playerKey, Logger);
                            insertTeamPlayerTask.GetAwaiter().GetResult();

                            // Insert CT TeamPlayer
                        }
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
                // Get average ELO for each team
                var getAverageTELOTask = database.GetPlayerELOFromTeamIDAsync(teamTID, Logger);
                int? averageTELO = getAverageTELOTask.GetAwaiter().GetResult();

                var getAverageCTELOTask = database.GetPlayerELOFromTeamIDAsync(teamCTID, Logger);
                int? averageCTELO = getAverageCTELOTask.GetAwaiter().GetResult();

                if (averageTELO != null && averageCTELO != null) {
                    // Calculate the probability of the winner winning
                    // Math.Abs will calculate difference between two numbers, regardless if in the wrong order. Maths.Abs(5-15) would be 10, for example.
                    double winProbability = 1 / (1.0 + Math.Pow(10, Math.Abs((double)averageTELO - (double)averageCTELO) / 400.0));
                    int deltaELO = (int)(50 * (1 - winProbability));

                    if (teamTScore > teamCTScore) {
                        var updatePlayerELOFromTeamIDAsyncTask = database.UpdatePlayerELOFromTeamIDAsync(teamTID, deltaELO, true, Logger);
                        updatePlayerELOFromTeamIDAsyncTask.GetAwaiter().GetResult();

                        updatePlayerELOFromTeamIDAsyncTask = database.UpdatePlayerELOFromTeamIDAsync(teamCTID, deltaELO, false, Logger);
                        updatePlayerELOFromTeamIDAsyncTask.GetAwaiter().GetResult();
                    }

                    else {
                        var updatePlayerELOFromTeamIDAsyncTask = database.UpdatePlayerELOFromTeamIDAsync(teamTID, deltaELO, false, Logger);
                        updatePlayerELOFromTeamIDAsyncTask.GetAwaiter().GetResult();

                        updatePlayerELOFromTeamIDAsyncTask = database.UpdatePlayerELOFromTeamIDAsync(teamCTID, deltaELO, true, Logger);
                        updatePlayerELOFromTeamIDAsyncTask.GetAwaiter().GetResult();
                    }
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

        public HookResult EventRoundStartHandler(EventRoundStart @event, GameEventInfo info) {

            if (!matchInProgress) {
                Logger.LogInformation($"EventRoundStart but matchInProgress = {matchInProgress}. Ignoring...");
                return HookResult.Continue;
            }

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

            if (startingPlayers != null) {
                foreach (var playerKey in startingPlayers.Keys) {
                    Logger.LogInformation($"Adding round for {playerKey}. From {startingPlayers[playerKey].RoundsPlayed} to {startingPlayers[playerKey].RoundsPlayed + 1}");
                    startingPlayers[playerKey].RoundsPlayed += 1;
                }
            }

            return HookResult.Continue;
        }

    }
}
