using Microsoft.Extensions.Logging;
using WukongMp.Api;
using WukongMp.Coop.Command;
using WukongMp.Coop.Gamemode;
using WukongMp.Coop.Patches;
using WukongMp.Coop.Systems;
using WukongMp.Sdk;
using WukongMp.Sdk.Api;

namespace WukongMp.Coop;

// ReSharper disable once ClassNeverInstantiated.Global
public sealed class Mod : ModBase
{
    public override string Name => "WukongMp.Coop";
    public override string Version => "1.0.0";

    public static Mod Instance { get; private set; } = null!;

    internal WukongClientApi ClientApi { get; private set; } = null!;
    internal WukongLocalApi LocalApi { get; private set; } = null!;
    internal CoopSaveManager SaveManager { get; private set; } = null!;

    internal static CoopWidgetManager CoopWidgetManager { get; private set; } = null!;

    internal static CoopSynchronizer CoopSynchronizer { get; private set; } = null!;

    protected override void Initialize()
    {
        if (!LaunchParameters.Instance.ValidForCoOp)
        {
            Logger.LogDebug("Co-op not launching.");
            return;
        }

        Instance = this;
        LocalApi = Sdk.Api.ReadyM.Local;
        ClientApi = Sdk.Api.ReadyM.Client;
        SaveManager = new CoopSaveManager(ClientApi, LocalApi, Logger);

        Logger.LogInformation("Initializing {PluginName} v{PluginVersion}", Name, Version);

        LocalApi.AddCommands([
            new CoopCommandRegistration(),
        ]);

        // TODO: These settings are internal to the API, this mod is priviledged to use them via InternalsVisibleTo
        Sdk.Api.ReadyM.Configuration.IsSupportMultiLockEnabled = true;
        Sdk.Api.ReadyM.Configuration.IsStrongDamageImmueEnabled = false;
        Sdk.Api.ReadyM.Configuration.EnableCustomCameraArmLength = false;
        Sdk.Api.ReadyM.Configuration.EnableSpawnedTamers = false;
        Sdk.Api.ReadyM.Configuration.SyncTamerTeamFromGameToEcs = true;

        CoopWidgetManager = new CoopWidgetManager(
            DI.Instance.WidgetManager,
            DI.Instance.State,
            DI.Instance.PlayerState,
            DI.Instance.EventBus,
            DI.Instance.FreeCameraManager,
            DI.Instance.AreaState,
            DI.Instance.GameplayEventRouter);

        CoopWidgetManager.Initialize();

        CoopSynchronizer = new CoopSynchronizer(
            DI.Instance.ArchetypeEvent,
            DI.Instance.State,
            DI.Instance.WukongArchetype,
            DI.Instance.World,
            DI.Instance.MappedField,
            DI.Instance.AreaState,
            DI.Instance.PawnState,
            DI.Instance.PlayerState,
            DI.Instance.PlayerPawnState,
            DI.Instance.ModeManager,
            DI.Instance.NetEntity,
            DI.Instance.ClientOwnership_,
            DI.Instance.MappedEvent,
            DI.Instance.JobRegistry,
            DI.Instance.NetComponentRegistry,
            DI.Instance.RelayClient,
            DI.Instance.EcsLoop,
            DI.Instance.EventBus,
            DI.Instance.WidgetManager,
            DI.Instance.GameplayEventRouter,
            DI.Instance.GameplayConfiguration,
            DI.Instance.FreeCameraManager,
            DI.Instance.FreeCameraController,
            DI.Instance.Logger);

        CoopSynchronizer.Initialize();

        Logger.LogInformation("Initialized {PluginName}", Name);
    }

    protected override IEnumerable<ModSystemBase> DefineSystems()
    {
        yield return new DetectSoftlockSystem(LocalApi, ClientApi, Logger);
        yield return new FixYellowbrowSystem(LocalApi, ClientApi, Logger);
        yield return new ReEnableCollidersSystem(LocalApi, ClientApi, Logger);
        yield return new RespawnMainCharacterSystem(LocalApi, ClientApi, Logger);
        yield return new ScaleMonsterHpSystem(LocalApi, ClientApi, Logger);
    }
}