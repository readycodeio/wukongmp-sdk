using Microsoft.Extensions.Logging;
using WukongMp.Api;
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

    public static Mod Instance { get; private set; } = null!;

    internal WukongClientApi ClientApi { get; private set; } = null!;
    internal WukongLocalApi LocalApi { get; private set; } = null!;
    internal CoopSaveManager SaveManager { get; private set; } = null!;

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

        Sdk.Api.ReadyM.CoopWidgetManager.Initialize(); // TODO: Internal
        Sdk.Api.ReadyM.CoopSynchronizer.Initialize(); // TODO: Internal

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