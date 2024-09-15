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

    public class HurtEvent {
        public ulong AttackerID;
        public ulong VictimID;
        public int DamageAmount;
        public string Weapon;
        public int Hitgroup;

        public HurtEvent(ulong attackerID, ulong victimID, int damageAmount, string weapon, int hitGroup) {
            this.AttackerID = attackerID;
            this.VictimID = victimID;
            this.DamageAmount = damageAmount;
            this.Weapon = weapon;
            this.Hitgroup = hitGroup;

        }
    }

    public class DeathEvent {
        public int? RoundID { get; set; }
        public ulong? AttackerID { get; set; }
        public ulong? AssisterID { get; set; }
        public ulong VictimID { get; set; }
        public string Weapon { get; set; }
        public int Hitgroup { get; set; }

        public DeathEvent(int? roundID, ulong? attackerID, ulong? assisterID, ulong victimID, string weapon, int hitgroup) {
            RoundID = roundID;
            AttackerID = attackerID;
            AssisterID = assisterID;
            VictimID = victimID;
            Weapon = weapon;
            Hitgroup = hitgroup;
        }
    }

}
