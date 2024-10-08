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

            startingPlayers = new Dictionary<ulong, Player>();

            List<CCSPlayerController> playerControllers = Utilities.GetPlayers();
            foreach (var playerController in playerControllers) {
                if (playerController.IsValid && playerController.ActionTrackingServices != null && !playerController.IsBot && (playerController.Team == CsTeam.CounterTerrorist || playerController.Team == CsTeam.Terrorist)) {
                    startingPlayers[playerController.SteamID] = new Player(playerController.Team);
                }
            }

            startingPlayers = await steamAPIClient.GetSteamSummariesAsync(startingPlayers);

            matchInProgress = true;
            Logger.LogInformation("Match started.");
        }
    }
}
