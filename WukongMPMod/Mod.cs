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
            Logging.LogDebug("Init WukongMP mod");
            Logging.LogDebug("Process name: {ProcessName}", Process.GetCurrentProcess().ProcessName);

            _wukongMp = WukongMP.Instance;

            if (_wukongMp.IsInitialized)
            {
                Logging.LogDebug("WukongMP is already initialized");
                return;
            }

            _wukongMp.Init();

            if (!CmdLineParams.Instance.ShouldEnableMultiplayer)
            {
                Logging.LogDebug("Multiplayer is disabled");
                return;
            }

            _wukongMp.Patch();

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

            //Utils.RegisterKeyBind(ModifierKeys.Alt, Key.V, () =>
            //{
            //    Logging.LogDebug("Alt + V");
            //    _wukongMp.Photon.SpawnClone();
            //});

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
            Logging.LogDebug("DeInit");
            _wukongMp.Unpatch();
            _wukongMp.DeInit();
            Logger.Instance.Dispose();
        }
    }
}