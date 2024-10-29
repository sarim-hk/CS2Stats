namespace CS2Stats {

    public struct LivePlayer {
        public int Kills;
        public int Assists;
        public int Deaths;
        public int Damage;
        public int Health;
        public int MoneySaved;

    }

    public struct LiveData {
        public HashSet<LivePlayer>? TPlayers;
        public HashSet<LivePlayer>? CTPlayers;
        public int? TScore;
        public int? CTScore;
        public int? RoundTick;
        public int? BombStatus;
    }

}
