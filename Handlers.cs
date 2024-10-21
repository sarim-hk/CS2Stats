using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;
using System.Numerics;

namespace CS2Stats
{

    public partial class CS2Stats {

        public HookResult EventRoundAnnounceLastRoundHalfHandler(EventRoundAnnounceLastRoundHalf @event, GameEventInfo info) {
            if (this.database == null || this.database.conn == null || this.database.transaction == null || this.match == null) {
                Logger.LogInformation("[EventRoundAnnounceLastRoundHalfHandler] Database conn/transaction or match is null. Returning.");
                return HookResult.Continue;
            }

            this.match.TeamsNeedSwapping = true;
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
                this.match.Round.RoundID = await this.database.BeginInsertRound(this.match, Logger);
            });

            return HookResult.Continue;
        }

        public HookResult EventCsWinPanelMatchHandler(EventCsWinPanelMatch @event, GameEventInfo info) {
            if (this.database == null || this.database.conn == null || this.database.transaction == null || this.match == null) {
                Logger.LogInformation("[EventCsWinPanelMatchHandler] Database conn/transaction or match is null. Returning.");
                return HookResult.Continue;
            }
            
            this.match.finishServerTick = Server.TickCount;

            LiveData liveData = new LiveData(null, null, null, null, null, null);
            List<ulong> startingPlayerIDs = this.match.StartingPlayers.Values
                .SelectMany(team => team.PlayerIDs)
                .ToList();
            
            Task.Run(async () => {

                try {
                    TeamInfo? teamNumInfo2 = GetTeamInfoByTeamNum((int)CsTeam.Terrorist);
                    TeamInfo? teamNumInfo3 = GetTeamInfoByTeamNum((int)CsTeam.CounterTerrorist);

                    if (teamNumInfo2 != null && teamNumInfo3 != null) {
                        teamNumInfo2.Score = GetCSTeamScore((int)CsTeam.Terrorist);
                        teamNumInfo3.Score = GetCSTeamScore((int)CsTeam.CounterTerrorist);

                        teamNumInfo2.Result = teamNumInfo2.Score > teamNumInfo3.Score ? "Win" : (teamNumInfo2.Score < teamNumInfo3.Score ? "Loss" : "Tie");
                        teamNumInfo3.Result = teamNumInfo3.Score > teamNumInfo2.Score ? "Win" : (teamNumInfo3.Score < teamNumInfo2.Score ? "Loss" : "Tie");

                        teamNumInfo2.AverageELO = await this.database.GetTeamAverageELO(teamNumInfo2.TeamID, Logger);
                        teamNumInfo3.AverageELO = await this.database.GetTeamAverageELO(teamNumInfo3.TeamID, Logger);

                        if (teamNumInfo2.Result == "Win") {
                            double expectedWin = 1 / (1 + Math.Pow(10, (double)(teamNumInfo3.AverageELO - teamNumInfo2.AverageELO) / 400));
                            teamNumInfo2.DeltaELO = (int)Math.Round(50 * (1 - expectedWin));
                            teamNumInfo3.DeltaELO = -teamNumInfo2.DeltaELO;
                        }
                        
                        else if (teamNumInfo3.Result == "Win") {
                            double expectedWin = 1 / (1 + Math.Pow(10, (double)(teamNumInfo2.AverageELO - teamNumInfo3.AverageELO) / 400));
                            teamNumInfo3.DeltaELO = (int)Math.Round(50 * (1 - expectedWin));
                            teamNumInfo2.DeltaELO = -teamNumInfo3.DeltaELO;
                        }

                        Logger.LogInformation($"Team 2 Info: {teamNumInfo2.TeamID}, Score: {teamNumInfo2.Score}, Result: {teamNumInfo2.Result}, ELO: {teamNumInfo2.AverageELO}, DeltaELO: {teamNumInfo2.DeltaELO}");
                        Logger.LogInformation($"Team 3 Info: {teamNumInfo3.TeamID}, Score: {teamNumInfo3.Score}, Result: {teamNumInfo3.Result}, ELO: {teamNumInfo3.AverageELO}, DeltaELO: {teamNumInfo3.DeltaELO}");
                        await this.database.FinishInsertTeamResult(this.match, teamNumInfo2, Logger);
                        await this.database.FinishInsertTeamResult(this.match, teamNumInfo3, Logger);
                        await this.database.UpdateELO(teamNumInfo2, Logger);
                        await this.database.UpdateELO(teamNumInfo3, Logger);
                    }

                    await this.database.FinishInsertMatch(this.match, Logger);
                    await this.database.IncrementMultiplePlayerMatchesPlayed(startingPlayerIDs, Logger);
                    await this.database.InsertLive(liveData, Logger);
                    await this.database.CommitTransaction();

                    this.match = null;
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

                this.match.Round.hurtEvents.Add(hurtEvent);
            }

            return HookResult.Continue;
        }

        public HookResult EventPlayerDeathHandler(EventPlayerDeath @event, GameEventInfo info) {
            if (this.database == null || this.database.conn == null || this.database.transaction == null || this.match == null) {
                Logger.LogInformation("[EventPlayerDeathHandler] Database conn/transaction or match is null. Returning.");
                return HookResult.Continue;
            }

            if (@event.Userid != null) {

                var matchingDeathEvent = this.match.Round.deathEvents.FirstOrDefault(deathEvent => deathEvent.AttackerID == @event.Userid.SteamID);

                if (matchingDeathEvent != null && Server.TickCount < matchingDeathEvent.ServerTick + (5 * Server.TickInterval)) {
                    this.match.Round.playersKAST.Add(matchingDeathEvent.VictimID);
                }

                this.match.Round.deathEvents.Add(new DeathEvent(
                    @event.Attacker?.SteamID,
                    @event.Assister?.SteamID,
                    @event.Userid.SteamID,
                    @event.Weapon,
                    @event.Hitgroup,
                    !this.match.Round.openingDeathOccurred,
                    Server.TickCount
                ));

                if (@event.Attacker != null) {
                    this.match.Round.playersKAST.Add(@event.Attacker.SteamID);
                }

                else if (@event.Assister != null) {
                    this.match.Round.playersKAST.Add(@event.Assister.SteamID);
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
            string? winningTeamID = GetTeamInfoByTeamNum(winningTeamNum)?.TeamID;
            string? losingTeamID = GetTeamInfoByTeamNum(losingTeamNum)?.TeamID;

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
                    playerController.PlayerPawn.Value?.LifeState == (byte)LifeState_t.LIFE_ALIVE)
                .Select(playerController => playerController.SteamID)
                .ToList();


            Task.Run(async () => {

                foreach (ulong playerID in alivePlayerIDs) {
                    this.match.Round.playersKAST.Add(playerID);
                }

                await this.database.IncrementMultiplePlayerRoundsKAST(this.match.Round.playersKAST.ToList(), Logger);
                await this.database.IncrementMultiplePlayerRoundsPlayed(playerIDs, Logger);

                if (this.match.MatchID != null && this.match.Round.RoundID != null) {
                    await this.database.InsertBatchedHurtEvents(this.match, Logger);
                    await this.database.InsertBatchedDeathEvents(this.match, Logger);
                    await this.database.InsertBatchedKAST(this.match, Logger);
                }

                else {
                    Logger.LogInformation($"[EventRoundEndHandler] Round ID is null. Could not insert hurt and death events.");
                }

                if (winningTeamID != null && losingTeamID != null) {
                    await this.database.FinishInsertRound(this.match.Round.RoundID, winningTeamID, losingTeamID, winningTeamNum, winningReason, Logger);
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
