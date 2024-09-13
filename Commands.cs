﻿using System.Collections.Generic;
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
        public async void StartMatch(CCSPlayerController? player, CommandInfo command) {
            if (player != null) {
                return;
            }

            if (this.database == null || this.database.conn == null || this.steamAPIClient == null) {
                Logger.LogInformation("Database or connection or SteamAPIClient is null. Returning.");
                return;
            }

            var mapName = Server.MapName;
            startingPlayers = new Dictionary<ulong, Player>();
            this.database.StartTransaction();

            List<CCSPlayerController> playerControllers = Utilities.GetPlayers();
            foreach (var playerController in playerControllers) {
                if (playerController.IsValid && playerController.ActionTrackingServices != null && !playerController.IsBot && (playerController.TeamNum == 3 || playerController.TeamNum == 2)) {
                    startingPlayers[playerController.SteamID] = new Player(playerController.TeamNum);
                }
            }

            startingPlayers = await steamAPIClient.GetSteamSummariesAsync(startingPlayers);
            await this.database.InsertStartingPlayers(startingPlayers, Logger);
            await this.database.InsertMap(mapName, Logger);
            var (teamNum2ID, teamNum3ID) = await this.database.InsertTeamsAndTeamPlayers(startingPlayers, Logger);
            var matchID = await this.database.InsertMatch(mapName, Logger);

            Logger.LogInformation("Match started.");

        }
    }
}
