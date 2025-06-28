using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

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

            string teamNum2Name = "Unknown";
            string teamNum3Name = "Unknown";

            foreach (CCSPlayerController playerController in Utilities.GetPlayers()) {
                if (!playerController.IsBot && playerController.IsValid && (playerController.Team == CsTeam.Terrorist || playerController.Team == CsTeam.CounterTerrorist)) {
                    if (playerController.TeamNum == (int)CsTeam.Terrorist) {
                        teamNum2.Add(playerController.SteamID);
                        teamNum2Name = playerController.PlayerName;
                    }

                    else if (playerController.TeamNum == (int)CsTeam.CounterTerrorist) {
                        teamNum3.Add(playerController.SteamID);
                        teamNum3Name = playerController.PlayerName;
                    }
                }
            }

            teamNum2Name = "team_" + Regex.Replace(teamNum2Name, "[^a-zA-Z0-9]", "");
            teamNum3Name = "team_" + Regex.Replace(teamNum3Name, "[^a-zA-Z0-9]", "");

            string teamNum2ID = GenerateTeamID(teamNum2, Logger);
            string teamNum3ID = GenerateTeamID(teamNum3, Logger);

            Dictionary<string, TeamInfo> startingPlayers = [];
            startingPlayers[teamNum2ID] = new TeamInfo(teamNum2ID, (int)CsTeam.Terrorist, teamNum2, teamNum2Name);
            startingPlayers[teamNum3ID] = new TeamInfo(teamNum3ID, (int)CsTeam.CounterTerrorist, teamNum3, teamNum3Name);

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

            if (this.Match == null || this.Database == null) {
                Logger.LogInformation("[CancelMatch] Match or database is null. Returning.");
                return;
            }

            if (this.Config.DemoRecordingEnabled == "1") {
                this.StopDemo(Logger);
            }

            Task.Run(async () => {
                await this.Database.ClearLive(Logger);
            });


            this.Match = null;

            Logger.LogInformation("[CancelMatch] Match cancelled.");
        }

    }
}

