using CounterStrikeSharp.API.Modules.Utils;

namespace CS2Stats {

    public class TeamInfo {
        public string TeamID { get; }
        public int Side { get; set; }
        public HashSet<ulong> PlayerIDs { get; }
        public int Score { get; set; }
        public string TeamName {  get; set; }
        public string? Result { get; set; }

        public TeamInfo(string teamID, int side, HashSet<ulong> playerIDs, string teamName) {
            this.TeamID = teamID;
            this.Side = side;
            this.PlayerIDs = playerIDs;
            this.Score = 0;
            this.TeamName = teamName;
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

}
