using CounterStrikeSharp.API.Modules.Utils;

namespace CS2Stats
{

    public class TeamInfo {
        public string TeamID;
        public int Side;
        public HashSet<ulong> PlayerIDs;

        public int? Score;
        public string? Result;
        public int? AverageELO;
        public int DeltaELO;

        public TeamInfo(string teamID, int side, HashSet<ulong> playerIDs) {
            this.TeamID = teamID;
            this.Side = side;
            this.PlayerIDs = playerIDs;
            this.DeltaELO = 0;
        }

        public void SwapSides() {
            if (this.Side == (int)CsTeam.Terrorist) {
                this.Side = (int)CsTeam.CounterTerrorist;
            }
            else if (this.Side == (int)CsTeam.CounterTerrorist) {
                this.Side = (int)CsTeam.Terrorist;
            }
        }
    }

    public class Match {
        public int? MatchID;
        public string? MapName;
        public bool TeamsNeedSwapping;
        public int? beginServerTick;
        public int? finishServerTick;

        public Dictionary<string, TeamInfo> StartingPlayers;
        public Round Round;

        public Match() {
            this.StartingPlayers = new Dictionary<string, TeamInfo>();
            this.Round = new Round();
        }

    }

}
