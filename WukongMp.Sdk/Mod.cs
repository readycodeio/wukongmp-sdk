using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using b1;
using b1.BGW;
using CSharpModBase;
using CSharpModBase.Input;
using DryIoc;
using Friflo.Engine.ECS.Systems;
using Microsoft.Extensions.Logging;
using PreludeLib.Compat;
using ReadyM.Api;
using UnrealEngine.Engine;
using WukongMp.Api;
using WukongMp.Api.NameCompressors;
using WukongMp.Api.Patches;
using WukongMp.Api.Shim;
using WukongMp.Api.WukongUtils;

namespace WukongMp.Sdk;

// ReSharper disable once UnusedType.Global
internal class Mod : ModBase
{
    public override string Name => "WukongMp.Sdk";
    public override string Version => "1.0.0";

    private PatcherBase _apiPatcher = null!;

#if DEBUG
    private class LoggingListener(ILogger logger) : TraceListener
    {
        public override void Write(string? message)
        {
            if (!string.IsNullOrEmpty(message))
                logger.LogError("Debug.Assert | {Message}", message);
        }

        public override void WriteLine(string? message)
        {
            if (!string.IsNullOrEmpty(message))
                logger.LogError("Debug.Assert | {Message}", message);
        }
    }
#endif

    protected override void Initialize(IDependencyContainer services)
    {
#if DEBUG
        Trace.Listeners.Clear();
        Trace.Listeners.Add(new LoggingListener(Logger));
#endif

        if (!LaunchParameters.Instance.Valid)
        {
            Logger.LogError("Multiplayer is disabled. Launch the game through the ReadyM Launcher to play WukongMP.");
            return;
        }

        DI.Instance.Init();

        // Start the relay client
//         if (LaunchParameters.Instance.PlayShimOnStart)
//             ShimUtils.InitRelayPlayShim(
//                 DI.Instance,
//                 LaunchParameters.Instance.PlayShimFile!
//             );
//         else if (LaunchParameters.Instance.RecordShimOnStart)
//             ShimUtils.InitRelayRecordShim(
//                 DI.Instance,
//                 LaunchParameters.Instance.ServerIp!,
//                 LaunchParameters.Instance.ServerPort!.Value,
//                 LaunchParameters.Instance.UserGuid,
// #if NO_DISCONNECT
//                 true,
// #else
//                 false,
// #endif
//                 LaunchParameters.Instance.RecordShimFile!
//             );
//         else
        ShimUtils.InitRelay(
            DI.Instance,
            LaunchParameters.Instance.ServerIp!,
            LaunchParameters.Instance.ServerPort!.Value,
            LaunchParameters.Instance.UserGuid,
#if NO_DISCONNECT
                true
#else
            false
#endif
        );

        _apiPatcher = new WukongPatcher(typeof(ExceptionPatches).Assembly, "WukongMp.Api", DI.Instance.Prelude);

        DI.Instance.Logger.LogInformation("Initialized {PluginName}", Name);
    }

    public override void LateInit()
    {
        Logger.LogInformation("LateInit WukongMP SDK");
        base.LateInit();

        if (!_apiPatcher.IsPatched)
            _apiPatcher.Patch();

        if (!LaunchParameters.Instance.Valid)
        {
            Logger.LogError("Multiplayer is disabled. Launch the game through the ReadyM Launcher to play WukongMP.");
            return;
        }

        AddModSystemsToEcs();
        DebugUtils.LogUe4SsPresence();
        DetectSdkVersion();
        RegisterKeybinds(DI.Instance);
        DI.Instance.StartHostedServices();
        StartRelayClient();
    }

    private void DetectSdkVersion()
    {
        // InformationalVersion from assembly def
        var trueModVersion = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "";

        Logger.LogInformation("Mod version: {Version}", trueModVersion);
        Logger.LogInformation("Process name: {ProcessName}", Process.GetCurrentProcess().ProcessName);

        DI.Instance.WidgetManager.SetSdkVersion(trueModVersion);
    }

    private void StartRelayClient()
    {
        // NOTE: EcsLoop requires initialization from the same thread that will execute Tick()
        Utils.TryRunOnGameThread(() =>
        {
            if (!DI.Instance.Connection.IsRunning)
            {
                DI.Instance.EcsLoop.Start();
                DI.Instance.Connection.Start();
            }
            else
            {
                Logger.LogError("WukongMP is already initialized");
                return;
            }

            if (!DI.Instance.Connection.RequestedConnect)
            {
                DI.Instance.Connection.Connect();
            }
        });
    }

