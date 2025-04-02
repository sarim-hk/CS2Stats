namespace CS2Stats {

    public struct LivePlayer {
        public ulong PlayerID;
        public int Kills;
        public int Assists;
        public int Deaths;
        public int ADR;
        public int Health;
        public int Money;
        public int Side;
    }

    public struct LiveStatus {
        public int BombStatus;
        public string Map;
        public int TScore;
        public int CTScore;
    }

    public struct LiveData {
        public List<LivePlayer> Players;
        public LiveStatus Status;
    }

}
