﻿using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;

namespace CS2Stats {
    public partial class CS2Stats {

        [ConsoleCommand("cs2s_start_match", "Start a match.")]
        public void StartMatch(CCSPlayerController? player, CommandInfo command) {
            if (player != null) {
                return;
            }

            if (this.database == null) {
                Logger.LogInformation("[StartMatch] Database is null. Returning.");
                return;
            }

            this.match = new Match();
            this.match.MapName = Server.MapName;
            this.match.beginServerTick = Server.TickCount;

            HashSet<ulong> team2 = new();
            HashSet<ulong> team3 = new();

            List<CCSPlayerController> playerControllers = Utilities.GetPlayers();
            foreach (CCSPlayerController playerController in playerControllers) {
                if (playerController.IsValid && !playerController.IsBot) {
                    if (playerController.TeamNum == (int)CsTeam.Terrorist) {
                        team2.Add(playerController.SteamID);
                    }
                    else if (playerController.TeamNum == (int)CsTeam.CounterTerrorist) {
                        team3.Add(playerController.SteamID);
                    }
                }
            }

            string teamNum2ID = GenerateTeamID(team2, Logger);
            string teamNum3ID = GenerateTeamID(team3, Logger);
            this.match.StartingPlayers[teamNum2ID] = new TeamInfo(teamNum2ID, (int)CsTeam.Terrorist, team2);
            this.match.StartingPlayers[teamNum3ID] = new TeamInfo(teamNum3ID, (int)CsTeam.CounterTerrorist, team3);

            HashSet<ulong> playerIDs = this.match.StartingPlayers.Values
                .SelectMany(team => team.PlayerIDs)
                .ToHashSet();

            Task.Run(async () => {
                await this.database.CreateConnection();
                await this.database.StartTransaction();
                await this.database.InsertMap(this.match.MapName, Logger);

                this.match.MatchID = await this.database.BeginInsertMatch(this.match, Logger);
                await this.database.InsertMultiplePlayers(playerIDs, Logger);
                await this.database.InsertTeamsAndTeamPlayers(this.match, Logger);
            });

            Logger.LogInformation("[StartMatch] Match started.");

        }
    }
}

