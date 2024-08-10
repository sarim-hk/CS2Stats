using CounterStrikeSharp.API.Modules.Utils;

namespace CS2Stats
{

    public class Player
    {
        public int Kills;
        public int Headshots;
        public int Assists;
        public int Deaths;
        public int TotalDamage;
        public int UtilityDamage;
        public int RoundsPlayed;
        public CsTeam Team;

        public string Username;
        public string AvatarS;
        public string AvatarM;
        public string AvatarL;


        public Player(CsTeam team)
        {
            Kills = 0;
            Headshots = 0;
            Assists = 0;
            Deaths = 0;
            TotalDamage = 0;
            UtilityDamage = 0;
            RoundsPlayed = 0;
            Team = team;

            Username = "";
            AvatarS = "";
            AvatarM = "";
            AvatarL = "";

        }

        public void SwapTeam()
        {
            if (Team == CsTeam.Terrorist)
            {
                Team = CsTeam.CounterTerrorist;
            }
            else
            {
                Team = CsTeam.Terrorist;
            }
        }
    }

}
