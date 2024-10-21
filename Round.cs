namespace CS2Stats {

    public class Round {

        public int? RoundID;
        public int StartTick;
        public int? EndTick;
        public bool OpeningDeathOccurred;
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
            this.KASTEvents = [];
            this.HurtEvents = [];
            this.DeathEvents = [];
            this.BlindEvents = [];
        }
    }

    public class HurtEvent {
        public ulong? AttackerID;
        public ulong VictimID;
        public int DamageAmount;
        public string Weapon;
        public int Hitgroup;
        public int RoundTick;

        public HurtEvent(ulong? attackerID, ulong victimID, int damageAmount, string weapon, int hitGroup, int roundTick) {
            this.AttackerID = attackerID;
            this.VictimID = victimID;
            this.DamageAmount = damageAmount;
            this.Weapon = weapon;
            this.Hitgroup = hitGroup;
            this.RoundTick = roundTick;
        }
    }

    public class DeathEvent {
        public ulong? AttackerID;
        public ulong? AssisterID;
        public ulong VictimID;
        public string Weapon;
        public int Hitgroup;
        public bool OpeningDeath;
        public int RoundTick;

        public DeathEvent(ulong? attackerID, ulong? assisterID, ulong victimID, string weapon, int hitgroup, bool openingDeath, int roundTick) {
            this.AttackerID = attackerID;
            this.AssisterID = assisterID;
            this.VictimID = victimID;
            this.Weapon = weapon;
            this.Hitgroup = hitgroup;
            this.OpeningDeath = openingDeath;
            this.RoundTick = roundTick;
        }
    }

    public class BlindEvent {
        public ulong ThrowerID;
        public ulong BlindedID;
        public float Duration;
        public bool TeamFlash;
        public int RoundTick;

        public BlindEvent(ulong throwerID, ulong blindedID, float duration, bool teamFlash, int roundTick) {
            this.ThrowerID = throwerID;
            this.BlindedID = blindedID;
            this.Duration = duration;
            this.TeamFlash = teamFlash;
            this.RoundTick = roundTick;
        }
    }

}
