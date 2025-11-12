using Microsoft.Extensions.Logging;
using WukongMp.Api;
using WukongMp.Api.UI;
using WukongMp.PvP.Chat;
using WukongMp.PvP.Configuration;
using WukongMp.PvP.Gamemode;

namespace WukongMp.PvP
{
    internal class PvpDI
    {
        public static PvpDI Instance { get; } = new();

        public DI DI { get; private set; } = null!;

        public PvpChatter PvpChatter { get; private set; } = null!;
        public PvpGameplayConfiguration GameplayConfiguration { get; private set; } = null!;
        public PvpSynchronizer Synchronizer { get; private set; } = null!;
        public PvpSaveManager SaveManager { get; private set; } = null!;

        public PvpMode PVP { get; private set; } = null!;

        public void Init(DI wukongDI)
        {
            wukongDI.Logger.LogDebug("Initializing PvP DI...");

            DI = wukongDI;

            var chatter = PvpChatter = new PvpChatter(DI.Chatter, DI.PlayerState, DI.Rpc, DI.GameplayEventRouter, DI.AreaState, DI.PawnState, DI.ClientOwnership);
            var gameplayConfig = GameplayConfiguration = new PvpGameplayConfiguration(DI.GameplayConfiguration, DI.AreaState);

            var saveManager = SaveManager = new PvpSaveManager(DI.Logger);

            var pvp = PVP = new PvpMode(DI.World, DI.Serializer, DI.RelayClient, DI.State, DI.AreaState, DI.PlayerState, DI.PlayerPawnState, DI.EventBus, DI.Rpc, DI.Chatter, DI.GameplayEventRouter, DI.ClientOwnership, DI.PawnState, DI.EcsLoop, FreeCameraManager.Instance, DI.Logger);

            var synchronizer = Synchronizer = new PvpSynchronizer(
                DI.ArchetypeEvent,
                DI.State,
                DI.ArchetypeRegistration,
                pvp,
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
                DI.Logger);
        }
    }
}
