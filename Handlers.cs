using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;

namespace CS2Stats {

    public partial class CS2Stats {

        public HookResult EventRoundAnnounceLastRoundHalfHandler(EventRoundAnnounceLastRoundHalf @event, GameEventInfo info) {
            if (this.match == null || this.database == null) {
                Logger.LogInformation("[EventRoundAnnounceLastRoundHalfHandler] Database or match is null. Returning.");
                return HookResult.Continue;
            }

            this.match.TeamsNeedSwapping = true;
            Logger.LogInformation("[EventRoundAnnounceLastRoundHalfHandler] Setting teamsNeedSwapping to true.");

            return HookResult.Continue;
        }

        public HookResult EventRoundStartHandler(EventRoundStart @event, GameEventInfo info) {
            if (this.match == null || this.database == null) {
                Logger.LogInformation("[EventRoundStartHandler] Database or match is null. Returning.");
                return HookResult.Continue;
            }

            this.match.RoundID += 1;

            this.match.Round = new Round {
                StartTick = Server.TickCount,
                RoundID = this.match.RoundID
            };

            this.SwapTeamsIfNeeded();

            LiveData liveData = GetLiveMatchData(this.match.Round);

            Task.Run(async () => {
                await this.database.InsertLive(liveData, Logger);
            });

            return HookResult.Continue;
        }

