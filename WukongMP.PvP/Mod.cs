using CSharpModBase;
using CSharpModBase.Input;
using System.Diagnostics;
using System.Reflection;
using Microsoft.Extensions.Logging;
using WukongMp.Api;
using WukongMp.Api.Configuration;
using WukongMp.Api.DTO;
using WukongMp.Api.Shim;
using WukongMp.Api.UI;
using WukongMp.Api.WukongUtils;
using WukongMp.Api.Input;

namespace WukongMp.PvP
{
    // ReSharper disable once UnusedType.Global
    public class Mod : ICSharpModExV2
    {
        public string Name => "WukongMp PvP";
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

            if (!LaunchParameters.Instance.ValidForPvP)
            {
                _logger.LogDebug("Pvp not launching.");
                return;
            }

            DI.Instance.Init();
            PvpDI.Instance.Init(DI.Instance);

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

            if (!PvpDI.Instance.Patcher.IsPatched)
            {
                PvpDI.Instance.Patcher.Patch();
            }
        }

        public void LateInit()
        {
            if (!LaunchParameters.Instance.Valid)
            {
                _logger.LogError("Multiplayer is disabled. Launch the game through the ReadyM Launcher to play WukongMP.");
                return;
            }

            if (!LaunchParameters.Instance.ValidForPvP)
            {
                _logger.LogDebug("Pvp not launching.");
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
                Debug.Assert(PvpDI.Instance.Patcher.IsPatched);

                if (!DI.Instance.Connection.IsRunning)
                {
                    DI.Instance.EcsLoop.Start();
                    DI.Instance.Connection.Start();
                }
                else
                {
                    _logger.LogInformation("WukongMP is already initialized");
                    return;
                }

                if (!DI.Instance.Connection.RequestedConnect)
                {
                    DI.Instance.Connection.Connect();
                }
            });

            if (!DI.Instance.Connection.RequestedConnect)
            {
                DI.Instance.Connection.Connect();
            }

#if DEBUG
            DI.Instance.InputManager.RegisterKeyBind(ModifierKeys.Alt, Key.D0, () =>
            {
                Logging.LogDebug("Alt + 0");
                if (LaunchParameters.Instance.RecordShimFile != null)
                    DI.Instance.ShimController.Save(LaunchParameters.Instance.RecordShimFile!);
            });

            DI.Instance.InputManager.RegisterKeyBind(ModifierKeys.Alt, Key.C, () =>
            {
                _logger.LogDebug("Alt + C");
                DI.Instance.NetLogger.DumpDebugInfo();
            });

            DI.Instance.InputManager.RegisterKeyBind(ModifierKeys.Alt, Key.X, () =>
            {
                _logger.LogDebug("Alt + X");
                PlayerUtils.ResetLocalPlayerCooldown();
            });

            DI.Instance.InputManager.RegisterKeyBind(ModifierKeys.Alt, Key.J, () =>
            {
                _logger.LogDebug("Alt + J");

                var mainEntity = DI.Instance.PlayerState.LocalMainCharacter;
                if (mainEntity == null)
                    return;

                DI.Instance.Rpc.OnMontageCallback(new MontageCallbackData(mainEntity.Value.GetMeta().NetId, true, "Player/Wukong/AM/Attack/ComboB/AM_wukong_combob_z_02_weak", 0f, false));
            });

            DI.Instance.InputManager.RegisterKeyBind(ModifierKeys.Alt, Key.K, () =>
            {
                _logger.LogDebug("Alt + K");

                var mainEntity = DI.Instance.PlayerState.LocalMainCharacter;
                if (mainEntity == null)
                    return;

                DI.Instance.Rpc.OnMontageCallback(new MontageCallbackData(mainEntity.Value.GetMeta().NetId, true, "Player/Wukong/AM/Attack/ComboB/AM_wukong_combob_z_02", 0f, false));
            });
#endif
            DI.Instance.InputManager.RegisterKeyBind(Key.F5, () =>
            {
                _logger.LogDebug("F5");
                if (DI.Instance.WukongInputManager.CanApplyInput())
                    DI.Instance.WidgetManager.ToggleDebugVisibility();
            });

            DI.Instance.InputManager.RegisterKeyBind(Key.J, () =>
            {
                _logger.LogDebug("J");
                if (DI.Instance.WukongInputManager.CanApplyInput())
                    PvpDI.Instance.PVP.SwitchReadyStateMulti();
            });

            DI.Instance.InputManager.RegisterKeyBind(Key.L, () =>
            {
                _logger.LogDebug("L");
                if (DI.Instance.WukongInputManager.CanApplyInput())
                    PvpDI.Instance.PVP?.SwitchTeam();
            });

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

            DI.Instance.InputManager.RegisterKeyBind(Key.TAB, () =>
            {
                _logger.LogDebug("TAB");
                DI.Instance.WidgetManager.CommandSelectSuggestion();
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
                if (PvpDI.Instance.Patcher.IsPatched)
                {
                    PvpDI.Instance.Patcher.Unpatch();
                }

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