    private void AddModSystemsToEcs()
    {
        var assemblySystems = DI.Instance.Container
            .ResolveMany<ModSystemBase>()
            .GroupBy(x => x.GetType().Assembly)
            .ToDictionary(x => x.Key, x => x.Select(s => s.ToBaseSystem()).ToList());

        foreach (var (assembly, systems) in assemblySystems)
        {
            var groupName = assembly.GetName().Name;
            var systemGroup = new SystemGroup(groupName);

            foreach (var system in systems)
            {
                systemGroup.Add(system);
            }

#if DEBUG
            systemGroup.SetMonitorPerf(true);
#endif
            DI.Instance.World.SystemRoot.Add(systemGroup);
        }
    }

    private void RegisterKeybinds(DI di)
    {
#if DEBUG
        di.InputManager.RegisterKeyBind(Key.F3, () =>
        {
            Logging.LogDebug("F3: Toggle super speed");
            DebugUtils.ToggleSuperFastSpeed();
        });

        di.InputManager.RegisterKeyBind(Key.F4, () =>
        {
            Logger.LogDebug("F4: Toggle invincibility");
            DebugUtils.InvincibilityEnabled = !DebugUtils.InvincibilityEnabled;
        });
#endif
        di.InputManager.RegisterKeyBind(Key.F5, () =>
        {
            Logger.LogDebug("F5: Toggle debug widget visibility");
            if (DI.Instance.WukongInputManager.CanApplyInput())
                DI.Instance.WidgetManager.ToggleDebugVisibility();
        });
#if DEBUG
        di.InputManager.RegisterKeyBind(Key.F7, () =>
        {
            Logging.LogDebug("F7: Force be hit animation");

            var localPawn = di.PlayerState.LocalMainCharacter?.Pawn;

            const string beHitMontage = "Player/Wukong/AM/Behit/TeWaZa/LYS_KJLDragon/AM_LYS_KJLDragon_Atk_14_player";
            var fullMontagePath = Compressors.MontageNameCompressor.Decompress(beHitMontage);
            var montage = string.IsNullOrEmpty(fullMontagePath) ? null : BGW_PreloadAssetMgr.Get(GameUtils.GetWorld()).TryGetCachedResourceObj<UAnimMontage>(fullMontagePath, ELoadResourceType.SyncLoadAndCache);

            var events = BUS_EventCollectionCS.Get(localPawn);
            var animInstance = localPawn?.Mesh.GetAnimInstance();
            animInstance?.Montage_Play(montage);
            events.Evt_PlayMontageCallback.Invoke(EMontageBindReason.Default, montage, EMontageCallbackState.OnStarted);
        });

        di.InputManager.RegisterKeyBind(Key.F8, () =>
        {
            Logging.LogDebug("F8: Force hit animation");

            var localPlayer = di.PlayerState.LocalMainCharacter?.Pawn;

            const string beHitMontage = "LYS/LYS_KJLDragon/new/Montage/AM_LYS_KJLDragon_Atk_14_monster";
            var fullMontagePath = Compressors.MontageNameCompressor.Decompress(beHitMontage);
            var montage = string.IsNullOrEmpty(fullMontagePath) ? null : BGW_PreloadAssetMgr.Get(GameUtils.GetWorld()).TryGetCachedResourceObj<UAnimMontage>(fullMontagePath, ELoadResourceType.SyncLoadAndCache);

            var target = TargetingUtils.GetTarget(localPlayer) as ABGUCharacter;

            if (target != null)
            {
                var events = BUS_EventCollectionCS.Get(target);
                var animInstance = target.Mesh.GetAnimInstance();
                animInstance?.Montage_Play(montage);
                events.Evt_PlayMontageCallback.Invoke(EMontageBindReason.Default, montage, EMontageCallbackState.OnStarted);
            }
        });

        di.InputManager.RegisterKeyBind(Key.F9, () =>
        {
            Logging.LogDebug("F9: Show actors markers");
            DebugUtils.ShowMarkersForActors(4000, "BP_DynamicObstcle");
        });

        di.InputManager.RegisterKeyBind(Key.F10, () =>
        {
            Logging.LogDebug("F10: Remove colliders markers");
            DebugUtils.DestroyTmpMarkerActors();
        });

        di.InputManager.RegisterKeyBind(Key.F12, () =>
        {
            Logger.LogDebug("F12: Skip cutscene");
            CutsceneUtils.RequestSkipCurrentCutscene();
        });

        // di.InputManager.RegisterKeyBind(ModifierKeys.Alt, Key.D0, () =>
        // {
        //     Logging.LogDebug("Alt + 0");
        //     if (LaunchParameters.Instance.RecordShimFile != null)
        //         di.ShimController.Save(LaunchParameters.Instance.RecordShimFile!);
        // });

        di.InputManager.RegisterKeyBind(ModifierKeys.Alt, Key.C, () =>
        {
            Logger.LogDebug("Alt + C");
            try
            {
                di.Resolve<WukongNetworkLogger>().DumpDebugInfo();
                di.RelayClient.LogEventStats();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error while dumping debug info");
            }
        });

        DI.Instance.InputManager.RegisterKeyBind(ModifierKeys.Alt, Key.N, () =>
        {
            Logger.LogDebug("Alt + N");
            DI.Instance.NetworkSessionStats.DumpToLog(Logger);
        });

        di.InputManager.RegisterKeyBind(ModifierKeys.Alt, Key.X, () =>
        {
            Logger.LogDebug("Alt + X");
            PlayerUtils.ResetLocalPlayerCooldown();
        });

        di.InputManager.RegisterKeyBind(ModifierKeys.Alt, Key.J, () =>
        {
            Logger.LogDebug("Alt + J");
            DebugUtils.DumpPlayersAnimationDebugInfo(di.State, di.PlayerState);
        });

        di.InputManager.RegisterKeyBind(ModifierKeys.Shift, Key.J, () =>
        {
            Logger.LogDebug("Shift + J");
            DebugUtils.DumpPlayersAnimationDebugInfo(di.State, di.PlayerState);
        });

        di.InputManager.RegisterKeyBind(ModifierKeys.Alt, Key.K, () =>
        {
            Logger.LogDebug("Alt + K");
            DebugUtils.ResetPlayersAnimation(di.State, di.PlayerState);
        });

        di.InputManager.RegisterKeyBind(Key.J, () =>
        {
            Logger.LogDebug("J");
            if (di.WukongInputManager.CanApplyInput())
            {
                if (di.PlayerState.LocalMainCharacter is not { } mainEntity)
                    return;

                CutsceneUtils.TeleportLocalPlayerToCutsceneLocation(mainEntity);
            }
        });
#endif
        di.InputManager.RegisterKeyBind(Key.K, () =>
        {
            Logger.LogDebug("K");
            if (di.WukongInputManager.CanApplyInput())
                di.WidgetManager.ToggleChatVisibility();
        });

        di.InputManager.RegisterKeyBind(Key.F1, () =>
        {
            Logger.LogDebug("F1");
            if (di.WukongInputManager.CanApplyInput())
                di.WidgetManager.ToggleCommandVisibility();
        });

        di.InputManager.RegisterKeyBind(Key.UP, () =>
        {
            Logger.LogDebug("UP");
            di.WidgetManager.CommandSelectUp();
        });

        di.InputManager.RegisterKeyBind(Key.DOWN, () =>
        {
            Logger.LogDebug("DOWN");
            di.WidgetManager.CommandSelectDown();
        });

        di.InputManager.RegisterKeyBind(ModifierKeys.Alt, Key.DOWN, () =>
        {
            Logger.LogDebug("ALT + DOWN");
            di.WidgetManager.CommandHistoryDown();
        });

        di.InputManager.RegisterKeyBind(ModifierKeys.Alt, Key.UP, () =>
        {
            Logger.LogDebug("ALT + UP");
            di.WidgetManager.CommandHistoryUp();
        });

        di.InputManager.RegisterKeyBind(Key.TAB, () =>
        {
            Logger.LogDebug("TAB");
            di.WidgetManager.CommandSelectSuggestion();
        });

        di.InputManager.RegisterKeyBind(Key.ENTER, () =>
        {
            Logger.LogDebug("ENTER");
            di.WukongInputManager.HandleEnterPressed();
        });
    }

    public override void DeInit()
    {
        Logger.LogInformation("DeInit");

        if (!LaunchParameters.Instance.ValidForCoOp)
        {
            return;
        }

        if (_apiPatcher.IsPatched)
            _apiPatcher.Unpatch();

        Utils.TryRunOnGameThread(() =>
        {
            if (DI.Instance.Connection.RequestedConnect)
            {
                DI.Instance.Connection.Disconnect();
            }

            if (DI.Instance.Connection.IsRunning)
            {
                DI.Instance.Connection.Stop();
                DI.Instance.EcsLoop.Stop();
            }
        });

        base.DeInit();
    }

    public override object GetReloadContext()
    {
        Logger.LogInformation("GetReloadContext");
        return (bool?)DI.Instance.AreaState.InRoom;
    }

    public override void Reload(object? context)
    {
        Logger.LogInformation("Reload");

        var connectedAndInRoom = context as bool?;
        if (connectedAndInRoom == true)
        {
            Logger.LogInformation("Reconnecting after a reload");
            DI.Instance.Connection.Reconnect();
        }
    }
}