        public HookResult EventCsWinPanelMatchHandler(EventCsWinPanelMatch @event, GameEventInfo info) {
            if (this.match == null || this.database == null) {
                Logger.LogInformation("[EventCsWinPanelMatchHandler] Database or match is null. Returning.");
                return HookResult.Continue;
            }

            this.match.EndTick = Server.TickCount;

            LiveData liveData = new();
            HashSet<ulong> startingPlayerIDs = this.match.StartingPlayers.Values
                .SelectMany(team => team.PlayerIDs)
                .ToHashSet();

            Task.Run(async () => {

                try {
                    await this.database.CreateConnection();
                    await this.database.StartTransaction();

                    await this.database.InsertMap(this.match, Logger);
                    await this.database.InsertMatch(this.match, Logger);

                    await this.database.InsertMultiplePlayers(startingPlayerIDs, Logger);
                    await this.database.InsertTeamsAndTeamPlayers(this.match, Logger);

                    while (this.match.Rounds.Count > 0) {
                        Round round = this.match.Rounds.Dequeue();

                        await this.database.InsertRound(this.match, round, Logger);
                        await this.database.IncrementPlayerValues(round.PlayersKAST, "RoundsKAST", Logger);
                        await this.database.IncrementPlayerValues(round.PlayersParticipated, "RoundsPlayed", Logger);

                        await this.database.InsertBatchedHurtEvents(this.match, round, Logger);
                        await this.database.InsertBatchedDeathEvents(this.match, round, Logger);
                        await this.database.InsertBatchedKAST(this.match, round, Logger);
                        await this.database.InsertBatchedBlindEvents(this.match, round, Logger);
                    }

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

                        await this.database.InsertTeamResult(this.match, teamNumInfo2, Logger);
                        await this.database.InsertTeamResult(this.match, teamNumInfo3, Logger);
                        await this.database.UpdateELO(teamNumInfo2, Logger);
                        await this.database.UpdateELO(teamNumInfo3, Logger);
                    }

                    await this.database.IncrementPlayerValues(startingPlayerIDs, "MatchesPlayed", Logger);
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
            if (this.match == null || this.database == null) {
                Logger.LogInformation("[EventPlayerHurtHandler] Database or match is null. Returning.");
                return HookResult.Continue;
            }

            if (@event.Userid != null) {
                this.match.Round.HurtEvents.Add(new HurtEvent() {
                    AttackerID = @event.Attacker?.SteamID,
                    VictimID = @event.Userid.SteamID,
                    DamageAmount = Math.Clamp(@event.DmgHealth, 1, 100),
                    Weapon = @event.Weapon,
                    Hitgroup = @event.Hitgroup,
                    RoundTick = Server.TickCount - this.match.Round.StartTick
                });
            }

            return HookResult.Continue;
        }

        public HookResult EventPlayerDeathHandler(EventPlayerDeath @event, GameEventInfo info) {
            if (this.match == null || this.database == null) {
                Logger.LogInformation("[EventPlayerDeathHandler] Database or match is null. Returning.");
                return HookResult.Continue;
            }

            if (@event.Userid != null) {

                if (this.match.Round.DeathEvents.Any(deathEvent => deathEvent.AttackerID == @event.Userid.SteamID)) {
                    DeathEvent matchingDeathEvent = this.match.Round.DeathEvents
                        .First(deathEvent => deathEvent.AttackerID == @event.Userid.SteamID);

                    if ((Server.TickCount - this.match.Round.StartTick) < matchingDeathEvent.RoundTick + (5 * Server.TickInterval)) {
                        this.match.Round.PlayersKAST.Add(matchingDeathEvent.VictimID);
                    }
                }

                this.match.Round.DeathEvents.Add(new DeathEvent() {
                    AttackerID = @event.Attacker?.SteamID,
                    AssisterID = @event.Assister?.SteamID,
                    VictimID = @event.Userid.SteamID,
                    Weapon = @event.Weapon,
                    Hitgroup = @event.Hitgroup,
                    OpeningDeath = !this.match.Round.OpeningDeathOccurred,
                    RoundTick = Server.TickCount - this.match.Round.StartTick
                });

                if (@event.Attacker != null) {
                    this.match.Round.PlayersKAST.Add(@event.Attacker.SteamID);
                }

                else if (@event.Assister != null) {
                    this.match.Round.PlayersKAST.Add(@event.Assister.SteamID);
                }
            }

            this.match.Round.OpeningDeathOccurred = true;

            LiveData liveData = GetLiveMatchData(this.match.Round);
            Task.Run(() => this.database.InsertLive(liveData, Logger));

            return HookResult.Continue;
        }

        public HookResult EventPlayerBlindHandler(EventPlayerBlind @event, GameEventInfo info) {
            if (this.match == null || this.database == null) {
                Logger.LogInformation("[EventPlayerBlindHandler] Database or match is null. Returning.");
                return HookResult.Continue;
            }

            if (@event.Userid != null && @event.Attacker != null) {
                this.match.Round.BlindEvents.Add(new BlindEvent() {
                    ThrowerID = @event.Attacker.SteamID,
                    BlindedID = @event.Userid.SteamID,
                    Duration = @event.BlindDuration,
                    TeamFlash = (@event.Attacker.TeamNum == @event.Userid.TeamNum),
                    RoundTick = Server.TickCount - this.match.Round.StartTick
                });
            }

            return HookResult.Continue;
        }

        public HookResult EventRoundEndHandler(EventRoundEnd @event, GameEventInfo info) {
            if (this.match == null || this.database == null) {
                Logger.LogInformation("[EventPlayerDeathHandler] Database or match is null. Returning.");
                return HookResult.Continue;
            }

            this.match.Round.WinningReason = @event.Reason;
            this.match.Round.WinningTeamNum = @event.Winner;
            this.match.Round.LosingTeamNum = (this.match.Round.WinningTeamNum == (int)CsTeam.Terrorist) ? (int)CsTeam.CounterTerrorist : (int)CsTeam.Terrorist;
            this.match.Round.WinningTeamID = GetTeamInfoByTeamNum(this.match.Round.WinningTeamNum)?.TeamID;
            this.match.Round.LosingTeamID = GetTeamInfoByTeamNum(this.match.Round.LosingTeamNum)?.TeamID;
            this.match.Round.EndTick = Server.TickCount;

            this.match.Round.PlayersParticipated = Utilities.GetPlayers()
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
                this.match.Round.PlayersKAST.Add(playerID);
            }

            this.match.Rounds.Enqueue(this.match.Round);

            return HookResult.Continue;
        }

        public void OnClientAuthorizedHandler(int playerSlot, SteamID playerID) {
            if (this.database == null || this.steamAPIClient == null) {
                Logger.LogInformation("[OnClientAuthorizedHandler] Database transaction or match is null. Returning..");
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
