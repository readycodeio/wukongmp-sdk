using Microsoft.Extensions.Logging;
using WukongMp.Api;
using WukongMp.Coop.Configuration;
using WukongMp.Coop.Gamemode;
using WukongMp.PvP.Chat;

namespace WukongMp.Coop
{
    internal class CoopDI
    {
        public static CoopDI Instance { get; } = new();

        public DI DI { get; private set; } = null!;

        public CoopChatter CoopChatter { get; private set; } = null!;
        public CoopGameplayConfiguration GameplayConfiguration { get; private set; } = null!;
        public CoopSynchronizer Synchronizer { get; private set; } = null!;
        public CoopSaveManager SaveManager { get; private set; } = null!;
        public WukongPatcher Patcher { get; private set; } = null!;


        public CoopMode Coop { get; private set; } = null!;

        public void Init(DI wukongDI)
        {
            wukongDI.Logger.LogDebug("Initializing Coop DI...");

            DI = wukongDI;
            
            var patcher = Patcher = new CoopPatcher(DI.Prelude);

            var chatter = CoopChatter = new CoopChatter(DI.Chatter);
            var gameplayConfig = GameplayConfiguration = new CoopGameplayConfiguration(DI.GameplayConfiguration);

            var saveManager = SaveManager = new CoopSaveManager(DI.Logger);

            var coop = Coop = new CoopMode(DI.Serializer, DI.RelayClient, DI.AreaState, DI.PlayerState, DI.PawnState, DI.GameplayEventRouter);

            var synchronizer = Synchronizer = new CoopSynchronizer(
                DI.ArchetypeEvent,
                DI.State,
                DI.ArchetypeRegistration,
                DI.World,
                DI.AreaState,
                DI.PlayerState,
                DI.PlayerPawnState,
                DI.ModeManager,
                DI.NetEntity,
                DI.ClientOwnership,
                DI.JobRegistry,
                DI.NetComponentRegistry,
                DI.RelayClient,
                DI.EcsLoop,
                DI.EventBus,
                DI.WidgetManager,
                DI.Rpc,
                DI.GameplayEventRouter,
                DI.GameplayConfiguration,
                DI.Logger);

        }
    }
}
