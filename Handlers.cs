﻿using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;

namespace CS2Stats {

    public partial class CS2Stats {

        public HookResult EventRoundAnnounceLastRoundHalfHandler(EventRoundAnnounceLastRoundHalf @event, GameEventInfo info) {
            if (this.Match == null || this.Database == null) {
                Logger.LogInformation("[EventRoundAnnounceLastRoundHalfHandler] Database or match is null. Returning.");
                return HookResult.Continue;
            }

            this.Match.TeamsNeedSwapping = true;
            Logger.LogInformation("[EventRoundAnnounceLastRoundHalfHandler] Setting teamsNeedSwapping to true.");

            return HookResult.Continue;
        }

        public HookResult EventRoundStartHandler(EventRoundStart @event, GameEventInfo info) {
            if (this.Match == null || this.Database == null) {
                Logger.LogInformation("[EventRoundStartHandler] Database or match is null. Returning.");
                return HookResult.Continue;
            }

            this.Match.RoundID += 1;

            this.Match.Round = new Round {
                StartTick = Server.TickCount,
                RoundID = this.Match.RoundID
            };

            this.SwapTeamsIfNeeded();

            LiveData liveData = GetLiveMatchData(this.Match.Round);

            Task.Run(async () => {
                await this.Database.InsertLive(liveData, Logger);
            });

            return HookResult.Continue;
        }

        public HookResult EventCsWinPanelMatchHandler(EventCsWinPanelMatch @event, GameEventInfo info) {
            if (this.Match == null || this.Database == null) {
                Logger.LogInformation("[EventCsWinPanelMatchHandler] Database or match is null. Returning.");
                return HookResult.Continue;
            }

            this.Match.EndTick = Server.TickCount;

            LiveData liveData = new();
            HashSet<ulong> startingPlayerIDs = this.Match.StartingPlayers.Values
                .SelectMany(team => team.PlayerIDs)
                .ToHashSet();

            Task.Run(async () => {

                try {
                    await this.Database.CreateConnection();
                    await this.Database.StartTransaction();

                    await this.Database.InsertMap(this.Match, Logger);
                    await this.Database.InsertMatch(this.Match, Logger);

                    await this.Database.InsertMultiplePlayers(startingPlayerIDs, Logger);
                    await this.Database.InsertTeamsAndTeamPlayers(this.Match, Logger);

                    while (this.Match.Rounds.Count > 0) {
                        Round round = this.Match.Rounds.Dequeue();

                        await this.Database.InsertRound(this.Match, round, Logger);
                        await this.Database.IncrementPlayerValues(round.PlayersKAST, "RoundsKAST", Logger);
                        await this.Database.IncrementPlayerValues(round.PlayersParticipated, "RoundsPlayed", Logger);

                        await this.Database.InsertBatchedHurtEvents(this.Match, round, Logger);
                        await this.Database.InsertBatchedDeathEvents(this.Match, round, Logger);
                        await this.Database.InsertBatchedKAST(this.Match, round, Logger);
                        await this.Database.InsertBatchedBlindEvents(this.Match, round, Logger);
                    }

                    TeamInfo? teamNumInfo2 = GetTeamInfoByTeamNum((int)CsTeam.Terrorist);
                    TeamInfo? teamNumInfo3 = GetTeamInfoByTeamNum((int)CsTeam.CounterTerrorist);

                    if (teamNumInfo2 != null && teamNumInfo3 != null) {
                        teamNumInfo2.Score = GetCSTeamScore((int)CsTeam.Terrorist);
                        teamNumInfo3.Score = GetCSTeamScore((int)CsTeam.CounterTerrorist);

                        teamNumInfo2.Result = teamNumInfo2.Score > teamNumInfo3.Score ? "Win" : (teamNumInfo2.Score < teamNumInfo3.Score ? "Loss" : "Tie");
                        teamNumInfo3.Result = teamNumInfo3.Score > teamNumInfo2.Score ? "Win" : (teamNumInfo3.Score < teamNumInfo2.Score ? "Loss" : "Tie");

                        teamNumInfo2.AverageELO = await this.Database.GetTeamAverageELO(teamNumInfo2.TeamID, Logger);
                        teamNumInfo3.AverageELO = await this.Database.GetTeamAverageELO(teamNumInfo3.TeamID, Logger);

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

                        await this.Database.InsertTeamResult(this.Match, teamNumInfo2, Logger);
                        await this.Database.InsertTeamResult(this.Match, teamNumInfo3, Logger);
                        await this.Database.UpdateELO(teamNumInfo2, Logger);
                        await this.Database.UpdateELO(teamNumInfo3, Logger);
                    }

                    await this.Database.IncrementPlayerValues(startingPlayerIDs, "MatchesPlayed", Logger);
                    await this.Database.InsertLive(liveData, Logger);
                    await this.Database.CommitTransaction();
                }

                catch (Exception ex) {
                    Logger.LogError(ex, "[EventCsWinPanelMatchHandler] Error occurred while finishing up the match.");
                }

                finally {
                    this.Match = null;
                }

            });

            Logger.LogInformation("[EventCsWinPanelMatchHandler] Match ended.");
            return HookResult.Continue;
        }

        public HookResult EventPlayerHurtHandler(EventPlayerHurt @event, GameEventInfo info) {
            if (this.Match == null || this.Database == null) {
                Logger.LogInformation("[EventPlayerHurtHandler] Database or match is null. Returning.");
                return HookResult.Continue;
            }

            if (@event.Userid != null) {
                this.Match.Round.HurtEvents.Add(new HurtEvent() {
                    AttackerID = @event.Attacker?.SteamID,
                    VictimID = @event.Userid.SteamID,
                    DamageAmount = Math.Clamp(@event.DmgHealth, 1, 100),
                    Weapon = @event.Weapon,
                    Hitgroup = @event.Hitgroup,
                    RoundTick = Server.TickCount - this.Match.Round.StartTick
                });
            }

            return HookResult.Continue;
        }

