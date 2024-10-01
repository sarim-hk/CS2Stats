using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;
using Mysqlx.Session;
using System.Transactions;

namespace CS2Stats
{

    public partial class CS2Stats {

        public HookResult EventRoundAnnounceLastRoundHalfHandler(EventRoundAnnounceLastRoundHalf @event, GameEventInfo info) {
            if (this.database == null || this.database.conn == null || this.database.transaction == null || this.match == null) {
                Logger.LogInformation("[EventRoundAnnounceLastRoundHalfHandler] Database conn/transaction or match is null. Returning.");
                return HookResult.Continue;
            }

            match.TeamsNeedSwapping = true;
            Logger.LogInformation("[EventRoundAnnounceLastRoundHalfHandler] Setting teamsNeedSwapping to true.");

            return HookResult.Continue;
        }

         public HookResult EventRoundStartHandler(EventRoundStart @event, GameEventInfo info) {
            if (this.database == null || this.database.conn == null || this.database.transaction == null || this.match == null) {
                Logger.LogInformation("[EventRoundStartHandler] Database conn/transaction or match is null. Returning.");
                return HookResult.Continue;
            }

            this.SwapTeamsIfNeeded();

            LiveData liveData = GetLiveMatchData();

            Task.Run(async () => {
                await this.database.InsertLive(liveData, Logger);
                match.RoundID = await this.database.BeginInsertRound(match.MatchID, Logger);
            });

            return HookResult.Continue;
        }

        public HookResult EventCsWinPanelMatchHandler(EventCsWinPanelMatch @event, GameEventInfo info) {
            if (this.database == null || this.database.conn == null || this.database.transaction == null || this.match == null) {
                Logger.LogInformation("[EventCsWinPanelMatchHandler] Database conn/transaction or match is null. Returning.");
                return HookResult.Continue;
            }

            LiveData liveData = new LiveData(null, null, null, null, null, null);
            List<ulong> startingPlayerIDs = match.StartingPlayers.Values
                .SelectMany(team => team.PlayerIDs)
                .ToList();
            
            Task.Run(async () => {
                
                try {
                    (string? winningTeamID, string? losingTeamID, int? winningTeamScore, int? losingTeamScore, int? winningTeamNum) = this.GetMatchWinner();

                    if (winningTeamID != null && losingTeamID != null) {
                        int? winningTeamAverageELO = await this.database.GetTeamAverageELO(winningTeamID, Logger);
                        int? losingTeamAverageELO = await this.database.GetTeamAverageELO(losingTeamID, Logger);

                        if (winningTeamAverageELO != null && losingTeamAverageELO != null) {
                            double expectedWin = 1 / (1 + Math.Pow(10, (double)(losingTeamAverageELO - winningTeamAverageELO) / 400));
                            int deltaELO = (int)Math.Round(50 * (1 - expectedWin));

                            await this.database.FinishInsertMatch(match.MatchID, winningTeamID, losingTeamID, winningTeamScore, losingTeamScore, winningTeamNum, deltaELO, Logger);
                            await this.database.UpdateTeamELO(winningTeamID, deltaELO, true, Logger);
                            await this.database.UpdateTeamELO(losingTeamID, deltaELO, false, Logger);

                        }
                    }

                    await this.database.IncrementMultiplePlayerMatchesPlayed(startingPlayerIDs, Logger);
                    await this.database.InsertBatchedHurtEvents(match.HurtEvents, Logger);
                    await this.database.InsertBatchedDeathEvents(match.DeathEvents, Logger);
                    await this.database.InsertLive(liveData, Logger);
                    await this.database.CommitTransaction();
                    match = null;
                }

                catch (Exception ex) {
                    Logger.LogError(ex, "[EventCsWinPanelMatchHandler] Error occurred while finishing up the match.");
                }

            });

            Logger.LogInformation("[EventCsWinPanelMatchHandler] Match ended.");
            return HookResult.Continue;
        }

        public HookResult EventPlayerHurtHandler(EventPlayerHurt @event, GameEventInfo info) {
            if (this.database == null || this.database.conn == null || this.database.transaction == null || this.match == null) {
                Logger.LogInformation("[EventPlayerHurtHandler] Database conn/transaction or match is null. Returning.");
                return HookResult.Continue;
            }

            if (@event.Attacker != null && @event.Userid != null) {
                HurtEvent hurtEvent = new HurtEvent(@event.Attacker.SteamID, @event.Userid.SteamID,
                    @event.DmgHealth, @event.Weapon, @event.Hitgroup
                );
                
                match.HurtEvents.Add(hurtEvent);
            }

            return HookResult.Continue;
        }

        public HookResult EventPlayerDeathHandler(EventPlayerDeath @event, GameEventInfo info) {
            if (this.database == null || this.database.conn == null || this.database.transaction == null || this.match == null) {
                Logger.LogInformation("[EventPlayerDeathHandler] Database conn/transaction or match is null. Returning.");
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

            LiveData liveData = GetLiveMatchData();
            Task.Run(() => this.database.InsertLive(liveData, Logger));

            return HookResult.Continue;
        }

        public HookResult EventRoundEndHandler(EventRoundEnd @event, GameEventInfo info) {
            if (this.database == null || this.database.conn == null || this.database.transaction == null || this.match == null) {
                Logger.LogInformation("[EventPlayerDeathHandler] database conn/transaction or match is null. Returning.");
                return HookResult.Continue;
            }

            int winningTeamNum = @event.Winner;
            int winningReason = @event.Reason;
            int losingTeamNum = (winningTeamNum == (int)CsTeam.Terrorist) ? (int)CsTeam.CounterTerrorist : (int)CsTeam.Terrorist;
            string? winningTeamID = GetTeamIDByTeamNum(winningTeamNum);
            string? losingTeamID = GetTeamIDByTeamNum(losingTeamNum);

            List<ulong> playerIDs = Utilities.GetPlayers()
                .Where(playerController =>
                    playerController.Team == CsTeam.Terrorist ||
                    playerController.Team == CsTeam.CounterTerrorist)
                .Select(playerController => playerController.SteamID)
                .ToList();

            Task.Run(async () => {
                await this.database.IncrementMultiplePlayerRoundsPlayed(playerIDs, Logger);

                if (winningTeamID != null && losingTeamID != null) {
                    await this.database.FinishInsertRound(match.RoundID, winningTeamID, losingTeamID, winningTeamNum, winningReason, Logger);
                }

                else {
                    Logger.LogInformation($"[EventRoundEndHandler] Could not find both team IDs. Winning Team ID: {winningTeamID}, Losing Team ID: {losingTeamID}");
                }

            });
            
            return HookResult.Continue;
        }

        public void OnClientAuthorizedHandler(int playerSlot, SteamID playerID) {
            if (this.database == null || this.database.conn == null || this.steamAPIClient == null) {
                Logger.LogInformation("[OnClientAuthorizedHandler] Database conn/transaction or match is null. Returning..");
                return;
            }

            Task.Run(async () => {
                PlayerInfo? playerInfo = await this.steamAPIClient.GetSteamSummaryAsync(playerID.SteamId64);
                if (playerInfo == null) {
                    Logger.LogInformation("[OnClientAuthorizedHandler] Steam API PlayerInfo is null.");
                    return;
                }

                await this.database.StartTransaction();
                try {
                    await this.database.InsertPlayerInfo(playerInfo, Logger);
                    await this.database.CommitTransaction();
                }
                catch (Exception ex) {
                    Logger.LogError(ex, "[OnClientAuthorizedHandler] Error occurred while inserting player.");
                }
            });
        }


    }
}
