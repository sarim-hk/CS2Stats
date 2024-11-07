﻿using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace CS2Stats {

    public partial class CS2Stats {

        public HookResult EventRoundAnnounceLastRoundHalfHandler(EventRoundAnnounceLastRoundHalf @event, GameEventInfo info) {
            if (this.Match == null || this.Match.Round == null || this.Database == null) {
                Logger.LogInformation("[EventRoundAnnounceLastRoundHalfHandler] Match, round, or database is null. Returning.");
                return HookResult.Continue;
            }

            this.Match.TeamsNeedSwapping = true;
            Logger.LogInformation("[EventRoundAnnounceLastRoundHalfHandler] Setting teamsNeedSwapping to true.");

            return HookResult.Continue;
        }

        public HookResult EventRoundFreezeEndHandler(EventRoundFreezeEnd @event, GameEventInfo info) {
            if (this.Match == null || this.Database == null) {
                Logger.LogInformation("[EventRoundFreezeEndHandler] Match or database is null. Returning.");
                return HookResult.Continue;
            }

            this.SwapTeamsIfNeeded();
            LiveData liveData = GetLiveMatchData();

            this.Match.RoundID += 1;
            this.Match.Round = new Round(
                roundID: this.Match.RoundID,
                startTick: Server.TickCount
            );

            Task.Run(async () => {
                await this.Database.InsertLive(liveData, Logger);
            });

            return HookResult.Continue;
        }

        public HookResult EventCsWinPanelMatchHandler(EventCsWinPanelMatch @event, GameEventInfo info) {
            if (this.Match == null || this.Match.Round == null || this.Database == null) {
                Logger.LogInformation("[EventCsWinPanelMatchHandler] Match, round, or database is null. Returning.");
                return HookResult.Continue;
            }

            this.Match.EndTick = Server.TickCount;

            LiveData liveData = new();
            List<ulong> startingPlayerIDs = this.Match.StartingPlayers.Values
                .SelectMany(team => team.PlayerIDs)
                .ToList();

            Task.Run(async () => {

                try {
                    await this.Database.CreateConnection();
                    await this.Database.StartTransaction();

                    await this.Database.InsertMap(this.Match, Logger);
                    await this.Database.InsertMatch(this.Match, Logger);

                    await this.Database.InsertTeamsAndTeamPlayers(this.Match, Logger);

                    while (this.Match.Rounds.Count > 0) {
                        Round round = this.Match.Rounds.Dequeue();

                        await this.Database.InsertRound(this.Match, round, Logger);
                        await this.Database.IncrementPlayerValues(round.KASTEvents.Select(kastEvent => kastEvent.PlayerID).ToList(), "RoundsKAST", Logger);
                        await this.Database.IncrementPlayerValues(round.PlayersParticipated, "RoundsPlayed", Logger);

                        await this.Database.InsertClutchEvent(this.Match, round, Logger);
                        await this.Database.InsertDuelEvent(this.Match, round, Logger);
                        await this.Database.InsertBatchedHurtEvents(this.Match, round, Logger);
                        await this.Database.InsertBatchedDeathEvents(this.Match, round, Logger);
                        await this.Database.InsertBatchedBlindEvents(this.Match, round, Logger);
                        await this.Database.InsertBatchedGrenadeEvents(this.Match, round, Logger);
                        await this.Database.InsertBatchedKAST(this.Match, round, Logger);
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
                    if (this.Config.DemoRecordingEnabled == "1") {
                        Server.NextFrame(() => this.StopDemo(Logger));
                    }

                    this.Match = null;
                }

            });

            Logger.LogInformation("[EventCsWinPanelMatchHandler] Match ended.");
            return HookResult.Continue;
        }

        public HookResult EventPlayerHurtHandler(EventPlayerHurt @event, GameEventInfo info) {
            if (this.Match == null || this.Match.Round == null || this.Database == null) {
                Logger.LogInformation("[EventPlayerHurtHandler] Match, round, or database is null. Returning.");
                return HookResult.Continue;
            }

            if (@event.Userid != null) {
                this.Match.Round.HurtEvents.Add(new HurtEvent() {
                    AttackerID = @event.Attacker?.SteamID,
                    AttackerSide = @event.Attacker?.TeamNum,
                    VictimID = @event.Userid.SteamID,
                    VictimSide = @event.Userid.TeamNum,
                    DamageAmount = Math.Clamp(@event.DmgHealth, 1, 100),
                    Weapon = @event.Weapon,
                    Hitgroup = @event.Hitgroup,
                    RoundTick = Server.TickCount - this.Match.Round.StartTick
                });
            }

            return HookResult.Continue;
        }

        public HookResult EventPlayerDeathHandler(EventPlayerDeath @event, GameEventInfo info) {
            if (this.Match == null || this.Match.Round == null || this.Database == null) {
                Logger.LogInformation("[EventPlayerDeathHandler] Match, round, or database is null. Returning.");
                return HookResult.Continue;
            }

            if (@event.Userid != null) {

                if (this.Match.Round.DeathEvents.Any(deathEvent => deathEvent.AttackerID == @event.Userid.SteamID)) {
                    DeathEvent matchingDeathEvent = this.Match.Round.DeathEvents
                        .First(deathEvent => deathEvent.AttackerID == @event.Userid.SteamID);

                    if ((Server.TickCount - this.Match.Round.StartTick) < matchingDeathEvent.RoundTick + (5 * Server.TickInterval)) {
                        KASTEvent kASTEvent = new KASTEvent() {
                            PlayerID = matchingDeathEvent.VictimID,
                            PlayerSide = matchingDeathEvent.VictimSide
                        };

                        this.Match.Round.KASTEvents.Add(kASTEvent);
                    }
                }

                this.Match.Round.DeathEvents.Add(new DeathEvent() {
                    AttackerID = @event.Attacker?.SteamID,
                    AttackerSide = @event.Attacker?.TeamNum,
                    AssisterID = @event.Assister?.SteamID,
                    AssisterSide = @event.Assister?.TeamNum,
                    VictimID = @event.Userid.SteamID,
                    VictimSide = @event.Userid.TeamNum,
                    Weapon = @event.Weapon,
                    Hitgroup = @event.Hitgroup,
                    OpeningDeath = !this.Match.Round.OpeningDeathOccurred,
                    RoundTick = Server.TickCount - this.Match.Round.StartTick
                });

                if (@event.Attacker != null) {
                    KASTEvent kastEvent = new KASTEvent() {
                        PlayerID = @event.Attacker.SteamID,
                        PlayerSide = @event.Attacker.TeamNum
                    };

                    this.Match.Round.KASTEvents.Add(kastEvent);
                }

                else if (@event.Assister != null) {
                    KASTEvent kastEvent = new KASTEvent() {
                        PlayerID = @event.Assister.SteamID,
                        PlayerSide = @event.Assister.TeamNum
                    };
                    this.Match.Round.KASTEvents.Add(kastEvent);
                }
            }

            if (this.Match.Round.ClutchEvent == null) {
                HashSet<CCSPlayerController> tsAlive = [];
                HashSet<CCSPlayerController> ctsAlive = [];

                foreach (CCSPlayerController playerController in Utilities.GetPlayers()) {
                    if (playerController.IsValid && !playerController.IsBot) {
                        if (playerController.TeamNum == (int)CsTeam.Terrorist && playerController.PlayerPawn.Value?.LifeState == (byte)LifeState_t.LIFE_ALIVE) {
                            tsAlive.Add(playerController);
                        }
                        else if (playerController.TeamNum == (int)CsTeam.CounterTerrorist && playerController.PlayerPawn.Value?.LifeState == (byte)LifeState_t.LIFE_ALIVE) {
                            ctsAlive.Add(playerController);
                        }
                    }
                }

                if (tsAlive.Count != 0 && ctsAlive.Count != 0) {
                    ClutchEvent clutchEvent = new();

                    if (tsAlive.Count == 1) {
                        Server.ExecuteCommand($"say New clutch event. Clutcher: {tsAlive.First().PlayerName}, Team: T, Vs: {ctsAlive.Count}");
                        this.Match.Round.ClutchEvent = new() {
                            ClutcherID = tsAlive.First().SteamID,
                            ClutcherSide = (int)CsTeam.Terrorist,
                            EnemyCount = ctsAlive.Count
                        };
                    }

                    else if (ctsAlive.Count == 1) {
                        Server.ExecuteCommand($"say New clutch event. Clutcher: {ctsAlive.First().PlayerName}, Team: CT, Vs: {tsAlive.Count}");
                        this.Match.Round.ClutchEvent = new() {
                            ClutcherID = ctsAlive.First().SteamID,
                            ClutcherSide = (int)CsTeam.CounterTerrorist,
                            EnemyCount = tsAlive.Count
                        };
                    }
                }

            }

            this.Match.Round.OpeningDeathOccurred = true;

            LiveData liveData = GetLiveMatchData();
            Task.Run(async () => await this.Database.InsertLive(liveData, Logger));

            return HookResult.Continue;
        }

        public HookResult EventPlayerBlindHandler(EventPlayerBlind @event, GameEventInfo info) {
            if (this.Match == null || this.Match.Round == null || this.Database == null) {
                Logger.LogInformation("[EventPlayerBlindHandler] Match, round, or database is null. Returning.");
                return HookResult.Continue;
            }

            if (@event.Userid != null && @event.Attacker != null) {
                this.Match.Round.BlindEvents.Add(new BlindEvent() {
                    ThrowerID = @event.Attacker.SteamID,
                    ThrowerSide = @event.Attacker.TeamNum,
                    BlindedID = @event.Userid.SteamID,
                    BlindedSide = @event.Userid.TeamNum,
                    Duration = @event.BlindDuration,
                    TeamFlash = (@event.Attacker.TeamNum == @event.Userid.TeamNum),
                    RoundTick = Server.TickCount - this.Match.Round.StartTick
                });
            }

            return HookResult.Continue;
        }

        public HookResult EventGrenadeThrownHandler(EventGrenadeThrown @event, GameEventInfo info) {
            if (this.Match == null || this.Match.Round == null || this.Database == null) {
                Logger.LogInformation("[EventPlayerBlindHandler] Match, round, or database is null. Returning.");
                return HookResult.Continue;
            }

            if (@event.Userid != null) {
                this.Match.Round.GrenadeEvents.Add(new GrenadeEvent() {
                    ThrowerID = @event.Userid.SteamID,
                    ThrowerSide = @event.Userid.TeamNum,
                    Weapon = @event.Weapon,
                    RoundTick = Server.TickCount - this.Match.Round.StartTick
                });
            }

            return HookResult.Continue;
        }

        public HookResult EventRoundEndHandler(EventRoundEnd @event, GameEventInfo info) {
            if (this.Match == null || this.Match.Round == null || this.Database == null) {
                Logger.LogInformation("[EventPlayerDeathHandler] Match, round, or database is null. Returning.");
                return HookResult.Continue;
            }

            this.Match.Round.WinningReason = @event.Reason;
            this.Match.Round.WinningTeamNum = @event.Winner;
            this.Match.Round.LosingTeamNum = (this.Match.Round.WinningTeamNum == (int)CsTeam.Terrorist) ? (int)CsTeam.CounterTerrorist : (int)CsTeam.Terrorist;
            this.Match.Round.WinningTeamID = GetTeamInfoByTeamNum(this.Match.Round.WinningTeamNum)?.TeamID;
            this.Match.Round.LosingTeamID = GetTeamInfoByTeamNum(this.Match.Round.LosingTeamNum)?.TeamID;
            this.Match.Round.EndTick = Server.TickCount;

            List<CCSPlayerController> playerControllersParticipated = Utilities.GetPlayers()
                .Where(playerController =>
                    !playerController.IsBot &&
                    (playerController.Team == CsTeam.Terrorist ||
                    playerController.Team == CsTeam.CounterTerrorist)).ToList();

            IEnumerable<KASTEvent> survivedKASTEvents = playerControllersParticipated
                .Where(playerController =>
                    playerController.PlayerPawn.Value?.LifeState == (byte)LifeState_t.LIFE_ALIVE)
                .Select(playerController => new KASTEvent {
                    PlayerID = playerController.SteamID,
                    PlayerSide = playerController.TeamNum
                });

            this.Match.Round.KASTEvents.UnionWith(survivedKASTEvents);

            this.Match.Round.PlayersParticipated = playerControllersParticipated
                .Select(playerController => playerController.SteamID)
                .ToList();

            if (this.Match.Round.ClutchEvent != null) {
                Server.ExecuteCommand($"say There was a clutch event.");
                if (@event.Winner == this.Match.Round.ClutchEvent.ClutcherSide) {
                    Server.ExecuteCommand($"say They won the clutch :)");
                    this.Match.Round.ClutchEvent.Result = "Win";
                }

                else {
                    Server.ExecuteCommand($"say They lost the clutch :(");
                    this.Match.Round.ClutchEvent.Result = "Loss";

                    int enemyTeamNum = (this.Match.Round.ClutchEvent.ClutcherSide == 2) ? 3 : 2;

                    HashSet<CCSPlayerController> enemiesAlive = [];
                    foreach (CCSPlayerController playerController in playerControllersParticipated) {
                        if (playerController.IsValid) {
                            if (playerController.TeamNum == enemyTeamNum && playerController.PlayerPawn.Value?.LifeState == (byte)LifeState_t.LIFE_ALIVE) {
                                enemiesAlive.Add(playerController);
                            }
                        }
                    }

                    if (enemiesAlive.Count == 1) {

                        Server.ExecuteCommand($"say There is only one enemy of the clutcher alive, so round ended in a 1v1.");
                        Server.ExecuteCommand($"say 1v1 winner: {enemiesAlive.First().PlayerName}");
                        if (@event.Winner == enemyTeamNum) {
                            this.Match.Round.DuelEvent = new() {
                                WinnerID = enemiesAlive.First().SteamID,
                                LoserID = this.Match.Round.ClutchEvent.ClutcherID
                            };
                        }

                    }
                }
            }

            Server.NextFrame(() => Server.ExecuteCommand($"say Round: {(this.Match.Round.RoundID - this.Match.Rounds.Peek().RoundID) + 1}"));
            this.Match.Rounds.Enqueue(this.Match.Round);

            return HookResult.Continue;
        }

        public HookResult EventBombPlantedHandler(EventBombPlanted @event, GameEventInfo info) {
            if (this.Match == null || this.Match.Round == null || this.Database == null) {
                Logger.LogInformation("[EventBombPlantedHandler] Match, round, or database is null. Returning.");
                return HookResult.Continue;
            }

            LiveData liveData = GetLiveMatchData();
            Task.Run(async () => {
                await this.Database.InsertLive(liveData, Logger);
            });

            return HookResult.Continue;
        }

        public HookResult EventBombDefusedHandler(EventBombDefused @event, GameEventInfo info) {
            if (this.Match == null || this.Match.Round == null || this.Database == null) {
                Logger.LogInformation("[EventBombDefusedHandler] Match, round, or database is null. Returning.");
                return HookResult.Continue;
            }

            LiveData liveData = GetLiveMatchData();
            Task.Run(async () => {
                await this.Database.InsertLive(liveData, Logger);
            });

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
                    playerInfo = new() {
                        Username = "Anonymous"
                    };

                    Logger.LogInformation("[OnClientAuthorizedHandler] Steam API PlayerInfo is null. Inserting a blank copy.");
                }

                await this.Database.InsertPlayerInfo(playerInfo.Value, Logger);
                await this.Database.InsertPlayer(playerID.SteamId64, Logger);

            });
        }

    }
}
