using System.Diagnostics.Eventing.Reader;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace CS2Stats {

    public partial class CS2Stats {

        public HookResult EventRoundAnnounceLastRoundHalfHandler(EventRoundAnnounceLastRoundHalf @event, GameEventInfo info) {
            if (this.Match == null) {
                Logger.LogInformation($"[EventRoundAnnounceLastRoundHalfHandler] Match is null. Returning.");
                return HookResult.Continue;
            }

            this.Match.TeamsNeedSwapping = true;
            Logger.LogInformation("[EventRoundAnnounceLastRoundHalfHandler] Setting teamsNeedSwapping to true.");

            return HookResult.Continue;
        }

        public HookResult EventRoundStartHandler(EventRoundStart @event, GameEventInfo info) {
            if (this.Match == null) {
                Logger.LogInformation($"[EventRoundStartHandler] Match is null. Returning.");
                return HookResult.Continue;
            }

            this.SwapTeamsIfNeeded();

            // LiveData liveData = GetLiveMatchData();

            return HookResult.Continue;
        }

        public HookResult EventRoundFreezeEndHandler(EventRoundFreezeEnd @event, GameEventInfo info) {
            if (this.Match == null) {
                Logger.LogInformation($"[EventRoundFreezeEndHandler] Match is null. Returning.");
                return HookResult.Continue;
            }

            this.Match.Round = new Round(
                startTick: Server.TickCount
            );

            return HookResult.Continue;
        }

        public HookResult EventRoundEndHandler(EventRoundEnd @event, GameEventInfo info) {
            if (this.Match == null || this.Match.Round == null) {
                Logger.LogInformation($"[EventRoundEndHandler] Match: {this.Match != null}, Round: {!(this.Match == null || (this.Match != null && this.Match.Round == null))}, Returning.");
                return HookResult.Continue;
            }

            this.Match.Round.WinningReason = @event.Reason;
            this.Match.Round.WinningTeamNum = @event.Winner;
            this.Match.Round.LosingTeamNum = @event.Winner == 2 ? 3 : 2;
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
                .Select(playerController => new PlayerParticipated {
                    PlayerID = playerController.SteamID,
                    PlayerSide = playerController.TeamNum
                }).ToList();

            if (this.Match.Round.ClutchEvent != null) {
                if (@event.Winner == this.Match.Round.ClutchEvent.ClutcherSide) {
                    this.Match.Round.ClutchEvent.Result = "Win";
                }

                else {
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
                        if (@event.Winner == enemyTeamNum) {
                            this.Match.Round.DuelEvent = new() {
                                WinnerID = enemiesAlive.First().SteamID,
                                WinnerSide = enemiesAlive.First().TeamNum,
                                LoserID = this.Match.Round.ClutchEvent.ClutcherID,
                                LoserSide = this.Match.Round.ClutchEvent.ClutcherSide
                            };
                        }

                    }
                }
            }

            this.Match.Rounds.Enqueue(this.Match.Round);
            this.Match.Round = null;

            return HookResult.Continue;
        }

        public HookResult EventCsWinPanelMatchHandler(EventCsWinPanelMatch @event, GameEventInfo info) {
            if (this.Match == null) {
                Logger.LogInformation("[EventCsWinPanelMatchHandler] Match is null. Returning.");
                return HookResult.Continue;
            }

            string gameDirectory = Server.GameDirectory;
            this.Match.EndTick = Server.TickCount;

            var teams = this.Match.Teams.ToList();
            var comparison = teams[0].Value.Score.CompareTo(teams[1].Value.Score);

            teams[0].Value.Result = comparison switch {
                > 0 => "Win",
                < 0 => "Loss",
                _ => "Tie"
            };

            teams[1].Value.Result = comparison switch {
                > 0 => "Loss",
                < 0 => "Win",
                _ => "Tie"
            };

            Match match = this.Match;

            Task.Run(async () => {
                try {
                    Logger.LogInformation("[EventCsWinPanelMatchHandler] Serializing match.");

                    string jsonifiedMatch = JsonConvert.SerializeObject(match, Formatting.Indented);

                    var logsDirectory = Path.Combine(gameDirectory, "csgo", "CS2Stats", "logs");
                    var filePath = Path.Combine(logsDirectory, $"{match.MatchName}.json");

                    Logger.LogInformation("[EventCsWinPanelMatchHandler] Creating directory: {Directory}", logsDirectory);
                    Directory.CreateDirectory(logsDirectory);

                    Logger.LogInformation("[EventCsWinPanelMatchHandler] Writing file: {FilePath}", filePath);
                    await File.WriteAllTextAsync(filePath, jsonifiedMatch);

                    Logger.LogInformation(
                        "[EventCsWinPanelMatchHandler] File written. Exists: {Exists}",
                        File.Exists(filePath)
                    );

                    Logger.LogInformation("[EventCsWinPanelMatchHandler] Uploading match JSON.");
                    await this.APIClient.UploadMatchJSONAsync(jsonifiedMatch);

                    Logger.LogInformation("[EventCsWinPanelMatchHandler] Upload complete.");
                }

                catch (Exception ex) {
                    Logger.LogError(ex, "[EventCsWinPanelMatchHandler] Error occurred while finishing up the match.");
                }

                finally {
                    Logger.LogInformation("[EventCsWinPanelMatchHandler] Cleaning up match.");

                    Server.NextFrame(() => this.StopDemo(Logger));
                    this.Match = null;
                }
            });

            Logger.LogInformation("[EventCsWinPanelMatchHandler] Match ended.");
            return HookResult.Continue;
        }

        public HookResult EventPlayerHurtHandler(EventPlayerHurt @event, GameEventInfo info) {
            if (this.Match == null || this.Match.Round == null) {
                Logger.LogInformation($"[EventPlayerHurtHandler] Match: {this.Match != null}, Round: {!(this.Match == null || (this.Match != null && this.Match.Round == null))}, Returning.");
                return HookResult.Continue;
            }

            if (@event.Userid != null) {

                if (@event.Attacker != null && @event.Attacker.TeamNum == @event.Userid.TeamNum) {
                    Logger.LogInformation("[EventPlayerHurtHandler] PlayerHurt is team damage. Returning.");
                    return HookResult.Continue;
                }

                this.Match.Round.HurtEvents.Add(new HurtEvent() {
                    AttackerID = @event.Attacker?.SteamID,
                    AttackerSide = @event.Attacker?.TeamNum,
                    VictimID = @event.Userid.SteamID,
                    VictimSide = @event.Userid.TeamNum,
                    Damage = Math.Clamp(@event.DmgHealth, 1, 100),
                    Weapon = @event.Weapon,
                    Hitgroup = @event.Hitgroup,
                    RoundTick = Server.TickCount - this.Match.Round.StartTick
                });
            }

            return HookResult.Continue;
        }

        public HookResult EventPlayerDeathHandler(EventPlayerDeath @event, GameEventInfo info) {
            if (this.Match == null || this.Match.Round == null) {
                Logger.LogInformation($"[EventPlayerDeathHandler] Match: {this.Match != null}, Round: {!(this.Match == null || (this.Match != null && this.Match.Round == null))}, Returning.");
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

                if (@event.Attacker != null && @event.Attacker.TeamNum == @event.Userid.TeamNum) {
                    Logger.LogInformation("[EventPlayerDeathHandler] PlayerDeath is team kill. Not storing it.");
                }

                else {
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
                }

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
                    if (!playerController.IsBot && playerController.IsValid && (playerController.Team == CsTeam.Terrorist || playerController.Team == CsTeam.CounterTerrorist)) {
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
                        this.Match.Round.ClutchEvent = new() {
                            ClutcherID = tsAlive.First().SteamID,
                            ClutcherSide = (int)CsTeam.Terrorist,
                            EnemyCount = ctsAlive.Count
                        };
                    }

                    else if (ctsAlive.Count == 1) {
                        this.Match.Round.ClutchEvent = new() {
                            ClutcherID = ctsAlive.First().SteamID,
                            ClutcherSide = (int)CsTeam.CounterTerrorist,
                            EnemyCount = tsAlive.Count
                        };
                    }
                }

            }

            this.Match.Round.OpeningDeathOccurred = true;

            // LiveData liveData = GetLiveMatchData();

            return HookResult.Continue;
        }

        public HookResult EventPlayerBlindHandler(EventPlayerBlind @event, GameEventInfo info) {
            if (this.Match == null || this.Match.Round == null) {
                Logger.LogInformation($"[EventPlayerBlindHandler] Match: {this.Match != null}, Round: {!(this.Match == null || (this.Match != null && this.Match.Round == null))}, Returning.");
                return HookResult.Continue;
            }

            if (@event.Attacker != null && @event.Userid != null) {

                if (@event.Attacker.TeamNum == @event.Userid.TeamNum) {
                    Logger.LogInformation("[EventPlayerBlindHandler] PlayerBlind is team flash. Returning.");
                    return HookResult.Continue;
                }

                this.Match.Round.BlindEvents.Add(new BlindEvent() {
                    ThrowerID = @event.Attacker.SteamID,
                    ThrowerSide = @event.Attacker.TeamNum,
                    BlindedID = @event.Userid.SteamID,
                    BlindedSide = @event.Userid.TeamNum,
                    Duration = @event.BlindDuration,
                    RoundTick = Server.TickCount - this.Match.Round.StartTick
                });
            }

            return HookResult.Continue;
        }

        public HookResult EventGrenadeThrownHandler(EventGrenadeThrown @event, GameEventInfo info) {
            if (this.Match == null || this.Match.Round == null) {
                Logger.LogInformation($"[EventGrenadeThrownHandler] Match: {this.Match != null}, Round: {!(this.Match == null || (this.Match != null && this.Match.Round == null))}, Returning.");
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

        public HookResult EventBombPlantedHandler(EventBombPlanted @event, GameEventInfo info) {
            if (this.Match == null || this.Match.Round == null) {
                Logger.LogInformation($"[EventBombPlantedHandler] Match: {this.Match != null}, Round: {!(this.Match == null || (this.Match != null && this.Match.Round == null))}, Returning.");
                return HookResult.Continue;
            }

            // LiveData liveData = GetLiveMatchData();

            return HookResult.Continue;
        }

        public HookResult EventBombDefusedHandler(EventBombDefused @event, GameEventInfo info) {
            if (this.Match == null || this.Match.Round == null) {
                Logger.LogInformation($"[EventBombDefusedHandler] Match: {this.Match != null}, Round: {!(this.Match == null || (this.Match != null && this.Match.Round == null))}, Returning.");
                return HookResult.Continue;
            }

            // LiveData liveData = GetLiveMatchData();

            return HookResult.Continue;
        }

        public void OnClientAuthorizedHandler(int playerSlot, SteamID playerID) {
            if (this.APIClient == null) {
                Logger.LogInformation($"[OnClientAuthorizedHandler] CS2StatsAPIClient is null, Returning.");
                return;
            }

            Task.Run(async () => {
                // send player id to api so it can store it
            });
        }

    }
}
