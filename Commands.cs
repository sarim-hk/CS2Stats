using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;

namespace CS2Stats {
    public partial class CS2Stats {

        [ConsoleCommand("cs2s_start_match", "Start a match.")]
        public void StartMatch(CCSPlayerController? player, CommandInfo info) {
            if (player != null) {
                return;
            }

            if (this.Database == null) {
                Logger.LogInformation("[StartMatch] Database is null. Returning.");
                return;
            }

            HashSet<ulong> teamNum2 = [];
            HashSet<ulong> teamNum3 = [];

            foreach (CCSPlayerController playerController in Utilities.GetPlayers()) {
                if (playerController.IsValid && !playerController.IsBot) {
                    if (playerController.TeamNum == (int)CsTeam.Terrorist) {
                        teamNum2.Add(playerController.SteamID);
                    }
                    else if (playerController.TeamNum == (int)CsTeam.CounterTerrorist) {
                        teamNum3.Add(playerController.SteamID);
                    }
                }
            }

            string teamNum2ID = GenerateTeamID(teamNum2, Logger);
            string teamNum3ID = GenerateTeamID(teamNum3, Logger);

            Dictionary<string, TeamInfo> startingPlayers = [];
            startingPlayers[teamNum2ID] = new TeamInfo(teamNum2ID, (int)CsTeam.Terrorist, teamNum2);
            startingPlayers[teamNum3ID] = new TeamInfo(teamNum3ID, (int)CsTeam.CounterTerrorist, teamNum3);

            string mapName = Server.MapName;
            int startTick = Server.TickCount;

            Task.Run(async () => {
                this.Match = new Match(
                    matchID: await this.Database.GetLastMatchID(Logger) + 1,
                    roundID: await this.Database.GetLastRoundID(Logger),
                    mapName: mapName,
                    startTick: startTick,
                    startingPlayers: startingPlayers
                );

            });

            if (this.Config.DemoRecordingEnabled == "1") {
                Server.NextFrame(() => this.StartDemo(Logger));
            }

            Logger.LogInformation("[StartMatch] Match started.");

        }

        [ConsoleCommand("cs2s_cancel_match", "Cancel a match without saving.")]
        public void CancelMatch(CCSPlayerController? player, CommandInfo info) {
            if (player != null) {
                return;
            }

            if (this.Database == null) {
                Logger.LogInformation("[CancelMatch] Database is null. Returning.");
                return;
            }

            if (this.Config.DemoRecordingEnabled == "1") {
                this.StopDemo(Logger);
            }

            Task.Run(async () => {
                LiveData liveData = new();
                await this.Database.InsertLive(liveData, Logger);
            });

            this.Match = null;

            Logger.LogInformation("[CancelMatch] Match cancelled.");
        }

    }
}

