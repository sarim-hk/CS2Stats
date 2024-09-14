using System.Collections.Generic;
using System.Threading.Tasks;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using Microsoft.Extensions.Logging;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API;

namespace CS2Stats {
    public partial class CS2Stats {

        [ConsoleCommand("cs2s_start_match", "Start a match.")]
        public void StartMatch(CCSPlayerController? player, CommandInfo command) {
            if (player != null) {
                return;
            }

            if (this.database == null || this.database.conn == null || this.steamAPIClient == null) {
                Logger.LogInformation("Database or connection or SteamAPIClient is null. Returning.");
                return;
            }

            var mapName = Server.MapName;
            startingPlayers = new Dictionary<string, List<ulong>>();

            List<ulong> team2 = new List<ulong>();
            List<ulong> team3 = new List<ulong>();
            List<CCSPlayerController> playerControllers = Utilities.GetPlayers();

            foreach (var playerController in playerControllers) {
                if (playerController.IsValid && !playerController.IsBot) {
                    if (playerController.TeamNum == 2) {
                        team2.Add(playerController.SteamID);
                    }

                    else if (playerController.TeamNum == 3) {
                        team3.Add(playerController.SteamID);
                    }
                }
            }

            teamNum2ID = GenerateTeamID(team2, Logger);
            teamNum3ID = GenerateTeamID(team3, Logger);
            startingPlayers[teamNum2ID] = team2;
            startingPlayers[teamNum3ID] = team3;

            this.database.StartTransaction();
            this.database.InsertMap(mapName, Logger).GetAwaiter().GetResult();
            this.database.InsertTeamsAndTeamPlayers(startingPlayers, Logger).GetAwaiter().GetResult();
            matchID = this.database.InsertMatch(mapName, Logger).GetAwaiter().GetResult();

            Logger.LogInformation("Match started.");

        }
    }
}

