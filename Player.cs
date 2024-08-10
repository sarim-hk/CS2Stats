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

            Username = "Anonymous";
            AvatarS = "https://avatars.steamstatic.com/b5bd56c1aa4644a474a2e4972be27ef9e82e517e.jpg";
            AvatarM = "https://avatars.steamstatic.com/b5bd56c1aa4644a474a2e4972be27ef9e82e517e_medium.jpg";
            AvatarL = "https://steamcdn-a.akamaihd.net/steamcommunity/public/images/avatars/b5/b5bd56c1aa4644a474a2e4972be27ef9e82e517e_full.jpg";

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
