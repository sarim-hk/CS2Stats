namespace CS2Stats {

    public class Round {

        public int RoundID { get; }
        public int StartTick { get; }
        public int? EndTick { get; set; }
        public bool OpeningDeathOccurred { get; set; }
        public HashSet<ulong> PlayersParticipated { get; set; }
        public HashSet<ulong> PlayersKAST { get; set; }
        public HashSet<HurtEvent> HurtEvents { get; set; }
        public HashSet<DeathEvent> DeathEvents { get; set; }
        public HashSet<BlindEvent> BlindEvents { get; set; }

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
            this.PlayersKAST = [];
            this.HurtEvents = [];
            this.DeathEvents = [];
            this.BlindEvents = [];
        }
    }

}
