using System.Collections.Generic;
using CSharpModBase.Input;
using Friflo.Engine.ECS.Systems;
using Microsoft.Extensions.Logging;
using ReadyM.Api.Command;
using WukongMp.Api;
using WukongMp.Api.ECS.GameEvents;
using WukongMp.Api.WukongUtils;
using WukongMp.PvP.Chat;
using WukongMp.PvP.Command;
using WukongMp.PvP.Configuration;
using WukongMp.PvP.ECS.Systems;
using WukongMp.PvP.GameMode;
using WukongMp.PvP.UI;
using WukongMp.Sdk;
using WukongMp.Sdk.Api;

namespace WukongMp.PvP;

// ReSharper disable once UnusedType.Global
public sealed class Mod : ModBase
{
    public override string Name => "WukongMp PvP";
    public override string Version => "1.0.0";

    public static Mod Instance { get; private set; } = null!;

    protected override void Initialize(IDependencyContainer services)
    {
        base.Initialize(services);

        Logger.LogInformation("Initializing {PluginName} v{PluginVersion}", Name, Version);

        Instance = this;

        services.RegisterSingleton<PvpChatter>();
        services.RegisterSingleton<PvpGameplayConfiguration>();
        services.RegisterSingleton<PvpSaveManager>();
        services.RegisterSingleton<PvpWidgetManager>();
        services.RegisterSingleton<PvpMode>();
        services.RegisterSingleton<PvpSynchronizer>();

        services.RegisterSingleton<IConsoleCommandRegistration, PvpCommandRegistration>();

        // WukongApi.Console.AddCommands([
        //     new PvpCommandRegistration(DI.Instance.PlayerState, DI.Instance.AreaState, DI.Instance.MappedEvent, DI.Instance.Chatter, DI.Instance.CommandConsole)
        // ]);
    }

    protected override IEnumerable<BaseSystem> DefineFrifloSystems()
    {
        yield return new PlayerListSystem(DI.Instance.PlayerState, DI.Instance.AreaState, PvpDI.Instance.WidgetManager);
        yield return new PvpAntiStallSystem(DI.Instance.AreaState, DI.Instance.ClientRpc);
        yield return new PvpRoundEndSystem(DI.Instance.World, DI.Instance.AreaState, PvpDI.Instance.PVP, DI.Instance.EcsLoop);
        yield return new ReadinessSystem(DI.Instance.World, DI.Instance.AreaState, PvpDI.Instance.WidgetManager, DI.Instance.PlayerState, PvpDI.Instance.PVP);
    }

    public override void LateInit()
    {
        base.LateInit();
        
        WukongApi.Input.RegisterKeyBind(Key.J, () =>
        {
            Logger.LogDebug("J");
            if (WukongApi.Input.CanApplyInput())
                WukongApi.Services.Resolve<PvpMode>().SwitchReadyStateMulti();
        });

        WukongApi.Input.RegisterKeyBind(Key.L, () =>
        {
            Logger.LogDebug("L");
            if (WukongApi.Input.CanApplyInput())
                WukongApi.Services.Resolve<PvpMode>().SwitchTeam();
        });
    }
}