using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Logging;

namespace CS2Stats
{

    public partial class CS2Stats : BasePlugin, IPluginConfig<Config> {

        public override string ModuleName => "CS2Stats";
        public override string ModuleVersion => "2.0.0";
        
        public Config Config { get; set; }
        public Database? database;
        public SteamAPIClient? steamAPIClient;
        public Dictionary<string, TeamInfo>? startingPlayers;
        public CounterStrikeSharp.API.Modules.Timers.Timer? liveTimer;

        public bool teamsNeedSwapping;
        public int? matchID;
        public int? roundID;
        public string? teamNum2ID, teamNum3ID;

        public void OnConfigParsed(Config config) {
            Config = config;
            database = new Database(Config.MySQLServer, Config.MySQLDatabase, Config.MySQLUsername, Config.MySQLPassword);
            steamAPIClient = new SteamAPIClient(Config.SteamAPIKey);
        }

        public override void Load(bool hotReload) {
            if (this.database == null) {
                Logger.LogInformation("Database is null. Unloading...");
                base.Unload(false);
            }

            RegisterEventHandler<EventCsWinPanelMatch>(EventCsWinPanelMatchHandler);
            RegisterEventHandler<EventRoundStart>(EventRoundStartHandler);
            RegisterEventHandler<EventRoundEnd>(EventRoundEndHandler);
            RegisterEventHandler<EventPlayerHurt>(EventPlayerHurtHandler);
            RegisterEventHandler<EventPlayerDeath>(EventPlayerDeathHandler);
            RegisterEventHandler<EventRoundAnnounceLastRoundHalf>(EventRoundAnnounceLastRoundHalfHandler);
            RegisterListener<Listeners.OnClientAuthorized>(OnClientAuthorizedHandler);

            Logger.LogInformation("Plugin loaded.");    
        }

        public override void Unload(bool hotReload) {
            Logger.LogInformation("Plugin unloaded.");
        }



    }
}
