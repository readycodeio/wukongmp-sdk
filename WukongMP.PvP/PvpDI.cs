using Microsoft.Extensions.Logging;
using WukongMp.Api;
using WukongMp.PvP.Chat;
using WukongMp.PvP.Configuration;
using WukongMp.PvP.GameMode;
using WukongMp.PvP.UI;

namespace WukongMp.PvP
{
    internal class PvpDI
    {
        public static PvpDI Instance { get; } = new();

        private DI DI { get; set; } = null!;

        public PvpChatter PvpChatter { get; private set; } = null!;
        public PvpGameplayConfiguration GameplayConfiguration { get; private set; } = null!;
        public PvpSynchronizer Synchronizer { get; private set; } = null!;
        public PvpSaveManager SaveManager { get; private set; } = null!;
        public PvpWidgetManager WidgetManager { get; private set; } = null!;
        public PvpMode PVP { get; private set; } = null!;

        public void Init(DI wukongDI)
        {
            wukongDI.Logger.LogDebug("Initializing PvP DI...");

            DI = wukongDI;
            
            var chatter = PvpChatter = new PvpChatter(DI.Chatter, DI.GameplayEventRouter, DI.AreaState, DI.ClientOwnership_);

            var gameplayConfig = GameplayConfiguration = new PvpGameplayConfiguration(DI.GameplayConfiguration, DI.AreaState);

            var saveManager = SaveManager = new PvpSaveManager(DI.Logger);
            var widgetManager = WidgetManager = new PvpWidgetManager(DI.WidgetManager, DI.State, DI.PlayerState, DI.EventBus, DI.FreeCameraManager, DI.AreaState, DI.GameplayEventRouter);

            var pvp = PVP = new PvpMode(
                DI.World,
                DI.MappedEvent,
                DI.Serializer,
                DI.RelayClient,
                DI.State,
                DI.AreaState,
                DI.PlayerState,
                DI.PlayerPawnState,
                DI.EventBus,
                DI.ClientRpc,
                DI.Chatter,
                DI.GameplayEventRouter,
                DI.MappingPolicyDir,
                DI.PawnState,
                DI.EcsLoop,
                widgetManager,
                DI.Logger);

            var synchronizer = Synchronizer = new PvpSynchronizer(
                DI.ArchetypeEvent,
                DI.State,
                DI.WukongArchetype,
                DI.World,
                DI.AreaState,
                DI.MappedField,
                DI.PlayerState,
                DI.PlayerPawnState,
                DI.ModeManager,
                DI.NetEntity,
                DI.ClientOwnership_,
                DI.ClientNetEntity,
                DI.JobRegistry,
                DI.NetComponentRegistry,
                DI.RelayClient,
                DI.EcsLoop,
                DI.MappedEvent,
                DI.EventBus,
                DI.ClientRpc,
                widgetManager,
                DI.GameplayEventRouter,
                DI.GameplayConfiguration,
                DI.FreeCameraManager,
                DI.FreeCameraController,
                pvp,
                DI.Logger);
        }
    }
}