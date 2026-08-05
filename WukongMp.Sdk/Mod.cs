using System;
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
using ReadyM.Api.DI;
using ReadyM.Api.Multiplayer.RPC;
using ReadyM.Relay.Client;
using UnrealEngine.Engine;
using WukongMp.Api;
using WukongMp.Api.NameCompressors;
using WukongMp.Api.Patches;
using WukongMp.Api.Shim;
using WukongMp.Api.WukongUtils;
using WukongMp.Sdk.Api;

namespace WukongMp.Sdk;

// ReSharper disable once UnusedType.Global
internal class Mod : ModBase
{
    public override string Name => "WukongMp.Sdk";

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
        // NOTE: LogInformation so these survive a release build's minimum level, and the mod loader
        // force-flushes for the whole Init window so the last line written is the step that crashed.
        //
        // This first line is load bearing: if it appears, Initialize was compiled and entered successfully, so
        // a crash is in the body. If it never appears, the fault is in Mono compiling or type-loading this
        // method, before any of it runs.
        Logger.LogInformation("Initialize entered");

// #if DEBUG
//         // NOTE: Trace.Listeners is not a cheap property access. The first touch initialises the diagnostics
//         // config, which on Mono goes through ConfigurationManager and pulls System.Configuration and
//         // System.Xml out of the game's bundled assemblies for the first time.
//         //
//         // Bracketed separately because a crash report put the fault between "Initialize entered" and the next
//         // line, and in a Debug build these two statements are the only code in between. Note the whole block is
//         // DEBUG only, so a Release build of the mod does not execute any of it.
//         Logger.LogInformation("Initialize step begin: Trace.Listeners.Clear");
//         Trace.Listeners.Clear();
//         Logger.LogInformation("Initialize step end: Trace.Listeners.Clear");
//
//         Logger.LogInformation("Initialize step begin: Trace.Listeners.Add");
//         Trace.Listeners.Add(new LoggingListener(Logger));
//         Logger.LogInformation("Initialize step end: Trace.Listeners.Add");
// #endif

        // Split from the Valid check because constructing LaunchParameters reads and deletes the handshake
        // file, which is real work, while Valid is just field comparisons.
        Logger.LogInformation("Initialize step begin: LaunchParameters.Instance");
        var launchParameters = LaunchParameters.Instance;
        Logger.LogInformation("Initialize step end: LaunchParameters.Instance");

        if (!launchParameters.Valid)
        {
            Logger.LogError("Multiplayer is disabled. Launch the game through the ReadyM Launcher to play WukongMP.");
            return;
        }

        Logger.LogInformation("Initialize step begin: DI.Init");
        DI.Instance.Init();
        Logger.LogInformation("Initialize step end: DI.Init");

        Logger.LogInformation("Initialize step begin: RegisterApis");
        WukongApi.RegisterApis();
        Logger.LogInformation("Initialize step end: RegisterApis");

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
        Logger.LogInformation("Initialize step begin: InitRelay");
        ShimUtils.InitRelay(
            DI.Instance,
            LaunchParameters.Instance.ServerIp!,
            LaunchParameters.Instance.ServerPort!.Value,
            LaunchParameters.Instance.Ticket,
#if NO_DISCONNECT
                true
#else
            false
#endif
        );
        Logger.LogInformation("Initialize step end: InitRelay");

        Logger.LogInformation("Initialize step begin: WukongPatcher ctor");
        _apiPatcher = new WukongPatcher(typeof(ExceptionPatches).Assembly, "WukongMp.Api", DI.Instance.Prelude);
        Logger.LogInformation("Initialize step end: WukongPatcher ctor");

