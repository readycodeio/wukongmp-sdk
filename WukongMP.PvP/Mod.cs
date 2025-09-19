using CSharpModBase;
using CSharpModBase.Input;
using System.Diagnostics;
using System.Reflection;
using Microsoft.Extensions.Logging;
using WukongMp.Api;
using WukongMp.Api.DTO;
using WukongMp.Api.Shim;
using WukongMp.Api.UI;
using WukongMp.Api.WukongUtils;

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
            if (!LaunchParameters.Instance.ShouldEnableMultiplayer)
            {
                _logger.LogError("Multiplayer is disabled. Launch the game through the ReadyM Launcher to play WukongMP.");
                return;
            }

            DI.Instance.Init();

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
                    false,
                    LaunchParameters.Instance.RecordShimFile!
                );
            else
                ShimUtils.InitRelay(
                    DI.Instance,
                    LaunchParameters.Instance.ServerIp!,
                    LaunchParameters.Instance.ServerPort!.Value,
                    LaunchParameters.Instance.UserGuid,
#if NO_DISCONNECT
                    true,
#else
                    false,
#endif
#if DEBUG
                    true
#else
                    false
#endif
                );

            if (!DI.Instance.Patcher.IsPatched)
            {
                DI.Instance.Patcher.Patch();
            }
        }

        public void LateInit()
        {
            if (!LaunchParameters.Instance.ShouldEnableMultiplayer)
            {
                _logger.LogError("Multiplayer is disabled. Launch the game through the ReadyM Launcher to play WukongMP.");
                return;
            }

            _logger.LogInformation("Init WukongMP mod");

            // InformationalVersion from assembly def
            var trueModVersion = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

            _logger.LogInformation("Mod version: {Version}", trueModVersion);
            _logger.LogInformation("Process name: {ProcessName}", Process.GetCurrentProcess().ProcessName);

            Debug.Assert(DI.Instance.Patcher.IsPatched);

            if (!DI.Instance.Connection.IsRunning)
            {
                DI.Instance.EcsLoop.Start();
                DI.Instance.ShimEcsLoop.Start();
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

            if (!DI.Instance.Connection.RequestedConnect)
            {
                DI.Instance.Connection.Connect();
            }

#if DEBUG
            Utils.RegisterKeyBind(ModifierKeys.Alt, Key.D0, () =>
            {
                Logging.LogDebug("Alt + 0");
                if (LaunchParameters.Instance.RecordShimFile != null)
                    DI.Instance.ShimController.Save(LaunchParameters.Instance.RecordShimFile!);
            });

            Utils.RegisterKeyBind(ModifierKeys.Alt, Key.C, () =>
            {
                _logger.LogDebug("Alt + C");
                DI.Instance.NetLogger.DumpDebugInfo();
            });

            Utils.RegisterKeyBind(ModifierKeys.Alt, Key.X, () =>
            {
                _logger.LogDebug("Alt + X");
                PlayerUtils.ResetLocalPlayerCooldown();
            });

            Utils.RegisterKeyBind(ModifierKeys.Alt, Key.J, () =>
            {
                _logger.LogDebug("Alt + J");

                var mainEntity = DI.Instance.PlayerState.LocalMainCharacter;
                if (mainEntity == null)
                    return;

                DI.Instance.Rpc.OnMontageCallback(new MontageCallbackData(mainEntity.Value.GetMeta().NetId, true, "Player/Wukong/AM/Attack/ComboB/AM_wukong_combob_z_02_weak", 0f, false));
            });

            Utils.RegisterKeyBind(ModifierKeys.Alt, Key.K, () =>
            {
                _logger.LogDebug("Alt + K");

                var mainEntity = DI.Instance.PlayerState.LocalMainCharacter;
                if (mainEntity == null)
                    return;

                DI.Instance.Rpc.OnMontageCallback(new MontageCallbackData(mainEntity.Value.GetMeta().NetId, true, "Player/Wukong/AM/Attack/ComboB/AM_wukong_combob_z_02", 0f, false));
            });
#endif
            Utils.RegisterKeyBind(Key.J, () =>
            {
                _logger.LogDebug("J");
                if (!ChatWidget.Instance.HasFocus())
                    DI.Instance.PVP?.SwitchReadyStateMulti();
            });

            Utils.RegisterKeyBind(Key.L, () =>
            {
                _logger.LogDebug("L");
                if (!ChatWidget.Instance.HasFocus())
                    DI.Instance.PVP?.SwitchTeam();
            });

            Utils.RegisterKeyBind(Key.K, () =>
            {
                _logger.LogDebug("K");
                if (!ChatWidget.Instance.HasFocus())
                    ChatWidget.Instance.ToggleVisibility();
            });

            Utils.RegisterKeyBind(Key.I, () =>
            {
                _logger.LogDebug("I");
                if (!ChatWidget.Instance.HasFocus())
                    DI.Instance.PVP?.SwitchReadyStateSingle();
            });

            Utils.RegisterKeyBind(Key.UP, () =>
            {
                _logger.LogDebug("UP");
                ChatWidget.Instance.SetHistoryNext();
            });

            Utils.RegisterKeyBind(Key.DOWN, () =>
            {
                _logger.LogDebug("DOWN");
                ChatWidget.Instance.SetHistoryPrev();
            });

            Utils.RegisterKeyBind(Key.ENTER, () =>
            {
                _logger.LogDebug("ENTER");
                if (!ChatWidget.Instance.HasFocus())
                {
                    ChatWidget.Instance.SetInputFocus();
                }
                else
                {
                    var message = ChatWidget.Instance.CommitMessage();
                    DI.Instance.Chatter.ProcessMessage(message);
                }
            });
        }

        public void DeInit()
        {
            _logger.LogInformation("DeInit");

            if (!LaunchParameters.Instance.ShouldEnableMultiplayer)
            {
                return;
            }

            if (DI.Instance.Patcher.IsPatched)
            {
                DI.Instance.Patcher.Unpatch();
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