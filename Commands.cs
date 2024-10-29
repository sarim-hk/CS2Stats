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

            this.Match = new Match() {
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
            
            Task.Run(async () => {
                this.Match.MatchID = await this.Database.GetLastMatchID(Logger) + 1;
                this.Match.RoundID = await this.Database.GetLastRoundID(Logger);
            });

            string teamNum2ID = GenerateTeamID(teamNum2, Logger);
            string teamNum3ID = GenerateTeamID(teamNum3, Logger);
            this.Match.StartingPlayers[teamNum2ID] = new TeamInfo(teamNum2ID, (int)CsTeam.Terrorist, teamNum2);
            this.Match.StartingPlayers[teamNum3ID] = new TeamInfo(teamNum3ID, (int)CsTeam.CounterTerrorist, teamNum3);



            Logger.LogInformation("[StartMatch] Match started.");

        }
    }
}

