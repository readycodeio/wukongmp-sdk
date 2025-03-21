using System;
using System.Diagnostics;
using System.Reflection;
using CSharpModBase;
using CSharpModBase.Input;
using WukongApi;
using WukongApi.UI;

namespace WukongMPMod
{
    // ReSharper disable once UnusedType.Global
    public class Mod : ICSharpMod
    {
        public string Name => "WukongMP";
        public string Version => "1.0.0";

        private WukongMP _wukongMp = null!; // initialized in Init

        public void Init()
        {
            // register global unhandled exception handler
            AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionHandler;

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

            if (!CmdLineParams.Instance.ShouldEnableMultiplayer)
            {
                Logging.LogInformation("Multiplayer is disabled");
                return;
            }

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
                WukongMP.ResetLocalPlayerCooldown();
            });
#endif
            Utils.RegisterKeyBind(Key.J, () =>
            {
                Logging.LogDebug("J");
                if (!ChatWidget.Instance.HasFocus())
                    _wukongMp.Photon.SwitchReadyState();
            });

            Utils.RegisterKeyBind(Key.L, () =>
            {
                Logging.LogDebug("L");
                if (!ChatWidget.Instance.HasFocus())
                    _wukongMp.Photon.SwitchTeam();
            });

            Utils.RegisterKeyBind(Key.K, () =>
            {
                Logging.LogDebug("K");
                if (!ChatWidget.Instance.HasFocus())
                    ChatWidget.Instance.ToggleVisibility();
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
                    ChatWidget.Instance.SetInputFocus();
                else
                    ChatWidget.Instance.CommitMessage();
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