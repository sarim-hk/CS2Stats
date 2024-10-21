namespace CS2Stats {

    public class Round {

        public int? RoundID;
        public int? ServerTick;
        public bool OpeningDeathOccurred;
        public HashSet<ulong> KASTEvents;
        public HashSet<HurtEvent> HurtEvents;
        public HashSet<DeathEvent> DeathEvents;

        public Round() {
            this.OpeningDeathOccurred = false;
            this.KASTEvents = new HashSet<ulong>();
            this.HurtEvents = new HashSet<HurtEvent>();
            this.DeathEvents = new HashSet<DeathEvent>();
        }
    }

    public class HurtEvent {
        public ulong? AttackerID;
        public ulong VictimID;
        public int DamageAmount;
        public string Weapon;
        public int Hitgroup;
        public int ServerTick;

        public HurtEvent(ulong? attackerID, ulong victimID, int damageAmount, string weapon, int hitGroup, int serverTick) {
            this.AttackerID = attackerID;
            this.VictimID = victimID;
            this.DamageAmount = damageAmount;
            this.Weapon = weapon;
            this.Hitgroup = hitGroup;
            this.ServerTick = serverTick;
        }
    }

    public class DeathEvent {
        public ulong? AttackerID;
        public ulong? AssisterID;
        public ulong VictimID;
        public string Weapon;
        public int Hitgroup;
        public bool OpeningDeath;
        public int ServerTick;

        public DeathEvent(ulong? attackerID, ulong? assisterID, ulong victimID, string weapon, int hitgroup, bool openingDeath, int serverTick) {
            this.AttackerID = attackerID;
            this.AssisterID = assisterID;
            this.VictimID = victimID;
            this.Weapon = weapon;
            this.Hitgroup = hitgroup;
            this.OpeningDeath = openingDeath;
            this.ServerTick = serverTick;
        }
    }

}
