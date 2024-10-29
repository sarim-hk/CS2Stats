namespace CS2Stats {

    public class Match {
        public int MatchID { get; }
        public int RoundID { get; set; }
        public string MapName { get;  }

        public bool TeamsNeedSwapping { get; set; }
        public int StartTick { get; }
        public int? EndTick { get; set; }

        public Dictionary<string, TeamInfo> StartingPlayers;
        public Queue<Round> Rounds;
        public Round? Round;

        public Match(int matchID, int roundID, string mapName, int startTick, Dictionary<string, TeamInfo> startingPlayers) {
            this.MatchID = matchID;
            this.RoundID = roundID;
            this.MapName = mapName;
            this.StartTick = startTick;
            this.StartingPlayers = startingPlayers;

            this.TeamsNeedSwapping = false;
            this.Rounds = new Queue<Round>();
        }

    }

}
