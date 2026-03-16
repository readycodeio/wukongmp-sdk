using CSharpModBase.Input;
using Friflo.Json.Burst;
using Microsoft.Extensions.Logging;
using WukongMp.Api;
using WukongMp.Coop.Commands;
using WukongMp.Coop.Configuration;
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
    internal CoopSaveManager SaveManager { get; private set; } = null!;
    internal static CoopWidgetManager CoopWidgetManager { get; private set; } = null!;
    internal static CoopEventCallbacks CoopEventCallbacks { get; private set; } = null!;

    protected override void Initialize()
    {
        // if (!LaunchParameters.Instance.ValidForCoOp)
        // {
        //     Logger.LogDebug("Co-op not launching.");
        //     return;
        // }

        Instance = this;
        SaveManager = new CoopSaveManager(Logger);

        Logger.LogInformation("Initializing {PluginName} v{PluginVersion}", Name, Version);

        WukongApi.Console.AddCommands([
            new CoopCommandRegistration(),
        ]);

        WukongApi.Configuration.IsSupportMultiLockEnabled = true;
        WukongApi.Configuration.IsStrongDamageImmueEnabled = false;
        WukongApi.Configuration.EnableCustomCameraArmLength = false;
        WukongApi.Configuration.DeleteDestroyedTamersFromEcs = false;
        WukongApi.Configuration.SyncTamerTeamFromGameToEcs = true;

        CoopWidgetManager = new CoopWidgetManager();

        CoopWidgetManager.Initialize();
        CoopEventCallbacks = new CoopEventCallbacks(Logger);

        Logger.LogInformation("Initialized {PluginName}", Name);
    }

    public override void LateInit()
    {
        base.LateInit();

        WukongApi.Input.RegisterKeyBind(Key.F6, () =>
        {
            Logging.LogDebug("F6: Toggle HP scaling");
            Config.ScaleMonsterHpToHalf = !Config.ScaleMonsterHpToHalf;
        });
    }
}