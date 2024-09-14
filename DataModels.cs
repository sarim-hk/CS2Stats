using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace CS2Stats
{

    public class PlayerInfo
    {
        public ulong PlayerID;
        public string? Username;
        public string? AvatarS;
        public string? AvatarM;
        public string? AvatarL;

    }

    public class TeamInfo {
        public int Side;
        public List<ulong> PlayerIDs;

        public TeamInfo(int side, List<ulong> playerIDs) {
            this.Side = side;
            this.PlayerIDs = playerIDs;
        }

        public void SwapSides() {
            if (this.Side == 2) {
                this.Side = 3;
            }
            else if (this.Side == 3) {
                this.Side = 2;
            }
        }
    }

    public class LivePlayer {
        public int Kills;
        public int Assists;
        public int Deaths;
        public int Damage;
        public int Health;
        public int MoneySaved;

        public LivePlayer(int kills, int assists, int deaths, int damage, int health, int moneySaved) {
            this.Kills = kills;
            this.Assists = assists;
            this.Deaths = deaths;
            this.Damage = damage;
            this.Health = health;
            this.MoneySaved = moneySaved;
        }

    }

}
