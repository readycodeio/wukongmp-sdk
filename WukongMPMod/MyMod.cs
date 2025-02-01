using CSharpModBase;
using CSharpModBase.Input;
using WukongCSharpMod;

namespace WukongMPMod
{
    public class MyMod : ICSharpMod
    {
        public string Name => "WukongMP";
        public string Version => "0.0.1";

        private WukongMP _wukongMp;

        public void Init()
        {
            Logging.LogDebug("Init");

            _wukongMp = WukongMP.Instance;

            Utils.RegisterKeyBind(ModifierKeys.Alt, Key.H, () =>
            {
                Logging.LogDebug("Alt + H");
                _wukongMp.Init();
            });

            Utils.RegisterKeyBind(ModifierKeys.Alt, Key.C, () =>
            {
                Logging.LogDebug("Alt + C");
                _wukongMp.DumpPlayerState();
            });

            Utils.RegisterKeyBind(ModifierKeys.Alt, Key.V, () =>
            {
                Logging.LogDebug("Alt + V");
                _wukongMp.Photon.SpawnClone();
            });

            Utils.RegisterKeyBind(ModifierKeys.Alt, Key.M, () =>
            {
                Logging.LogDebug("Alt + M");
                _wukongMp.EnableMultiplayer();
            });
        }

        public void DeInit()
        {
            Logging.LogDebug("DeInit");
            _wukongMp.Unpatch();
        }
    }
}