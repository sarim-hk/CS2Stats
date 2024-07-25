using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities;
using Microsoft.Extensions.Logging;
using Serilog.Core;

namespace CS2Stats {

    public partial class CS2Stats {

        public HookResult EventCsWinPanelMatchHandler(EventCsWinPanelMatch @event, GameEventInfo info) {
            // https://docs.cssharp.dev/examples/WithDatabase.html
            // this is cracked as fuck

            if (!matchInProgress) {
                Logger.LogInformation($"EventCsWinPanelMatch but matchInProgress = {matchInProgress}. Ignoring...");
                return HookResult.Continue;
            }

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
                            if (database != null) {
                                var insertPlayerTask = database.InsertPlayerAsync(playerID, username, Logger);
                                insertPlayerTask.GetAwaiter().GetResult();

                                var insertPlayerStatTask = database.InsertPlayerStatAsync(playerID, kills, headshots, assists, deaths, damage, utilityDamage, roundsPlayed, Logger);
                                insertPlayerStatTask.GetAwaiter().GetResult();
                            }
                        }
                        catch (Exception ex) {
                            Logger.LogError(ex, "Error handling player data.");
                        }
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
