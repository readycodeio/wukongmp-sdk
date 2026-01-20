using System;
using System.Diagnostics;
using System.Reflection;
using b1;
using b1.BGW;
using CSharpModBase;
using CSharpModBase.Input;
using Microsoft.Extensions.Logging;
using UnrealEngine.Engine;
using WukongMp.Api;
using WukongMp.Api.NameCompressors;
using WukongMp.Api.Shim;
using WukongMp.Api.WukongUtils;

namespace WukongMp.Coop
{
    // ReSharper disable once UnusedType.Global
    public class Mod : ICSharpModExV2
    {
        public string Name => "WukongMp co-op";
        public string Version => "1.0.0";

        private ILogger _logger = null!;

        public bool IsDebug
#if DEBUG
            => true;
#else
            => false;
#endif

        public void SetLoggerFactory(ILoggerFactory loggerFactory)
        {
            DI.Instance.InitLogging(loggerFactory);
            _logger = DI.Instance.Logger;
        }

        public void Init()
        {
            if (!LaunchParameters.Instance.Valid)
            {
                _logger.LogError("Multiplayer is disabled. Launch the game through the ReadyM Launcher to play WukongMP.");
                return;
            }

            if (!LaunchParameters.Instance.ValidForCoOp)
            {
                _logger.LogDebug("Co-op not launching.");
                return;
            }

            DI.Instance.Init();
            CoopDI.Instance.Init(DI.Instance);

            if (LaunchParameters.Instance.PlayShimOnStart)
                ShimUtils.InitRelayPlayShim(
                    DI.Instance,
                    LaunchParameters.Instance.PlayShimFile!
                );
            else if (LaunchParameters.Instance.RecordShimOnStart)
                ShimUtils.InitRelayRecordShim(
                    DI.Instance,
                    LaunchParameters.Instance.ServerIp!,
                    LaunchParameters.Instance.ServerPort!.Value,
                    LaunchParameters.Instance.UserGuid,
#if NO_DISCONNECT
                    true,
#else
                    false,
#endif
                    LaunchParameters.Instance.RecordShimFile!
                );
            else
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

            if (!CoopDI.Instance.Patcher.IsPatched)
            {
                CoopDI.Instance.Patcher.Patch();
            }
        }

