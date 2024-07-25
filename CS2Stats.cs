using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using Microsoft.Extensions.Logging;
namespace CS2Stats {

    public partial class CS2Stats : BasePlugin, IPluginConfig<MySQLConfig> {

        public override string ModuleName => "CS2Stats";
        public override string ModuleVersion => "0.0.1";

        public Database? database;
        public Dictionary<ulong, int>? playerRounds;
        public MySQLConfig Config { get; set; }
        public bool matchInProgress = false;

        public void OnConfigParsed(MySQLConfig config) {
            Config = config;
            database = new Database(Config.MySQLServer, Config.MySQLDatabase, Config.MySQLUsername, Config.MySQLPassword);
        }

        public override void Load(bool hotReload) {
            RegisterEventHandler<EventCsWinPanelMatch>(EventCsWinPanelMatchHandler);
            RegisterEventHandler<EventRoundEnd>(EventRoundEndHandler);
            Logger.LogInformation("Plugin loaded.");    
        }

        public override void Unload(bool hotReload) {
            Logger.LogInformation("Plugin unloaded.");
        }

    }
}
