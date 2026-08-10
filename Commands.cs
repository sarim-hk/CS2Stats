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
            if (player != null)
                return;

            HashSet<ulong> teamNum2 = [];
            HashSet<ulong> teamNum3 = [];

            string teamNum2Name = "Unknown";
            string teamNum3Name = "Unknown";

            foreach (var p in Utilities.GetPlayers()) {
                if (p.IsBot || !p.IsValid)
                    continue;

                switch (p.Team) {
                    case CsTeam.Terrorist:
                        teamNum2.Add(p.SteamID);
                        teamNum2Name = p.PlayerName;
                        break;

                    case CsTeam.CounterTerrorist:
                        teamNum3.Add(p.SteamID);
                        teamNum3Name = p.PlayerName;
                        break;
                }
            }

            string teamNum2ID = GenerateTeamID(teamNum2, Logger);
            string teamNum3ID = GenerateTeamID(teamNum3, Logger);

            Dictionary<string, TeamInfo> teams = [];
            teams[teamNum2ID] = new TeamInfo(teamNum2ID, (int)CsTeam.Terrorist, teamNum2, $"team_{teamNum2Name}");
            teams[teamNum3ID] = new TeamInfo(teamNum3ID, (int)CsTeam.CounterTerrorist, teamNum3, $"team_{teamNum3Name}");

            Match = new Match(
                mapName: Server.MapName,
                startTick: Server.TickCount,
                teams: teams
            );

            Server.NextFrame(() => this.StartDemo(Logger));
            Logger.LogInformation("[StartMatch] Match started.");
        }

        [ConsoleCommand("cs2s_cancel_match", "Cancel a match without saving.")]
        public void CancelMatch(CCSPlayerController? player, CommandInfo info) {
            if (player != null) {
                return;
            }

            this.StopDemo(Logger);
            this.Match = null;

            Logger.LogInformation("[CancelMatch] Match cancelled.");
        }

    }
}

