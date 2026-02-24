using Microsoft.Extensions.Logging;
using ReadyM.Api;
using WukongMp.Coop.Command;
using WukongMp.Coop.Gamemode;
using WukongMp.Coop.Systems;
using WukongMp.Sdk;
using WukongMp.Sdk.Api;

namespace WukongMp.Coop;

// ReSharper disable once ClassNeverInstantiated.Global
public sealed class Mod : ModBase
{
    public override string Name => "WukongMp.Coop";
    public override string Version => "1.0.0";

    private readonly List<PluginSystemBase> _systems = [];

    public static Mod Instance { get; private set; } = null!;

    internal WukongClientApi ClientApi { get; private set; } = null!;
    internal WukongLocalApi LocalApi { get; private set; } = null!;
    internal CoopSaveManager SaveManager { get; private set; } = null!;

    public override void Init()
    {
        base.Init();
        
        Instance = this;
        var localApi = LocalApi = Sdk.Api.ReadyM.Local;
        var clientApi = ClientApi = Sdk.Api.ReadyM.Client;
        SaveManager = new CoopSaveManager(clientApi, localApi, Logger);

        Logger.LogInformation("Initializing {PluginName} v{PluginVersion}", Name, Version);

        _systems.Add(new DetectSoftlockSystem(localApi, clientApi, Logger));
        _systems.Add(new FixYellowbrowSystem(localApi, clientApi, Logger));
        _systems.Add(new ReEnableCollidersSystem(localApi, clientApi, Logger));
        _systems.Add(new RespawnMainCharacterSystem(localApi, clientApi, Logger));
        _systems.Add(new ScaleMonsterHpSystem(localApi, clientApi, Logger));

        localApi.AddCommands([
            new CoopCommandRegistration(),
        ]);

        // TODO: These settings are internal to the API, this mod is priviledged to use them via InternalsVisibleTo
        Sdk.Api.ReadyM.Configuration.IsSupportMultiLockEnabled = true;
        Sdk.Api.ReadyM.Configuration.IsStrongDamageImmueEnabled = false;
        Sdk.Api.ReadyM.Configuration.EnableCustomCameraArmLength = false;
        Sdk.Api.ReadyM.Configuration.EnableSpawnedTamers = false;
        Sdk.Api.ReadyM.Configuration.SyncTamerTeamFromGameToEcs = true;

        Sdk.Api.ReadyM.CoopWidgetManager.Initialize(); // TODO: Internal
        Sdk.Api.ReadyM.CoopSynchronizer.Initialize(); // TODO: Internal

        Logger.LogInformation("Initialized {PluginName}", Name);
    }

    public override void DeInit()
    {
        _systems.ForEach(x => x.Dispose());
        _systems.Clear();
        
        base.DeInit();
    }
}