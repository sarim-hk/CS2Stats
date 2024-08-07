using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CS2Stats.Structs;
using Microsoft.Extensions.Logging;

namespace CS2Stats {

    public partial class CS2Stats : BasePlugin, IPluginConfig<MySQLConfig> {

        public override string ModuleName => "CS2Stats";
        public override string ModuleVersion => "0.0.1";

        public Database? database;
        public MySQLConfig Config { get; set; }

        public Dictionary<ulong, Player>? startingPlayers;
        public bool teamsNeedSwapping = false;
        public bool matchInProgress = false;

        public void OnConfigParsed(MySQLConfig config) {
            Config = config;
            database = new Database(Config.MySQLServer, Config.MySQLDatabase, Config.MySQLUsername, Config.MySQLPassword);
        }

        public override void Load(bool hotReload) {
            if (database == null) {
                Logger.LogError("Database is null. Unloading...");
                base.Unload(false);
            }

            RegisterEventHandler<EventCsWinPanelMatch>(EventCsWinPanelMatchHandler);
            RegisterEventHandler<EventRoundEnd>(EventRoundEndHandler);
            RegisterEventHandler<EventRoundAnnounceLastRoundHalf>(EventRoundAnnounceLastRoundHalfHandler);
            RegisterEventHandler<EventRoundStart>(EventRoundStartHandler);
            Logger.LogInformation("Plugin loaded.");    
        }

        public override void Unload(bool hotReload) {
            Logger.LogInformation("Plugin unloaded.");
        }

    }
}
