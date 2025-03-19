using System;
using CSharpModBase;
using CSharpModBase.Input;
using System.Diagnostics;
using Photon.Realtime;
using WukongApi;

namespace WukongMPMod
{
    // ReSharper disable once UnusedType.Global
    public class Mod : ICSharpMod
    {
        public string Name => "WukongMP";
        public string Version => "1.0.0";

        private WukongMP _wukongMp;

        public void Init()
        {
            Logging.LogInformation("Init WukongMP mod");
            Logging.LogDebug("Process name: {ProcessName}", Process.GetCurrentProcess().ProcessName);

            _wukongMp = WukongMP.Instance;

            if (_wukongMp.IsInitialized)
            {
                Logging.LogInformation("WukongMP is already initialized");
                return;
            }

            _wukongMp.Init();

            if (!CmdLineParams.Instance.ShouldEnableMultiplayer)
            {
                Logging.LogInformation("Multiplayer is disabled");
                return;
            }

            // register global unhandled exception handler
            AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionHandler;

            _wukongMp.Patch();
#if DEBUG
            Utils.RegisterKeyBind(ModifierKeys.Alt, Key.C, () =>
            {
                Logging.LogDebug("Alt + C");
                _wukongMp.DumpPlayerState();
            });

            Utils.RegisterKeyBind(ModifierKeys.Alt, Key.X, () =>
            {
                Logging.LogDebug("Alt + X");
                _wukongMp.ResetLocalPlayerCooldown();
            });
#endif
            Utils.RegisterKeyBind(Key.J, () =>
            {
                Logging.LogDebug("J");
                if (!_wukongMp.ChatWidget.HasFocus())
                    _wukongMp.Photon.SwitchReadyState();
            });

            Utils.RegisterKeyBind(Key.L, () =>
            {
                Logging.LogDebug("L");
                if (!_wukongMp.ChatWidget.HasFocus())
                    _wukongMp.Photon.SwitchTeam();
            });

            Utils.RegisterKeyBind(Key.K, () =>
            {
                Logging.LogDebug("K");
                if (!_wukongMp.ChatWidget.HasFocus())
                    _wukongMp.ChatWidget.ToggleVisibility();
            });

            Utils.RegisterKeyBind(Key.UP, () =>
            {
                Logging.LogDebug("UP");
                _wukongMp.ChatWidget.SetHistoryNext();
            });

            Utils.RegisterKeyBind(Key.DOWN, () =>
            {
                Logging.LogDebug("DOWN");
                _wukongMp.ChatWidget.SetHistoryPrev();
            });
        }

        public void DeInit()
        {
            Logging.LogInformation("DeInit");
            _wukongMp.Unpatch();
            _wukongMp.DeInit();
            AppDomain.CurrentDomain.UnhandledException -= UnhandledExceptionHandler;
            Logger.Instance.Dispose();
        }

        private static void UnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs args)
        {
            Logging.LogCriticalException((Exception)args.ExceptionObject);
        }
    }
}