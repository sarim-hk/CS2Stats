using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Logging;

namespace CS2Stats {

    public partial class CS2Stats : BasePlugin, IPluginConfig<Config> {

        public override string ModuleName => "CS2Stats";
        public override string ModuleVersion => "2.0.0";

        public required Config Config { get; set; }
        public required CS2StatsAPIClient CS2StatsAPIClient;
        public Match? Match;

        public void OnConfigParsed(Config config)
        {
            this.Config = config;
            this.CS2StatsAPIClient = new CS2StatsAPIClient(Config.APIAuthKey, Config.APIBaseURL);
        }

        public override void Load(bool hotReload) {
            if (this.CS2StatsAPIClient == null) {
                Logger.LogInformation("[Load] CS2StatsAPIClient is null. Unloading...");
                base.Unload(false);
                return;
            }

            RegisterEventHandler<EventCsWinPanelMatch>(EventCsWinPanelMatchHandler);
            RegisterEventHandler<EventRoundFreezeEnd>(EventRoundFreezeEndHandler);
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
            DeregisterEventHandler<EventRoundFreezeEnd>(EventRoundFreezeEndHandler);
            DeregisterEventHandler<EventRoundEnd>(EventRoundEndHandler);
            DeregisterEventHandler<EventRoundStart>(EventRoundStartHandler);
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
