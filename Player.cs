using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace CS2Stats
{

    public class Player
    {
        public string? Username;
        public string? AvatarS;
        public string? AvatarM;
        public string? AvatarL;
        public int TeamNum;

        public Player(int teamNum)
        {
            this.TeamNum = teamNum;
        }

        public void SwapTeam() {
            if (this.TeamNum == 2) {
                this.TeamNum = 3;
            } else {
                this.TeamNum = 2;
            }
        }

    }
}
