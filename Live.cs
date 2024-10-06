namespace CS2Stats {

    public class LivePlayer {
        public int Kills;
        public int Assists;
        public int Deaths;
        public int Damage;
        public int Health;
        public int MoneySaved;

        public LivePlayer(int kills, int assists, int deaths, int damage, int health, int moneySaved) {
            this.Kills = kills;
            this.Assists = assists;
            this.Deaths = deaths;
            this.Damage = damage;
            this.Health = health;
            this.MoneySaved = moneySaved;
        }

    }

    public class LiveData {
        public List<LivePlayer>? TPlayers;
        public List<LivePlayer>? CTPlayers;
        public int? TScore;
        public int? CTScore;
        public float? RoundTime;
        public int? BombStatus;

        public LiveData(List<LivePlayer>? tPlayers, List<LivePlayer>? ctPlayers, int? tScore, int? ctScore, float? roundTime, int? bombStatus) {
            this.TPlayers = tPlayers;
            this.CTPlayers = ctPlayers;
            this.TScore = tScore;
            this.CTScore = ctScore;
            this.RoundTime = roundTime;
            this.BombStatus = bombStatus;
        }


    }

}
