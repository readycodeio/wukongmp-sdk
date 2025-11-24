using System;
using System.Diagnostics;
using System.Reflection;
using CSharpModBase;
using CSharpModBase.Input;
using Microsoft.Extensions.Logging;
using WukongMp.Api;
using WukongMp.Api.Configuration;
using WukongMp.Api.Shim;
using WukongMp.Api.UI;
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
            DI.Instance.InputManager.RegisterKeyBind(ModifierKeys.Alt, Key.L, () =>
            {
                Logging.LogDebug("Alt + L: Teleport underground");
                PlayerUtils.MoveAllOtherPlayersUnderGround();
            });

            DI.Instance.InputManager.RegisterKeyBind(ModifierKeys.Alt, Key.Y, () =>
            {
                Logging.LogDebug("Alt + Y: Show colliders markers");
                DebugUtils.ShowMarkersForInvisibleWalls(4000);
            });

            DI.Instance.InputManager.RegisterKeyBind(ModifierKeys.Alt, Key.U, () =>
            {
                Logging.LogDebug("Alt + U: Remove colliders markers");
                DebugUtils.DestroyTmpMarkerActors();
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

            DI.Instance.InputManager.RegisterKeyBind(ModifierKeys.Alt, Key.S, () =>
            {
                _logger.LogDebug("Alt + S");
                CutsceneUtils.RequestSkipCurrentCutscene();
            });

            DI.Instance.InputManager.RegisterKeyBind(ModifierKeys.Alt, Key.X, () =>
            {
                _logger.LogDebug("Alt + X");
                PlayerUtils.ResetLocalPlayerCooldown();
            });

            DI.Instance.InputManager.RegisterKeyBind(Key.J, () =>
            {
                _logger.LogDebug("J (Dump anim info)");
                DebugUtils.DumpTamerAnimationDebugInfo("JiRuHuo");
            });

            DI.Instance.InputManager.RegisterKeyBind(ModifierKeys.Alt, Key.J, () =>
            {
                _logger.LogDebug("Alt + J");
                DebugUtils.DumpTamerAnimationDebugInfo("JiRuHuo");
            });

            DI.Instance.InputManager.RegisterKeyBind(ModifierKeys.Shift, Key.J, () =>
            {
                _logger.LogDebug("Shift + J");
                DebugUtils.DumpTamerAnimationDebugInfo("JiRuHuo");
            });

            DI.Instance.InputManager.RegisterKeyBind(ModifierKeys.Alt, Key.K, () =>
            {
                _logger.LogDebug("Alt + K");
                DebugUtils.ResetPlayersAnimation();
            });
#endif
            DI.Instance.InputManager.RegisterKeyBind(Key.F5, () =>
            {
                _logger.LogDebug("F5");
                if (!DI.Instance.WidgetManager.ChatHasFocus())
                    DI.Instance.WidgetManager.ToggleDebugVisibility();
            });

            DI.Instance.InputManager.RegisterKeyBind(Key.J, () =>
            {
                _logger.LogDebug("J");
                if (!DI.Instance.WidgetManager.ChatHasFocus())
                    CutsceneUtils.TeleportLocalPlayerToCutsceneLocation();
            });

            DI.Instance.InputManager.RegisterKeyBind(Key.K, () =>
            {
                _logger.LogDebug("K");
                if (!DI.Instance.WidgetManager.ChatHasFocus())
                    DI.Instance.WidgetManager.ToggleChatVisibility();
            });

            DI.Instance.InputManager.RegisterKeyBind(Key.UP, () =>
            {
                _logger.LogDebug("UP");
                DI.Instance.WidgetManager.SetChatHistoryNext();
            });

            DI.Instance.InputManager.RegisterKeyBind(Key.DOWN, () =>
            {
                _logger.LogDebug("DOWN");
                DI.Instance.WidgetManager.SetChatHistoryPrev();
            });

            DI.Instance.InputManager.RegisterKeyBind(Key.ENTER, () =>
            {
                _logger.LogDebug("ENTER");
                if (!DI.Instance.WidgetManager.ChatHasFocus())
                {
                    DI.Instance.WidgetManager.SetChatInputFocus();
                }
                else
                {
                    var message = DI.Instance.WidgetManager.CommitChatMessage();
                    DI.Instance.Chatter.ProcessMessage(message);
                }
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