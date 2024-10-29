namespace CS2Stats {

    public class Round {

        public int? RoundID;
        public int StartTick;
        public int? EndTick;
        public bool OpeningDeathOccurred;
        public HashSet<ulong> PlayersParticipated;
        public HashSet<ulong> KASTEvents;
        public HashSet<HurtEvent> HurtEvents;
        public HashSet<DeathEvent> DeathEvents;
        public HashSet<BlindEvent> BlindEvents;

        public string? WinningTeamID;
        public string? LosingTeamID;
        public int? WinningTeamNum;
        public int? LosingTeamNum;
        public int? WinningReason;

        public Round() {
            this.OpeningDeathOccurred = false;
            this.PlayersParticipated = [];
            this.KASTEvents = [];
            this.HurtEvents = [];
            this.DeathEvents = [];
            this.BlindEvents = [];
        }
    }

    public struct HurtEvent {
        public ulong? AttackerID;
        public ulong VictimID;
        public int DamageAmount;
        public string Weapon;
        public int Hitgroup;
        public int RoundTick;

    }

    public struct DeathEvent {
        public ulong? AttackerID;
        public ulong? AssisterID;
        public ulong VictimID;
        public string Weapon;
        public int Hitgroup;
        public bool OpeningDeath;
        public int RoundTick;
    }

    public struct BlindEvent {
        public ulong ThrowerID;
        public ulong BlindedID;
        public float Duration;
        public bool TeamFlash;
        public int RoundTick;
    }

}
