namespace CS2Stats {

    public class Round {

        public int? RoundID;
        public int? serverTick;
        public HashSet<ulong> playersKAST;
        public List<HurtEvent> hurtEvents;
        public List<DeathEvent> deathEvents;

        public Round() {
            this.playersKAST = new HashSet<ulong>();
            this.hurtEvents = new List<HurtEvent>();
            this.deathEvents = new List<DeathEvent>();
        }
    }

    public class HurtEvent {
        public ulong AttackerID;
        public ulong VictimID;
        public int DamageAmount;
        public string Weapon;
        public int Hitgroup;
        public int ServerTick;

        public HurtEvent(ulong attackerID, ulong victimID, int damageAmount, string weapon, int hitGroup, int serverTick) {
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
        public int ServerTick;

        public DeathEvent(ulong? attackerID, ulong? assisterID, ulong victimID, string weapon, int hitgroup, int serverTick) {
            this.AttackerID = attackerID;
            this.AssisterID = assisterID;
            this.VictimID = victimID;
            this.Weapon = weapon;
            this.Hitgroup = hitgroup;
            this.ServerTick = serverTick;
        }
    }

}