        DI.Instance.Logger.LogInformation("Initialized {PluginName}", Name);
    }

    public override void LateInit()
    {
        // Same reasoning as Initialize: the loader force-flushes through late init, so the last "step begin"
        // without a matching "step end" names whatever took the process down.
        Logger.LogInformation("LateInit step begin: base.LateInit");
        base.LateInit();
        Logger.LogInformation("LateInit step end: base.LateInit");

        if (!_apiPatcher.IsPatched)
        {
            Logger.LogInformation("LateInit step begin: apiPatcher.Patch");
            _apiPatcher.Patch();
            Logger.LogInformation("LateInit step end: apiPatcher.Patch");
        }

        if (!LaunchParameters.Instance.Valid)
        {
            Logger.LogError("Multiplayer is disabled. Launch the game through the ReadyM Launcher to play WukongMP.");
            return;
        }

        Logger.LogInformation("LateInit step begin: AddModSystemsToEcs");
        AddModSystemsToEcs();
        Logger.LogInformation("LateInit step end: AddModSystemsToEcs");

        Logger.LogInformation("LateInit step begin: SetUpRpcOffsets");
        SetUpRpcOffsets();
        Logger.LogInformation("LateInit step end: SetUpRpcOffsets");

        DebugUtils.LogUe4SsPresence();
        DetectSdkVersion();

        Logger.LogInformation("LateInit step begin: RegisterKeybinds");
        RegisterKeybinds(DI.Instance);
        Logger.LogInformation("LateInit step end: RegisterKeybinds");

        Logger.LogInformation("LateInit step begin: StartHostedServices");
        DI.Instance.StartHostedServices();
        Logger.LogInformation("LateInit step end: StartHostedServices");

        Logger.LogInformation("LateInit step begin: StartRelayClient");
        StartRelayClient();
        Logger.LogInformation("LateInit step end: StartRelayClient");
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
    
    private void SetUpRpcOffsets()
    {
        var rpcClasses = DI.Instance.Container.GetServiceRegistrations()
            .Where(r => typeof(RpcClassBase).IsAssignableFrom(r.Factory.ImplementationType ?? r.ServiceType))
            .Where(r => r.Factory.Reuse is null or SingletonReuse)
            .OrderBy(t => t.FactoryRegistrationOrder) // ensure deterministic order
            .ToList();
        
        Logger.LogDebug("Found {RpcCount} RPC classes", rpcClasses.Count);
        var offsetProvider = DI.Instance.Container.Resolve<RpcOffsetProvider>();
        var ecsLoop = DI.Instance.Container.Resolve<IClientEcsUpdateLoop>();
        
        foreach (var rpcClassRegistration in rpcClasses)
        {
            var rpcObject = (RpcClassBase)DI.Instance.Container.Resolve(rpcClassRegistration.ServiceType, rpcClassRegistration.OptionalServiceKey);
            var offsetBefore = offsetProvider.CurrentOffset;
            rpcObject.Initialize(offsetProvider, ecsLoop.Scheduler);
            Logger.LogDebug("Registered RPC class {RpcClass} with codes {BeforeOffset}..{AfterOffset}", rpcClassRegistration.ServiceType.FullName, offsetBefore, offsetProvider.CurrentOffset - 1);
        }
    }

    private void RegisterKeybinds(DI di)
    {
#if DEBUG
        WukongApi.Input.RegisterKeyBind(Key.F3, () =>
        {
            Logging.LogDebug("F3: Toggle super speed");
            DebugUtils.ToggleSuperFastSpeed();
        });

        WukongApi.Input.RegisterKeyBind(Key.F4, () =>
        {
            Logger.LogDebug("F4: Toggle invincibility");
            DebugUtils.InvincibilityEnabled = !DebugUtils.InvincibilityEnabled;
        });
#endif
        WukongApi.Input.RegisterKeyBind(Key.F5, () =>
        {
            Logger.LogDebug("F5: Toggle debug widget visibility");
            if (DI.Instance.WukongInputManager.CanApplyInput())
                DI.Instance.WidgetManager.ToggleDebugVisibility();
        });
#if DEBUG
        WukongApi.Input.RegisterKeyBind(Key.F7, () =>
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

        WukongApi.Input.RegisterKeyBind(Key.F8, () =>
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

        WukongApi.Input.RegisterKeyBind(Key.F9, () =>
        {
            Logging.LogDebug("F9: Show actors markers");
            DebugUtils.ShowMarkersForActors(4000, "BP_DynamicObstcle");
        });

        WukongApi.Input.RegisterKeyBind(Key.F10, () =>
        {
            Logging.LogDebug("F10: Remove colliders markers");
            DebugUtils.DestroyTmpMarkerActors();
        });

        WukongApi.Input.RegisterKeyBind(Key.F12, () =>
        {
            Logger.LogDebug("F12: Skip cutscene");
            CutsceneUtils.RequestSkipCurrentCutscene();
        });

        // WukongApi.Input.RegisterKeyBind(ModifierKeys.Alt, Key.D0, () =>
        // {
        //     Logging.LogDebug("Alt + 0");
        //     if (LaunchParameters.Instance.RecordShimFile != null)
        //         di.ShimController.Save(LaunchParameters.Instance.RecordShimFile!);
        // });

        WukongApi.Input.RegisterKeyBind(ModifierKeys.Alt, Key.C, () =>
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

        WukongApi.Input.RegisterKeyBind(ModifierKeys.Alt, Key.N, () =>
        {
            Logger.LogDebug("Alt + N");
            DI.Instance.NetworkSessionStats.DumpToLog(Logger);
        });

        WukongApi.Input.RegisterKeyBind(ModifierKeys.Alt, Key.X, () =>
        {
            Logger.LogDebug("Alt + X");
            PlayerUtils.ResetLocalPlayerCooldown();
        });

        WukongApi.Input.RegisterKeyBind(ModifierKeys.Alt, Key.J, () =>
        {
            Logger.LogDebug("Alt + J");
            DebugUtils.DumpPlayersAnimationDebugInfo(di.State, di.PlayerState);
        });

        WukongApi.Input.RegisterKeyBind(ModifierKeys.Shift, Key.J, () =>
        {
            Logger.LogDebug("Shift + J");
            DebugUtils.DumpPlayersAnimationDebugInfo(di.State, di.PlayerState);
        });

        WukongApi.Input.RegisterKeyBind(ModifierKeys.Alt, Key.K, () =>
        {
            Logger.LogDebug("Alt + K");
            DebugUtils.ResetPlayersAnimation(di.State, di.PlayerState);
        });
#endif
        WukongApi.Input.RegisterKeyBind(Key.K, () =>
        {
            Logger.LogDebug("K");
            if (di.WukongInputManager.CanApplyInput())
                di.WidgetManager.ToggleChatVisibility();
        });

        WukongApi.Input.RegisterKeyBind(Key.F1, () =>
        {
            di.WidgetManager.ToggleCommandVisibility();
        });

        WukongApi.Input.RegisterKeyBind(Key.UP, () =>
        {
            Logger.LogDebug("UP");
            di.WidgetManager.CommandSelectUp();
        });

        WukongApi.Input.RegisterKeyBind(Key.DOWN, () =>
        {
            Logger.LogDebug("DOWN");
            di.WidgetManager.CommandSelectDown();
        });

        WukongApi.Input.RegisterKeyBind(ModifierKeys.Alt, Key.DOWN, () =>
        {
            Logger.LogDebug("ALT + DOWN");
            di.WidgetManager.CommandHistoryDown();
        });

        WukongApi.Input.RegisterKeyBind(ModifierKeys.Alt, Key.UP, () =>
        {
            Logger.LogDebug("ALT + UP");
            di.WidgetManager.CommandHistoryUp();
        });

        WukongApi.Input.RegisterKeyBind(Key.TAB, () =>
        {
            Logger.LogDebug("TAB");
            di.WidgetManager.CommandSelectSuggestion();
        });

        WukongApi.Input.RegisterKeyBind(Key.ENTER, () =>
        {
            Logger.LogDebug("ENTER");
            di.WukongInputManager.HandleEnterPressed();
        });
    }

    public override void DeInit()
    {
        Logger.LogInformation("DeInit");

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