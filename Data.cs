using CounterStrikeSharp.API.Modules.Utils;

namespace CS2Stats {

    public struct HurtEvent {
        public ulong? AttackerID;
        public int? AttackerSide;
        public ulong VictimID;
        public int? VictimSide;
        public int Damage;
        public string Weapon;
        public int Hitgroup;
        public int RoundTick;
    }

    public struct DeathEvent {
        public ulong? AttackerID;
        public int? AttackerSide;
        public ulong? AssisterID;
        public int? AssisterSide;
        public ulong VictimID;
        public int VictimSide;
        public string Weapon;
        public int Hitgroup;
        public bool OpeningDeath;
        public int RoundTick;
    }

    public struct BlindEvent {
        public ulong ThrowerID;
        public int ThrowerSide;
        public ulong BlindedID;
        public int BlindedSide;
        public float Duration;
        public int RoundTick;
    }

    public struct GrenadeEvent {
        public ulong ThrowerID;
        public int ThrowerSide;
        public string Weapon;
        public int RoundTick;
    }

    public class KASTEvent {
        public ulong PlayerID;
        public int PlayerSide;

        public override bool Equals(object? obj) {
            if (obj is KASTEvent other) {
                return this.PlayerID == other.PlayerID;
            }
            return false;
        }

        public override int GetHashCode() {
            return HashCode.Combine(this.PlayerID);
        }

    }

    public class ClutchEvent {
        public ulong ClutcherID;
        public int ClutcherSide;
        public int EnemyCount;
        public string? Result; // Win, Loss
    }

    public struct DuelEvent {
        public ulong WinnerID;
        public int WinnerSide;
        public ulong LoserID;
        public int LoserSide;
    }

    public struct PlayerParticipated {
        public ulong PlayerID;
        public int PlayerSide;
    }

    public class Match {
        public string MapName { get; }
        public string MatchName { get; set; }
        public bool TeamsNeedSwapping { get; set; }
        public int StartTick { get; }
        public int? EndTick { get; set; }

        public Dictionary<string, TeamInfo> Teams;
        public Queue<Round> Rounds;
        public Round? Round;

        public Match(string mapName, int startTick, Dictionary<string, TeamInfo> teams) {

            this.MapName = mapName;
            this.MatchName = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + "_" + mapName;
            this.StartTick = startTick;
            this.Teams = teams;
            this.TeamsNeedSwapping = false;
            this.Rounds = new Queue<Round>();
        }

    }

    public class Round {
        public int StartTick { get; }
        public int? EndTick { get; set; }
        public bool OpeningDeathOccurred { get; set; }
        public List<PlayerParticipated> PlayersParticipated { get; set; }
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

        public Round(int startTick) {
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

    public class TeamInfo {
        public string TeamID { get; }
        public int Side { get; set; }
        public HashSet<ulong> PlayerIDs { get; }
        public int Score { get; set; }
        public string TeamName { get; set; }
        public string? Result { get; set; }

        public TeamInfo(string teamID, int side, HashSet<ulong> playerIDs, string teamName) {
            this.TeamID = teamID;
            this.Side = side;
            this.PlayerIDs = playerIDs;
            this.Score = 0;
            this.TeamName = teamName;
        }

        public void SwapSides() {
            if (this.Side == (int)CsTeam.Terrorist) {
                this.Side = (int)CsTeam.CounterTerrorist;
            }
            else if (this.Side == (int)CsTeam.CounterTerrorist) {
                this.Side = (int)CsTeam.Terrorist;
            }
        }
    }

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
        public string MapName;
        public int TScore;
        public int CTScore;
    }

    public struct LiveData {
        public List<LivePlayer> Players;
        public LiveStatus Status;
    }

}