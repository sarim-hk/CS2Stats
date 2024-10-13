using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;

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

            this.match.Round = new Round();
            this.match.Round.serverTick = Server.TickCount;

            this.SwapTeamsIfNeeded();

            LiveData liveData = GetLiveMatchData();

            Task.Run(async () => {
                await this.database.InsertLive(liveData, Logger);
                match.Round.RoundID = await this.database.BeginInsertRound(match, Logger);
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
                HurtEvent hurtEvent = new HurtEvent(
                    @event.Attacker.SteamID,
                    @event.Userid.SteamID,
                    Math.Clamp(@event.DmgHealth, 1, 100),
                    @event.Weapon,
                    @event.Hitgroup,
                    Server.TickCount
                );

                match.Round.hurtEvents.Add(hurtEvent);
            }

            return HookResult.Continue;
        }

        public HookResult EventPlayerDeathHandler(EventPlayerDeath @event, GameEventInfo info) {
            if (this.database == null || this.database.conn == null || this.database.transaction == null || this.match == null) {
                Logger.LogInformation("[EventPlayerDeathHandler] Database conn/transaction or match is null. Returning.");
                return HookResult.Continue;
            }

            if (@event.Userid != null) {

                var matchingDeathEvent = match.Round.deathEvents.FirstOrDefault(deathEvent => deathEvent.AttackerID == @event.Userid.SteamID);

                if (matchingDeathEvent != null && Server.TickCount !> matchingDeathEvent.ServerTick + (5 * Server.TickInterval)) {
                    match.Round.playersKAST.Add(matchingDeathEvent.VictimID);
                }

                match.Round.deathEvents.Add(new DeathEvent(
                    @event.Attacker?.SteamID,
                    @event.Assister?.SteamID,
                    @event.Userid.SteamID,
                    @event.Weapon,
                    @event.Hitgroup,
                    !this.match.Round.openingDeathOccurred,
                    Server.TickCount
                ));

                if (@event.Attacker != null) {
                    match.Round.playersKAST.Add(@event.Attacker.SteamID);
                }

                else if (@event.Assister != null) {
                    match.Round.playersKAST.Add(@event.Assister.SteamID);
                }
            }

            this.match.Round.openingDeathOccurred = true;

            LiveData liveData = GetLiveMatchData();
            Task.Run(() => this.database.InsertLive(liveData, Logger));

            return HookResult.Continue;
        }

        public HookResult EventRoundEndHandler(EventRoundEnd @event, GameEventInfo info) {
            if (this.database == null || this.database.conn == null || this.database.transaction == null || this.match == null) {
                Logger.LogInformation("[EventPlayerDeathHandler] Database conn/transaction or match is null. Returning.");
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

            List<ulong> alivePlayerIDs = Utilities.GetPlayers()
                .Where(playerController =>
                    (playerController.Team == CsTeam.Terrorist ||
                    playerController.Team == CsTeam.CounterTerrorist) &&
                    playerController.PawnIsAlive)
                .Select(playerController => playerController.SteamID)
                .ToList();


            Task.Run(async () => {

                foreach (ulong playerID in alivePlayerIDs) {
                    this.match.Round.playersKAST.Add(playerID);
                }

                await this.database.IncrementMultiplePlayerRoundsKAST(match.Round.playersKAST.ToList(), Logger);
                await this.database.IncrementMultiplePlayerRoundsPlayed(playerIDs, Logger);

                if (match.Round.RoundID != null) {
                    await this.database.InsertBatchedHurtEvents(match.Round, Logger);
                    await this.database.InsertBatchedDeathEvents(match.Round, Logger);
                }
                else {
                    Logger.LogInformation($"[EventRoundEndHandler] Round ID is null. Could not insert hurt and death events.");
                }

                if (winningTeamID != null && losingTeamID != null) {
                    await this.database.FinishInsertRound(match.Round.RoundID, winningTeamID, losingTeamID, winningTeamNum, winningReason, Logger);
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