        public HookResult EventPlayerDeathHandler(EventPlayerDeath @event, GameEventInfo info) {
            if (this.Match == null || this.Database == null) {
                Logger.LogInformation("[EventPlayerDeathHandler] Database or match is null. Returning.");
                return HookResult.Continue;
            }

            if (@event.Userid != null) {

                if (this.Match.Round.DeathEvents.Any(deathEvent => deathEvent.AttackerID == @event.Userid.SteamID)) {
                    DeathEvent matchingDeathEvent = this.Match.Round.DeathEvents
                        .First(deathEvent => deathEvent.AttackerID == @event.Userid.SteamID);

                    if ((Server.TickCount - this.Match.Round.StartTick) < matchingDeathEvent.RoundTick + (5 * Server.TickInterval)) {
                        this.Match.Round.PlayersKAST.Add(matchingDeathEvent.VictimID);
                    }
                }

                this.Match.Round.DeathEvents.Add(new DeathEvent() {
                    AttackerID = @event.Attacker?.SteamID,
                    AssisterID = @event.Assister?.SteamID,
                    VictimID = @event.Userid.SteamID,
                    Weapon = @event.Weapon,
                    Hitgroup = @event.Hitgroup,
                    OpeningDeath = !this.Match.Round.OpeningDeathOccurred,
                    RoundTick = Server.TickCount - this.Match.Round.StartTick
                });

                if (@event.Attacker != null) {
                    this.Match.Round.PlayersKAST.Add(@event.Attacker.SteamID);
                }

                else if (@event.Assister != null) {
                    this.Match.Round.PlayersKAST.Add(@event.Assister.SteamID);
                }
            }

            this.Match.Round.OpeningDeathOccurred = true;

            LiveData liveData = GetLiveMatchData(this.Match.Round);
            Task.Run(() => this.Database.InsertLive(liveData, Logger));

            return HookResult.Continue;
        }

        public HookResult EventPlayerBlindHandler(EventPlayerBlind @event, GameEventInfo info) {
            if (this.Match == null || this.Database == null) {
                Logger.LogInformation("[EventPlayerBlindHandler] Database or match is null. Returning.");
                return HookResult.Continue;
            }

            if (@event.Userid != null && @event.Attacker != null) {
                this.Match.Round.BlindEvents.Add(new BlindEvent() {
                    ThrowerID = @event.Attacker.SteamID,
                    BlindedID = @event.Userid.SteamID,
                    Duration = @event.BlindDuration,
                    TeamFlash = (@event.Attacker.TeamNum == @event.Userid.TeamNum),
                    RoundTick = Server.TickCount - this.Match.Round.StartTick
                });
            }

            return HookResult.Continue;
        }

        public HookResult EventRoundEndHandler(EventRoundEnd @event, GameEventInfo info) {
            if (this.Match == null || this.Database == null) {
                Logger.LogInformation("[EventPlayerDeathHandler] Database or match is null. Returning.");
                return HookResult.Continue;
            }

            this.Match.Round.WinningReason = @event.Reason;
            this.Match.Round.WinningTeamNum = @event.Winner;
            this.Match.Round.LosingTeamNum = (this.Match.Round.WinningTeamNum == (int)CsTeam.Terrorist) ? (int)CsTeam.CounterTerrorist : (int)CsTeam.Terrorist;
            this.Match.Round.WinningTeamID = GetTeamInfoByTeamNum(this.Match.Round.WinningTeamNum)?.TeamID;
            this.Match.Round.LosingTeamID = GetTeamInfoByTeamNum(this.Match.Round.LosingTeamNum)?.TeamID;
            this.Match.Round.EndTick = Server.TickCount;

            this.Match.Round.PlayersParticipated = Utilities.GetPlayers()
                .Where(playerController =>
                    playerController.Team == CsTeam.Terrorist ||
                    playerController.Team == CsTeam.CounterTerrorist)
                .Select(playerController => playerController.SteamID)
                .ToHashSet();

            HashSet<ulong> alivePlayerIDs = Utilities.GetPlayers()
                .Where(playerController =>
                    (playerController.Team == CsTeam.Terrorist ||
                    playerController.Team == CsTeam.CounterTerrorist) &&
                    playerController.PlayerPawn.Value?.LifeState == (byte)LifeState_t.LIFE_ALIVE)
                .Select(playerController => playerController.SteamID)
                .ToHashSet();

            foreach (ulong playerID in alivePlayerIDs) {
                this.Match.Round.PlayersKAST.Add(playerID);
            }

            this.Match.Rounds.Enqueue(this.Match.Round);

            return HookResult.Continue;
        }

        public void OnClientAuthorizedHandler(int playerSlot, SteamID playerID) {
            if (this.Database == null || this.SteamAPIClient == null) {
                Logger.LogInformation("[OnClientAuthorizedHandler] Database transaction or match is null. Returning..");
                return;
            }

            Task.Run(async () => {
                PlayerInfo? playerInfo = await this.SteamAPIClient.GetSteamSummaryAsync(playerID.SteamId64);

                if (playerInfo == null) {
                    Logger.LogInformation("[OnClientAuthorizedHandler] Steam API PlayerInfo is null.");
                    return;
                }

                else {
                    await this.Database.InsertPlayerInfo(playerInfo, Logger);
                }

            });
        }

    }
}
