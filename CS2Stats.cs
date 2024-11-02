using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Logging;

namespace CS2Stats {

    public partial class CS2Stats : BasePlugin, IPluginConfig<Config> {

        public override string ModuleName => "CS2Stats";
        public override string ModuleVersion => "2.0.0";

        public Config Config { get; set; }
        public Database? Database;
        public SteamAPIClient? SteamAPIClient;
        public Match? Match;

        public void OnConfigParsed(Config config) {
            this.Config = config;
            this.Database = new Database(Config.MySQLServer, Config.MySQLDatabase, Config.MySQLUsername, Config.MySQLPassword);
            this.SteamAPIClient = new SteamAPIClient(Config.SteamAPIKey);
        }

        public override void Load(bool hotReload) {
            if (this.Database == null) {
                Logger.LogInformation("[Load] Database is null. Unloading...");
                base.Unload(false);
            }

            RegisterEventHandler<EventCsWinPanelMatch>(EventCsWinPanelMatchHandler);
            RegisterEventHandler<EventRoundStart>(EventRoundStartHandler);
            RegisterEventHandler<EventRoundEnd>(EventRoundEndHandler);
            RegisterEventHandler<EventPlayerHurt>(EventPlayerHurtHandler);
            RegisterEventHandler<EventPlayerDeath>(EventPlayerDeathHandler);
            RegisterEventHandler<EventPlayerBlind>(EventPlayerBlindHandler);
            RegisterEventHandler<EventGrenadeThrown>(EventGrenadeThrownHandler);
            RegisterEventHandler<EventRoundAnnounceLastRoundHalf>(EventRoundAnnounceLastRoundHalfHandler);
            RegisterEventHandler<EventBombPlanted>(EventBombPlantedHandler);
            RegisterEventHandler<EventBombDefused>(EventBombDefusedHandler);
            RegisterListener<Listeners.OnClientAuthorized>(OnClientAuthorizedHandler);
            Logger.LogInformation("[Load] Plugin loaded.");
        }

        public override void Unload(bool hotReload) {
            DeregisterEventHandler<EventCsWinPanelMatch>(EventCsWinPanelMatchHandler);
            DeregisterEventHandler<EventRoundStart>(EventRoundStartHandler);
            DeregisterEventHandler<EventRoundEnd>(EventRoundEndHandler);
            DeregisterEventHandler<EventPlayerHurt>(EventPlayerHurtHandler);
            DeregisterEventHandler<EventPlayerBlind>(EventPlayerBlindHandler);
            DeregisterEventHandler<EventGrenadeThrown>(EventGrenadeThrownHandler);
            DeregisterEventHandler<EventPlayerDeath>(EventPlayerDeathHandler);
            DeregisterEventHandler<EventBombPlanted>(EventBombPlantedHandler);
            DeregisterEventHandler<EventBombDefused>(EventBombDefusedHandler);
            DeregisterEventHandler<EventRoundAnnounceLastRoundHalf>(EventRoundAnnounceLastRoundHalfHandler);
            Logger.LogInformation("[Unload] Plugin unloaded.");
        }

    }
}
