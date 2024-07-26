using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
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

            var insertMatchTask = database.InsertMatchAsync(teamTID, teamCTID, teamTScore, teamCTScore, Logger);
            int? matchID = insertMatchTask.GetAwaiter().GetResult();

            List<CCSPlayerController> playerControllers = Utilities.GetPlayers();
            foreach (var playerController in playerControllers) {
                if (playerRounds != null) {
                    if (playerController.IsValid && playerController.ActionTrackingServices != null && !playerController.IsBot && playerRounds.ContainsKey(playerController.SteamID)) {

                        ulong playerID = playerController.SteamID;
                        string username = playerController.PlayerName;
                        int kills = playerController.ActionTrackingServices.MatchStats.Kills;
                        int headshots = playerController.ActionTrackingServices.MatchStats.HeadShotKills;
                        int assists = playerController.ActionTrackingServices.MatchStats.Assists;
                        int deaths = playerController.ActionTrackingServices.MatchStats.Deaths;
                        int damage = playerController.ActionTrackingServices.MatchStats.Damage;
                        int utilityDamage = playerController.ActionTrackingServices.MatchStats.UtilityDamage;
                        int roundsPlayed = playerRounds[playerController.SteamID];

                        Logger.LogInformation($"PlayerID: {playerID}");
                        Logger.LogInformation($"Username: {username}");
                        Logger.LogInformation($"Kills: {kills}");
                        Logger.LogInformation($"Headshots: {headshots}");
                        Logger.LogInformation($"Assists: {assists}");
                        Logger.LogInformation($"Deaths: {deaths}");
                        Logger.LogInformation($"Damage: {damage}");
                        Logger.LogInformation($"Utility Damage: {utilityDamage}");
                        Logger.LogInformation($"Rounds Played: {roundsPlayed}");
                        Logger.LogInformation("--------------------------------");

                        try {
                            // Insert Player
                            var insertPlayerTask = database.InsertPlayerAsync(playerID, username, Logger);
                            insertPlayerTask.GetAwaiter().GetResult();

                            // Insert Player_Match
                            var insertPlayer_MatchTask = database.InsertPlayer_MatchAsync(playerID, matchID, Logger);
                            insertPlayer_MatchTask.GetAwaiter().GetResult();

                            // Insert T TeamPlayer
                            if (playerController.Team.Equals(CsTeam.Terrorist)) {
                                var insertTeamPlayerTask = database.InsertTeamPlayerAsync(teamTID, playerID, Logger);
                                insertTeamPlayerTask.GetAwaiter().GetResult();

                            // Insert CT TeamPlayer
                            } else if (playerController.Team.Equals(CsTeam.CounterTerrorist)) {
                                var insertTeamPlayerTask = database.InsertTeamPlayerAsync(teamCTID, playerID, Logger);
                                insertTeamPlayerTask.GetAwaiter().GetResult();
                            }

                            // Insert PlayerStat
                            var insertPlayerStatTask = database.InsertPlayerStatAsync(playerID, kills, headshots, assists, deaths, damage, utilityDamage, roundsPlayed, Logger);
                            insertPlayerStatTask.GetAwaiter().GetResult();
                            int? playerStatID = insertPlayerStatTask.GetAwaiter().GetResult();

                            // Insert Player_PlayerStat
                            var insertPlayer_PlayerStatTask = database.InsertPlayer_PlayerStatTaskAsync(playerID, playerStatID, Logger);
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
                if (playerController.IsValid && !playerController.IsBot && playerRounds != null) {
                    if (playerRounds.ContainsKey(playerController.SteamID)) {
                        playerRounds[playerController.SteamID] += 1;
                    }
                    else {
                        playerRounds[playerController.SteamID] = 1;
                    }
                    Logger.LogInformation($"Added round to Player {playerController.SteamID} {playerController.PlayerName}. Count: {playerRounds[playerController.SteamID]}");
                }
            }
            return HookResult.Continue;
        }

    }
}
