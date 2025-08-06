using System;
using System.Diagnostics;
using System.Reflection;
using CSharpModBase;
using CSharpModBase.Input;
using Microsoft.Extensions.Logging;
using ReadyM.Api.Multiplayer.ECS.Components;
using WukongMp.Api;
using WukongMp.Api.DTO;
using WukongMp.Api.Old;
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
            if (!CmdLineParams.Instance.ShouldEnableMultiplayer)
            {
                _logger.LogError("Multiplayer is disabled. Launch the game through the ReadyM Launcher to play WukongMP.");
                return;
            }

            DI.Instance.Init();

            if (CmdLineParams.Instance.PlayShimOnStart)
                ShimUtils.InitRelayPlayShim(
                    DI.Instance,
                    CmdLineParams.Instance.PlayShimFile!
                );
            else if (CmdLineParams.Instance.RecordShimOnStart)
                ShimUtils.InitRelayRecordShim(
                    DI.Instance,
                    CmdLineParams.Instance.ServerIp!,
                    CmdLineParams.Instance.ServerPort!.Value,
                    CmdLineParams.Instance.UserGuid,
                    CmdLineParams.Instance.RecordShimFile!
                );
            else
                ShimUtils.InitRelay(
                    DI.Instance,
                    CmdLineParams.Instance.ServerIp!,
                    CmdLineParams.Instance.ServerPort!.Value,
                    CmdLineParams.Instance.UserGuid
                );

            if (!DI.Instance.Patcher.IsPatched)
            {
                DI.Instance.Patcher.Patch();
            }
        }

        public void LateInit()
        {
            if (!CmdLineParams.Instance.ShouldEnableMultiplayer)
            {
                _logger.LogError("Multiplayer is disabled. Launch the game through the ReadyM Launcher to play WukongMP.");
                return;
            }

            _logger.LogInformation("Init WukongMP mod");

            // InformationalVersion from assembly def
            var trueModVersion = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

            _logger.LogInformation("Mod version: {Version}", trueModVersion);
            _logger.LogDebug("Process name: {ProcessName}", Process.GetCurrentProcess().ProcessName);

            Debug.Assert(DI.Instance.Patcher.IsPatched);
            
            if (!DI.Instance.Connection.IsRunning)
            {
                DI.Instance.Connection.Start();
            }
            else
            {
                _logger.LogInformation("WukongMP is already initialized");
                return;
            }

#if DEBUG
            Utils.RegisterKeyBind(ModifierKeys.Alt, Key.Y, () => 
            { 
                Logging.LogWarning("Alt + Y: Disable threading");
                GameUtils.DisableThreading();
            });

            Utils.RegisterKeyBind(ModifierKeys.Alt, Key.U, () => 
            {
                Logging.LogWarning("Alt + U: Enable threading");
                GameUtils.EnableThreading();
            });

            Utils.RegisterKeyBind(ModifierKeys.Alt, Key.D0, () =>
            {
                Logging.LogDebug("Alt + 0");
                if (CmdLineParams.Instance.RecordShimFile != null)
                    DI.Instance.ShimController.Save(CmdLineParams.Instance.RecordShimFile!);
            });
            
            Utils.RegisterKeyBind(ModifierKeys.Alt, Key.C, () =>
            {
                _logger.LogDebug("Alt + C");
                try
                {
                    DI.Instance.NetLogger.DumpDebugInfo();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while dumping debug info");
                }
            });

            Utils.RegisterKeyBind(ModifierKeys.Alt, Key.S, () =>
            {
                _logger.LogDebug("Alt + S");
                CutsceneUtils.SkipCurrentCutscene();
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
                    CutsceneUtils.TeleportLocalPlayerToCutsceneLocation();
            });

            Utils.RegisterKeyBind(Key.K, () =>
            {
                _logger.LogDebug("K");
                if (!ChatWidget.Instance.HasFocus())
                    ChatWidget.Instance.ToggleVisibility();
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

            if (!CmdLineParams.Instance.ShouldEnableMultiplayer)
            {
                return;
            }

            if (DI.Instance.Patcher.IsPatched)
            {
                DI.Instance.Patcher.Unpatch();
            }

            if (DI.Instance.Connection.IsRunning)
            {
                DI.Instance.Connection.Stop();
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