namespace CS2Stats {

    public class Round {

        public int RoundID { get; }
        public int StartTick { get; }
        public int? EndTick { get; set; }
        public bool OpeningDeathOccurred { get; set; }
        public List<ulong> PlayersParticipated { get; set; }
        public List<HurtEvent> HurtEvents { get; set; }
        public List<DeathEvent> DeathEvents { get; set; }
        public List<BlindEvent> BlindEvents { get; set; }
        public List<GrenadeEvent> GrenadeEvents { get; set; }
        public HashSet<KASTEvent> KASTEvents { get; set; }
        public ClutchEvent? ClutchEvent { get; set; }
        public DuelEvent? DuelEvent { get; set; }

        public string? WinningTeamID { get; set; }
        public string? LosingTeamID { get; set; }
        public int? WinningTeamNum { get; set; }
        public int? LosingTeamNum { get; set; }
        public int? WinningReason { get; set; }

        public Round(int roundID, int startTick) {
            this.RoundID = roundID;
            this.StartTick = startTick;

            this.OpeningDeathOccurred = false;
            this.PlayersParticipated = [];
            this.KASTEvents = [];
            this.HurtEvents = [];
            this.DeathEvents = [];
            this.BlindEvents = [];
            this.GrenadeEvents = [];
        }
    }

}
