using CounterStrikeSharp.API.Modules.Utils;

namespace CS2Stats.Structs {

    public class Player {
        public int Kills;
        public int Headshots;
        public int Assists;
        public int Deaths;
        public int TotalDamage;
        public int UtilityDamage;
        public int RoundsPlayed;
        public CsTeam Team;

        public Player(CsTeam team) {
            Kills = 0;
            Headshots = 0;
            Assists = 0;
            Deaths = 0;
            TotalDamage = 0;
            UtilityDamage = 0;
            RoundsPlayed = 0;
            Team = team;
        }

        public void SwapTeam() {
            if (this.Team == CsTeam.Terrorist) {
                this.Team = CsTeam.CounterTerrorist;
            } else {
                this.Team = CsTeam.Terrorist;
            }
        }
    }

}
