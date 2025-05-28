using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using CSharpModBase;
using CSharpModBase.Input;
using ReadyM.Relay.Common.ECS;
using WukongMp.Api;
using WukongMp.Api.UI;

namespace WukongMp.Coop
{
    // ReSharper disable once UnusedType.Global
    public class Mod : ICSharpModEx
    {
        public string Name => "WukongMp co-op";
        public string Version => "1.0.0";

        private WukongMP _wukongMp = null!; // initialized in Init

        public void Init()
        {
            if (!CmdLineParams.Instance.ShouldEnableMultiplayer)
            {
                Logging.LogError("Multiplayer is disabled. Launch the game through the ReadyM Launcher to play WukongMP.");
                return;
            }

            // register global unhandled exception handlers
            AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionHandler;
            TaskScheduler.UnobservedTaskException += UnobservedTaskExceptionHandler;

            Logging.LogInformation("Init WukongMP mod");

            // InformationalVersion from assembly def
            var trueModVersion = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

            Logging.LogInformation("Mod version: {Version}", trueModVersion);
            Logging.LogDebug("Process name: {ProcessName}", Process.GetCurrentProcess().ProcessName);

            try
            {
                _wukongMp = WukongMP.Instance;
            }
            catch (Exception e)
            {
                Logging.LogException(e);
                return;
            }

            if (_wukongMp.IsInitialized)
            {
                Logging.LogInformation("WukongMP is already initialized");
                return;
            }

            _wukongMp.Init();

            _wukongMp.Patch();
#if DEBUG
            Utils.RegisterKeyBind(ModifierKeys.Alt, Key.B, () => { Logging.LogDebug("Alt + B: Test"); });

            Utils.RegisterKeyBind(ModifierKeys.Alt, Key.C, () =>
            {
                Logging.LogDebug("Alt + C");
                try
                {
                    _wukongMp.DumpDebugInfo();
                }
                catch (Exception e)
                {
                    Logging.LogException(e);
                }
            });

            Utils.RegisterKeyBind(ModifierKeys.Alt, Key.S, () =>
            {
                Logging.LogDebug("Alt + S");
                _wukongMp.SkipCutscene();
            });

            Utils.RegisterKeyBind(ModifierKeys.Alt, Key.X, () =>
            {
                Logging.LogDebug("Alt + X");
                WukongMP.ResetLocalPlayerCooldown();
            });

            Utils.RegisterKeyBind(ModifierKeys.Alt, Key.J, () =>
            {
                Logging.LogDebug("Alt + J");
                WukongMP.Instance.ApplyPlayerMontageCallback(new MontageCallbackData(NetworkIdComponent.FromPlayerPeerId(WukongMP.Instance.Client.LocalPlayerState.PeerId), true, "Player/Wukong/AM/Attack/ComboB/AM_wukong_combob_z_02_weak", 0f, false));
            });

            Utils.RegisterKeyBind(ModifierKeys.Alt, Key.K, () =>
            {
                Logging.LogDebug("Alt + K");
                WukongMP.Instance.ApplyPlayerMontageCallback(new MontageCallbackData(NetworkIdComponent.FromPlayerPeerId(WukongMP.Instance.Client.LocalPlayerState.PeerId), true, "Player/Wukong/AM/Attack/ComboB/AM_wukong_combob_z_02", 0f, false));
            });
#endif
            Utils.RegisterKeyBind(Key.J, () =>
            {
                Logging.LogDebug("J");
                if (!ChatWidget.Instance.HasFocus())
                    _wukongMp.Client.SwitchReadyStateMulti();
            });

            Utils.RegisterKeyBind(Key.L, () =>
            {
                Logging.LogDebug("L");
                if (!ChatWidget.Instance.HasFocus())
                    _wukongMp.Client.SwitchTeam();
            });

            Utils.RegisterKeyBind(Key.K, () =>
            {
                Logging.LogDebug("K");
                if (!ChatWidget.Instance.HasFocus())
                    ChatWidget.Instance.ToggleVisibility();
            });

            Utils.RegisterKeyBind(Key.I, () =>
            {
                Logging.LogDebug("I");
                if (!ChatWidget.Instance.HasFocus())
                    _wukongMp.Client.SwitchReadyStateSingle();
            });

            Utils.RegisterKeyBind(Key.UP, () =>
            {
                Logging.LogDebug("UP");
                ChatWidget.Instance.SetHistoryNext();
            });

            Utils.RegisterKeyBind(Key.DOWN, () =>
            {
                Logging.LogDebug("DOWN");
                ChatWidget.Instance.SetHistoryPrev();
            });

            Utils.RegisterKeyBind(Key.ENTER, () =>
            {
                Logging.LogDebug("ENTER");
                if (!ChatWidget.Instance.HasFocus())
                {
                    ChatWidget.Instance.SetInputFocus();
                }
                else
                {
                    var message = ChatWidget.Instance.CommitMessage();
                    _wukongMp.Client.WukongChat.ProcessMessage(message);
                }
            });
        }

        public void DeInit()
        {
            Logging.LogInformation("DeInit");

            if (!CmdLineParams.Instance.ShouldEnableMultiplayer)
            {
                return;
            }

            _wukongMp.Unpatch();
            _wukongMp.DeInit();
            AppDomain.CurrentDomain.UnhandledException -= UnhandledExceptionHandler;
            TaskScheduler.UnobservedTaskException -= UnobservedTaskExceptionHandler;
            Logger.Instance.Dispose();
        }

        public object GetReloadContext()
        {
            Logging.LogInformation("GetReloadContext");
            return (bool?)_wukongMp.Client.ConnectedAndInRoom;
        }

        public void Reload(object? context)
        {
            Logging.LogInformation("Reload");

            var connectedAndInRoom = context as bool?;
            if (connectedAndInRoom == true)
            {
                Logging.LogInformation("Reconnecting after a reload");
                _wukongMp.Reload();
            }
        }

        private static void UnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs args)
        {
            Logging.LogCriticalException((Exception)args.ExceptionObject);
        }

        private static void UnobservedTaskExceptionHandler(object sender, UnobservedTaskExceptionEventArgs args)
        {
            Logging.LogCriticalException(args.Exception);
            args.SetObserved();
        }
    }
}