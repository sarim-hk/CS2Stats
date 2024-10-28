namespace CS2Stats {

    public class Match {
        public int? MatchID;
        public int? RoundID;
        public string? MapName;

        public bool TeamsNeedSwapping;
        public int? StartTick;
        public int? EndTick;

        public Dictionary<string, TeamInfo> StartingPlayers;
        public Queue<Round> Rounds;
        public Round Round;

        public Match() {
            this.StartingPlayers = [];
            this.Rounds = new Queue<Round>();
            this.Round = new Round();

        }

    }

}
