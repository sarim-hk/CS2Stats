using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;
using System.Transactions;

namespace CS2Stats
{

    public partial class CS2Stats {

        public HookResult EventRoundAnnounceLastRoundHalfHandler(EventRoundAnnounceLastRoundHalf @event, GameEventInfo info) {
            if (this.database == null || this.database.conn == null || this.database.transaction == null || this.match == null) {
                Logger.LogInformation("EventRoundAnnounceLastRoundHalf but database conn/transaction or match is null. Returning.");
                return HookResult.Continue;
            }

            match.TeamsNeedSwapping = true;
            Logger.LogInformation("Setting teamsNeedSwapping to true.");

            return HookResult.Continue;
        }

        public HookResult EventRoundStartHandler(EventRoundStart @event, GameEventInfo info) {
            if (this.database == null || this.database.conn == null || this.database.transaction == null || this.match == null) {
                Logger.LogInformation("EventRoundStart but database conn/transaction or match is null. Returning.");
                return HookResult.Continue;
            }

            if (match.TeamsNeedSwapping) {
                if (match.StartingPlayers != null) {
                    foreach (var teamInfo in match.StartingPlayers.Keys) {
                        Logger.LogInformation($"Swapping team for player {teamInfo}.");
                        match.StartingPlayers[teamInfo].SwapSides();
                    }
                }
                match.TeamsNeedSwapping = false;
                Logger.LogInformation("Setting teamsNeedSwapping to false.");
            }

            match.RoundID = this.database.InsertRound(match.MatchID, Logger).GetAwaiter().GetResult();

            return HookResult.Continue;
        }

        public HookResult EventCsWinPanelMatchHandler(EventCsWinPanelMatch @event, GameEventInfo info) {
            if (this.database == null || this.database.conn == null || this.database.transaction == null || this.match == null) {
                Logger.LogInformation("EventCsWinPanelMatch but database conn/transaction or match is null. Returning.");
                return HookResult.Continue;
            }

            int? team2Score = GetCSTeamScore(2);
            int? team3Score = GetCSTeamScore(3);

            if (team2Score.HasValue && team3Score.HasValue) {
                if (team2Score != team3Score) {
                    int winningTeamNum = (team2Score > team3Score) ? 2 : 3;
                    int losingTeamNum = (winningTeamNum == 2) ? 3 : 2;

                    string? winningTeamID = GetTeamIDByTeamNum(winningTeamNum);
                    string? losingTeamID = GetTeamIDByTeamNum(losingTeamNum);

                    int? winningTeamScore = (winningTeamNum == 2) ? team2Score : team3Score;
                    int? losingTeamScore = (losingTeamNum == 2) ? team2Score : team3Score;

                    if (winningTeamID != null && losingTeamID != null) {
                        this.database.UpdateMatch(match.MatchID, winningTeamID, losingTeamID, winningTeamScore, losingTeamScore, winningTeamNum, 25, Logger).GetAwaiter().GetResult();
                    }
                    else {
                        Logger.LogInformation($"Could not find both team IDs. Winning Team ID: {winningTeamID}, Losing Team ID: {losingTeamID} - not updating match info");
                    }
                }

                else {
                    Logger.LogInformation("Game is a tie - not updating match info");
                }
            }
            
            this.database.InsertBatchedHurtEvents(match.HurtEvents, Logger).GetAwaiter().GetResult();
            this.database.InsertBatchedDeathEvents(match.DeathEvents, Logger).GetAwaiter().GetResult();

            match = null;

            this.database.CommitTransaction();
            Logger.LogInformation("Match ended.");
            return HookResult.Continue;
        }

        public HookResult EventPlayerHurtHandler(EventPlayerHurt @event, GameEventInfo info) {
            if (this.database == null || this.database.conn == null || this.database.transaction == null || this.match == null) {
                Logger.LogInformation("EventPlayerHurt but database conn/transaction or match is null. Returning.");
                return HookResult.Continue;
            }

            if (@event.Attacker != null && @event.Userid != null) {
                var hurtEvent = new HurtEvent(@event.Attacker.SteamID, @event.Userid.SteamID,
                    @event.DmgHealth, @event.Weapon, @event.Hitgroup
                );
                
                match.HurtEvents.Add(hurtEvent);
            }

            return HookResult.Continue;
        }

