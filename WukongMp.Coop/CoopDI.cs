using Microsoft.Extensions.Logging;
using WukongMp.Api;
using WukongMp.Api.UI;
using WukongMp.Coop.Configuration;
using WukongMp.Coop.Gamemode;
using WukongMp.Coop.UI;
using WukongMp.Coop.Command;
using WukongMp.Api.FreeCamera;

namespace WukongMp.Coop
{
    internal class CoopDI
    {
        public static CoopDI Instance { get; } = new();

        public DI DI { get; private set; } = null!;

        public CoopCommandConsole CoopCommandConsole { get; private set; } = null!;
        public CoopGameplayConfiguration GameplayConfiguration { get; private set; } = null!;
        public CoopSynchronizer Synchronizer { get; private set; } = null!;
        public CoopSaveManager SaveManager { get; private set; } = null!;
        public CoopWidgetManager WidgetManager { get; private set; } = null!;
        public WukongPatcher Patcher { get; private set; } = null!;


        public CoopMode Coop { get; private set; } = null!;

        public void Init(DI wukongDI)
        {
            wukongDI.Logger.LogDebug("Initializing Coop DI...");

            DI = wukongDI;

            var patcher = Patcher = new CoopPatcher(DI.Prelude);

            var coopCommandConsole = CoopCommandConsole = new CoopCommandConsole(DI.CommandConsole);
            var gameplayConfig = GameplayConfiguration = new CoopGameplayConfiguration(DI.GameplayConfiguration);

            var saveManager = SaveManager = new CoopSaveManager(DI.Logger);
            var widgetManager = WidgetManager = new CoopWidgetManager(DI.WidgetManager, DI.State, DI.PlayerState, DI.EventBus, DI.FreeCameraManager, DI.AreaState, DI.GameplayEventRouter);

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
                DI.ColliderDisableData,
                DI.FreeCameraManager,
                DI.FreeCameraMover,
                DI.Logger);
        }
    }
}