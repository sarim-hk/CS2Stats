namespace CS2Stats {

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

    public struct GrenadeEvent {
        public ulong ThrowerID;
        public string Weapon;
        public int RoundTick;
    }

}