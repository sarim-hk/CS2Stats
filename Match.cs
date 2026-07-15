using CounterStrikeSharp.API;

namespace CS2Stats {

    public class Match {
        public string MapName { get;  }

        public string MatchName { get; set; }

        public bool TeamsNeedSwapping { get; set; }
        public int StartTick { get; }
        public int? EndTick { get; set; }

        public Dictionary<string, TeamInfo> StartingPlayers;
        public Queue<Round> Rounds;
        public Round? Round;

        public Match(string mapName, int startTick, Dictionary<string, TeamInfo> startingPlayers) {

            this.MapName = mapName;
            this.MatchName = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + "_" + mapName;
            this.StartTick = startTick;
            this.StartingPlayers = startingPlayers;
            this.TeamsNeedSwapping = false;

            this.Rounds = new Queue<Round>();
        }

    }

}
