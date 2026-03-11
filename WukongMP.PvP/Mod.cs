using System.Collections.Generic;
using CSharpModBase.Input;
using Friflo.Engine.ECS.Systems;
using Microsoft.Extensions.Logging;
using WukongMp.Api;
using WukongMp.Api.ECS.GameEvents;
using WukongMp.Api.WukongUtils;
using WukongMp.PvP.Command;
using WukongMp.PvP.ECS.Systems;
using WukongMp.Sdk;
using WukongMp.Sdk.Api;

namespace WukongMp.PvP;

// ReSharper disable once UnusedType.Global
public sealed class Mod : ModBase
{
    public override string Name => "WukongMp PvP";
    public override string Version => "1.0.0";

    public static Mod Instance { get; private set; } = null!;
    internal WukongClientApi ClientApi { get; private set; } = null!;
    internal WukongLocalApi LocalApi { get; private set; } = null!;

    protected override void Initialize()
    {
        base.Initialize();
        
        if (!LaunchParameters.Instance.ValidForPvP)
        {
            Logger.LogDebug("Pvp not launching.");
            return;
        }

        Logger.LogInformation("Initializing {PluginName} v{PluginVersion}", Name, Version);
        
        Instance = this;
        PvpDI.Instance.Init(DI.Instance);

        LocalApi = Sdk.Api.ReadyM.Local;
        ClientApi = Sdk.Api.ReadyM.Client;

        // TODO: We don't want to expose DI here
        LocalApi.AddCommands([
            new PvpCommandRegistration(DI.Instance.PlayerState, DI.Instance.AreaState, DI.Instance.MappedEvent, DI.Instance.Chatter, DI.Instance.CommandConsole)
        ]);
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
        
#if DEBUG
        DI.Instance.InputManager.RegisterKeyBind(Key.F9, () =>
        {
            Logger.LogDebug("F9: Show actors markers");
            DebugUtils.ShowMarkersForActors(300);
        });

        DI.Instance.InputManager.RegisterKeyBind(Key.F10, () =>
        {
            Logger.LogDebug("F10: Destroy tmp actors markers");
            DebugUtils.DestroyTmpMarkerActors();
        });

        DI.Instance.InputManager.RegisterKeyBind(ModifierKeys.Alt, Key.D0, () =>
        {
            Logger.LogDebug("Alt + 0");
            if (LaunchParameters.Instance.RecordShimFile != null)
                DI.Instance.ShimController.Save(LaunchParameters.Instance.RecordShimFile!);
        });

        DI.Instance.InputManager.RegisterKeyBind(ModifierKeys.Alt, Key.C, () =>
        {
            Logger.LogDebug("Alt + C");
            DI.Instance.NetLogger.DumpDebugInfo();
        });

        DI.Instance.InputManager.RegisterKeyBind(ModifierKeys.Alt, Key.N, () =>
        {
            Logger.LogDebug("Alt + N");
            DI.Instance.NetworkSessionStats.DumpToLog(Logger);
        });

        DI.Instance.InputManager.RegisterKeyBind(ModifierKeys.Alt, Key.X, () =>
        {
            Logger.LogDebug("Alt + X");
            PlayerUtils.ResetLocalPlayerCooldown();
        });

        DI.Instance.InputManager.RegisterKeyBind(ModifierKeys.Alt, Key.J, () =>
        {
            Logger.LogDebug("Alt + J");

            var mainEntity = DI.Instance.PlayerState.LocalMainCharacter;
            if (mainEntity == null)
                return;

            DI.Instance.MappedEvent.InvokeInGameAndNotifyEcs(new MontageCallbackEvent(mainEntity.Value, "Player/Wukong/AM/Attack/ComboB/AM_wukong_combob_z_02_weak", 0f, false));
        });

        DI.Instance.InputManager.RegisterKeyBind(ModifierKeys.Alt, Key.K, () =>
        {
            Logger.LogDebug("Alt + K");

            var mainEntity = DI.Instance.PlayerState.LocalMainCharacter;
            if (mainEntity == null)
                return;

            DI.Instance.MappedEvent.InvokeInGameAndNotifyEcs(new MontageCallbackEvent(mainEntity.Value, "Player/Wukong/AM/Attack/ComboB/AM_wukong_combob_z_02", 0f, false));
        });
#endif
        DI.Instance.InputManager.RegisterKeyBind(Key.F5, () =>
        {
            Logger.LogDebug("F5");
            if (DI.Instance.WukongInputManager.CanApplyInput())
                DI.Instance.WidgetManager.ToggleDebugVisibility();
        });

        DI.Instance.InputManager.RegisterKeyBind(Key.J, () =>
        {
            Logger.LogDebug("J");
            if (DI.Instance.WukongInputManager.CanApplyInput())
                PvpDI.Instance.PVP.SwitchReadyStateMulti();
        });

        DI.Instance.InputManager.RegisterKeyBind(Key.L, () =>
        {
            Logger.LogDebug("L");
            if (DI.Instance.WukongInputManager.CanApplyInput())
                PvpDI.Instance.PVP.SwitchTeam();
        });

        DI.Instance.InputManager.RegisterKeyBind(Key.K, () =>
        {
            Logger.LogDebug("K");
            if (DI.Instance.WukongInputManager.CanApplyInput())
                DI.Instance.WidgetManager.ToggleChatVisibility();
        });

        DI.Instance.InputManager.RegisterKeyBind(Key.F1, () =>
        {
            Logger.LogDebug("F1");
            if (DI.Instance.WukongInputManager.CanApplyInput())
                DI.Instance.WidgetManager.ToggleCommandVisibility();
        });
    }
}