        public void LateInit()
        {
            if (!LaunchParameters.Instance.Valid)
            {
                _logger.LogError("Multiplayer is disabled. Launch the game through the ReadyM Launcher to play WukongMP.");
                return;
            }

            if (!LaunchParameters.Instance.ValidForCoOp)
            {
                _logger.LogDebug("Co-op not launching.");
                return;
            }

            _logger.LogInformation("Init WukongMP mod");
            DebugUtils.LogUe4SsPresence();

            // InformationalVersion from assembly def
            var trueModVersion = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "";

            _logger.LogInformation("Mod version: {Version}", trueModVersion);
            _logger.LogInformation("Process name: {ProcessName}", Process.GetCurrentProcess().ProcessName);

            DI.Instance.WidgetManager.SetModVersion(trueModVersion);

            // NOTE: EcsLoop requires initialization from the same thread that will execute Tick()
            Utils.TryRunOnGameThread(() =>
            {
                Debug.Assert(CoopDI.Instance.Patcher.IsPatched);

                if (!DI.Instance.Connection.IsRunning)
                {
                    DI.Instance.EcsLoop.Start();
                    DI.Instance.Connection.Start();
                }
                else
                {
                    _logger.LogError("WukongMP is already initialized");
                    return;
                }

                if (!DI.Instance.Connection.RequestedConnect)
                {
                    DI.Instance.Connection.Connect();
                }
            });
#if DEBUG
            DI.Instance.InputManager.RegisterKeyBind(Key.F3, () =>
            {
                Logging.LogDebug("F3: Toggle super speed");
                DebugUtils.ToggleSuperFastSpeed();
            });

            DI.Instance.InputManager.RegisterKeyBind(Key.F4, () =>
            {
                _logger.LogDebug("F4: Toggle invincibility");
                DebugUtils.InvincibilityEnabled = !DebugUtils.InvincibilityEnabled;
            });
#endif
            DI.Instance.InputManager.RegisterKeyBind(Key.F5, () =>
            {
                _logger.LogDebug("F5: Toggle debug widget visibility");
                DI.Instance.WidgetManager.ToggleDebugVisibility();
            });
#if DEBUG
            DI.Instance.InputManager.RegisterKeyBind(Key.F6, () =>
            {
                Logging.LogDebug("F6: Toggle HP scaling");
                DebugUtils.ScaleMonsterHpToHalf = !DebugUtils.ScaleMonsterHpToHalf;
            });

            DI.Instance.InputManager.RegisterKeyBind(Key.F7, () =>
            {
                Logging.LogDebug("F7: Force be hit animation");

                var localPlayer = DI.Instance.PlayerState.LocalMainCharacter?.GetLocalState().Pawn;

                const string beHitMontage = "Player/Wukong/AM/Behit/TeWaZa/LYS_KJLDragon/AM_LYS_KJLDragon_Atk_14_player";
                var fullMontagePath = Compressors.MontageNameCompressor.Decompress(beHitMontage);
                var montage = string.IsNullOrEmpty(fullMontagePath) ? null : BGW_PreloadAssetMgr.Get(GameUtils.GetWorld()).TryGetCachedResourceObj<UAnimMontage>(fullMontagePath, ELoadResourceType.SyncLoadAndCache);

                var events = BUS_EventCollectionCS.Get(localPlayer);
                var animInstance = localPlayer?.Mesh.GetAnimInstance();
                animInstance?.Montage_Play(montage);
                events.Evt_PlayMontageCallback.Invoke(EMontageBindReason.Default, montage, EMontageCallbackState.OnStarted);
            });

            DI.Instance.InputManager.RegisterKeyBind(Key.F8, () =>
            {
                Logging.LogDebug("F8: Force hit animation");

                var localPlayer = DI.Instance.PlayerState.LocalMainCharacter?.GetLocalState().Pawn;

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

            DI.Instance.InputManager.RegisterKeyBind(Key.F9, () =>
            {
                Logging.LogDebug("F9: Show colliders markers");
                DebugUtils.ShowMarkersForInvisibleWalls(4000);
            });

            DI.Instance.InputManager.RegisterKeyBind(Key.F10, () =>
            {
                Logging.LogDebug("F10: Remove colliders markers");
                DebugUtils.DestroyTmpMarkerActors();
            });

            DI.Instance.InputManager.RegisterKeyBind(Key.F12, () =>
            {
                _logger.LogDebug("F12: Skip cutscene");
                CutsceneUtils.RequestSkipCurrentCutscene();
            });

            DI.Instance.InputManager.RegisterKeyBind(ModifierKeys.Alt, Key.D0, () =>
            {
                Logging.LogDebug("Alt + 0");
                if (LaunchParameters.Instance.RecordShimFile != null)
                    DI.Instance.ShimController.Save(LaunchParameters.Instance.RecordShimFile!);
            });

            DI.Instance.InputManager.RegisterKeyBind(ModifierKeys.Alt, Key.C, () =>
            {
                _logger.LogDebug("Alt + C");
                try
                {
                    DI.Instance.NetLogger.DumpDebugInfo();
                    DI.Instance.RelayClient.LogEventStats();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while dumping debug info");
                }
            });

            DI.Instance.InputManager.RegisterKeyBind(ModifierKeys.Alt, Key.X, () =>
            {
                _logger.LogDebug("Alt + X");
                PlayerUtils.ResetLocalPlayerCooldown();
            });

            DI.Instance.InputManager.RegisterKeyBind(ModifierKeys.Alt, Key.J, () =>
            {
                _logger.LogDebug("Alt + J");
                DebugUtils.DumpPlayersAnimationDebugInfo();
            });

            DI.Instance.InputManager.RegisterKeyBind(ModifierKeys.Shift, Key.J, () =>
            {
                _logger.LogDebug("Shift + J");
                DebugUtils.DumpPlayersAnimationDebugInfo();
            });

            DI.Instance.InputManager.RegisterKeyBind(ModifierKeys.Alt, Key.K, () =>
            {
                _logger.LogDebug("Alt + K");
                DebugUtils.ResetPlayersAnimation();
            });
            
            DI.Instance.InputManager.RegisterKeyBind(Key.J, () =>
            {
                _logger.LogDebug("J");
                if (DI.Instance.WukongInputManager.CanApplyInput())
                    CutsceneUtils.TeleportLocalPlayerToCutsceneLocation();
            });
#endif

            DI.Instance.InputManager.RegisterKeyBind(Key.K, () =>
            {
                _logger.LogDebug("K");
                if (DI.Instance.WukongInputManager.CanApplyInput())
                    DI.Instance.WidgetManager.ToggleChatVisibility();
            });

            DI.Instance.InputManager.RegisterKeyBind(Key.F1, () =>
            {
                _logger.LogDebug("F1");
                if (DI.Instance.WukongInputManager.CanApplyInput())
                    DI.Instance.WidgetManager.ToggleCommandVisibility();
            });

            DI.Instance.InputManager.RegisterKeyBind(Key.UP, () =>
            {
                _logger.LogDebug("UP");
                DI.Instance.WidgetManager.CommandSelectUp();
            });

            DI.Instance.InputManager.RegisterKeyBind(Key.DOWN, () =>
            {
                _logger.LogDebug("DOWN");
                DI.Instance.WidgetManager.CommandSelectDown();
            });

            DI.Instance.InputManager.RegisterKeyBind(ModifierKeys.Alt, Key.DOWN, () =>
            {
                _logger.LogDebug("ALT + DOWN");
                DI.Instance.WidgetManager.CommandHistoryDown();
            });

            DI.Instance.InputManager.RegisterKeyBind(ModifierKeys.Alt, Key.UP, () =>
            {
                _logger.LogDebug("ALT + UP");
                DI.Instance.WidgetManager.CommandHistoryUp();
            });

            DI.Instance.InputManager.RegisterKeyBind(Key.ENTER, () =>
            {
                _logger.LogDebug("ENTER");
                DI.Instance.WukongInputManager.HandleEnterPressed();
            });
        }

        public void DeInit()
        {
            _logger.LogInformation("DeInit");

            if (!LaunchParameters.Instance.ValidForCoOp)
            {
                return;
            }

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

                if (CoopDI.Instance.Patcher.IsPatched)
                {
                    CoopDI.Instance.Patcher.Unpatch();
                }
            });
        }

        public object GetReloadContext()
        {
            _logger.LogInformation("GetReloadContext");
            return (bool?)DI.Instance.AreaState.InRoom;
        }

        public void Reload(object? context)
        {
            _logger.LogInformation("Reload");

            var connectedAndInRoom = context as bool?;
            if (connectedAndInRoom == true)
            {
                _logger.LogInformation("Reconnecting after a reload");
                DI.Instance.Connection.Reconnect();
            }
        }
    }
}