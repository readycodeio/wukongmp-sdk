using CSharpModBase;
using CSharpModBase.Input;
using System.Diagnostics;
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
                _wukongMp.Photon.SwitchReadyState();
            });

            Utils.RegisterKeyBind(Key.L, () =>
            {
                Logging.LogDebug("L");
                _wukongMp.Photon.SwitchTeam();
            });
        }

        public void DeInit()
        {
            Logging.LogInformation("DeInit");
            _wukongMp.Unpatch();
            _wukongMp.DeInit();
            Logger.Instance.Dispose();
        }
    }
}