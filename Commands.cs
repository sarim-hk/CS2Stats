using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;

namespace CS2Stats {
    public partial class CS2Stats {

        [ConsoleCommand("cs2s_start_match", "Start a match.")]
        public void StartMatch(CCSPlayerController? player) {
            if (player != null) {
                return;
            }

            if (this.database == null) {
                Logger.LogInformation("[StartMatch] Database is null. Returning.");
                return;
            }

            this.match = new Match() {
                MapName = Server.MapName,
                StartTick = Server.TickCount
            };

            HashSet<ulong> teamNum2 = [];
            HashSet<ulong> teamNum3 = [];

            List<CCSPlayerController> playerControllers = Utilities.GetPlayers();
            foreach (CCSPlayerController playerController in playerControllers) {
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
            this.match.StartingPlayers[teamNum2ID] = new TeamInfo(teamNum2ID, (int)CsTeam.Terrorist, teamNum2);
            this.match.StartingPlayers[teamNum3ID] = new TeamInfo(teamNum3ID, (int)CsTeam.CounterTerrorist, teamNum3);

            Task.Run(async () => {
                this.match.MatchID = await this.database.GetLastMatchID(Logger) + 1;
                this.match.RoundID = await this.database.GetLastRoundID(Logger);
            });

            Logger.LogInformation("[StartMatch] Match started.");

        }
    }
}

