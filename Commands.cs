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
                Logger.LogInformation("[StartMatch] Database or connection or SteamAPIClient is null. Returning.");
                return;
            }

            this.match = new Match();
            this.match.MapName = Server.MapName;
            this.match.serverTick = Server.TickCount;

            List<ulong> team2 = new List<ulong>();
            List<ulong> team3 = new List<ulong>();

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
            match.StartingPlayers[teamNum2ID] = new TeamInfo((int)CsTeam.Terrorist, team2);
            match.StartingPlayers[teamNum3ID] = new TeamInfo((int)CsTeam.CounterTerrorist, team3);

            List<ulong> playerIDs = match.StartingPlayers.Values
                .SelectMany(team => team.PlayerIDs)
                .ToList();

            Task.Run(async () => {
                await this.database.StartTransaction();
                await this.database.InsertMap(this.match.MapName, Logger);
                await this.database.InsertMultiplePlayers(playerIDs, Logger);
                await this.database.InsertTeamsAndTeamPlayers(match.StartingPlayers, Logger);
                match.MatchID = await this.database.BeginInsertMatch(this.match, Logger);
            });

            Logger.LogInformation("[StartMatch] Match started.");

        }
    }
}

