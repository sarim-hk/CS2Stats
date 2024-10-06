using CounterStrikeSharp.API.Modules.Utils;

namespace CS2Stats
{

    public class TeamInfo {
        public int Side;
        public List<ulong> PlayerIDs;

        public TeamInfo(int side, List<ulong> playerIDs) {
            this.Side = side;
            this.PlayerIDs = playerIDs;
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

        public Dictionary<string, TeamInfo> StartingPlayers;
        public Round Round;

        public Match() {
            this.StartingPlayers = new Dictionary<string, TeamInfo>();
            this.Round = new Round();
        }

    }

}