        public HookResult EventPlayerDeathHandler(EventPlayerDeath @event, GameEventInfo info) {
            if (this.database == null || this.database.conn == null || this.database.transaction == null || this.match == null) {
                Logger.LogInformation("EventPlayerDeath but database conn/transaction or match is null. Returning.");
                return HookResult.Continue;
            }

            if (@event.Userid != null) {
                match.DeathEvents.Add(new DeathEvent(
                    match.RoundID,
                    @event.Attacker?.SteamID,
                    @event.Assister?.SteamID,
                    @event.Userid.SteamID,
                    @event.Weapon,
                    @event.Hitgroup
                ));
            }

            return HookResult.Continue;
        }

        public HookResult EventRoundEndHandler(EventRoundEnd @event, GameEventInfo info) {
            if (this.database == null || this.database.conn == null || this.database.transaction == null || this.match == null) {
                Logger.LogInformation("EventRoundEnd but database conn/transaction or match is null. Returning.");
                return HookResult.Continue;
            }

            int winningTeamNum = @event.Winner;
            int losingTeamNum = (winningTeamNum == 2) ? 3 : 2;
            string? winningTeamID = GetTeamIDByTeamNum(winningTeamNum);
            string? losingTeamID = GetTeamIDByTeamNum(losingTeamNum);

            if (winningTeamID != null && losingTeamID != null) {
                this.database.UpdateRound(match.RoundID, winningTeamID, losingTeamID, @event.Winner, @event.Reason, Logger).GetAwaiter().GetResult();
            }
            else {
                Logger.LogInformation($"Could not find both team IDs. Winning Team ID: {winningTeamID}, Losing Team ID: {losingTeamID}");
            }
            return HookResult.Continue;
        }
        
        private void OnClientAuthorizedHandler(int playerSlot, SteamID playerID) {
            if (this.database == null || this.database.conn == null || this.steamAPIClient == null) {
                Logger.LogInformation("OnClientAuthorized but database conn / transaction is null. Returning.");
            }

            else {
                PlayerInfo? player = this.steamAPIClient.GetSteamSummaryAsync(playerID.SteamId64).GetAwaiter().GetResult();
                if (player == null) {
                    Logger.LogInformation("Steam API PlayerInfo is null.");
                }

                else {
                    this.database.StartTransaction();
                    this.database.InsertPlayer(player, Logger).GetAwaiter().GetResult();
                    this.database.CommitTransaction();
                }
            }
        }

        /*
        public async void InsertLiveHandler() {
            if (this.database == null || this.database.conn == null || this.database.transaction == null || this.match == null) {
                Logger.LogInformation("InsertLiveHandler but database conn/transaction or match is null. Clearing LiveTable and returning.");
                return;
            }

            List<LivePlayer> tPlayers = new List<LivePlayer>();
            List<LivePlayer> ctPlayers = new List<LivePlayer>();

            List<CCSPlayerController> playerControllers = Utilities.GetPlayers();
            foreach (var playerController in playerControllers) {
                if (playerController.ActionTrackingServices != null) {
                    var livePlayer = new LivePlayer(playerController.ActionTrackingServices.MatchStats.Kills,
                        playerController.ActionTrackingServices.MatchStats.Assists,
                        playerController.ActionTrackingServices.MatchStats.Deaths,
                        playerController.ActionTrackingServices.MatchStats.Damage,
                        playerController.Health,
                        playerController.ActionTrackingServices.MatchStats.MoneySaved);

                    if (playerController.TeamNum == 2) {
                        tPlayers.Add(livePlayer);
                    }
                    else {
                        ctPlayers.Add(livePlayer);
                    }
                }
            }

            int? tScore = GetCSTeamScore(2);
            int? ctScore = GetCSTeamScore(3);
            float roundTime = (GetGameRules().RoundStartTime + GetGameRules().RoundTime) - Server.CurrentTime;

            int bombStatus = GetGameRules().BombPlanted switch {
                true => 1,
                false => GetGameRules().BombDefused ? 2 : 0
            };

            await this.database.InsertLive(tPlayers, ctPlayers, tScore, ctScore, bombStatus, roundTime, Logger);

        }
        */

    }
}
