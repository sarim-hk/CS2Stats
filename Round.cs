namespace CS2Stats {

    public class Round {

        public int? RoundID;
        public int StartTick;
        public int? EndTick;
        public bool OpeningDeathOccurred;
        public HashSet<ulong> PlayersParticipated;
        public HashSet<ulong> PlayersKAST;
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
            this.PlayersKAST = [];
            this.HurtEvents = [];
            this.DeathEvents = [];
            this.BlindEvents = [];
        }
    }

}
