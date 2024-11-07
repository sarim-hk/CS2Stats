namespace CS2Stats {

    public struct HurtEvent {
        public ulong? AttackerID;
        public int? AttackerSide;
        public ulong VictimID;
        public int? VictimSide;
        public int DamageAmount;
        public string Weapon;
        public int Hitgroup;
        public int RoundTick;
    }

    public struct DeathEvent {
        public ulong? AttackerID;
        public int? AttackerSide;
        public ulong? AssisterID;
        public int? AssisterSide;
        public ulong VictimID;
        public int VictimSide;
        public string Weapon;
        public int Hitgroup;
        public bool OpeningDeath;
        public int RoundTick;
    }

    public struct BlindEvent {
        public ulong ThrowerID;
        public int ThrowerSide;
        public ulong BlindedID;
        public int BlindedSide;
        public float Duration;
        public bool TeamFlash;
        public int RoundTick;
    }

    public struct GrenadeEvent {
        public ulong ThrowerID;
        public int ThrowerSide;
        public string Weapon;
        public int RoundTick;
    }

    public class KASTEvent {
        public ulong PlayerID;
        public int PlayerSide;

        public override bool Equals(object? obj) {
            if (obj is KASTEvent other) {
                return this.PlayerID == other.PlayerID;
            }
            return false;
        }

        public override int GetHashCode() {
            return HashCode.Combine(this.PlayerID);
        }

    }


    public class ClutchEvent {
        public ulong ClutcherID;
        public int ClutcherSide;
        public int EnemyCount;
        public string? Result; // Win, Loss
    }

    public struct DuelEvent {
        public ulong WinnerID;
        public int WinnerSide;
        public ulong LoserID;
        public int LoserSide;
    }

}