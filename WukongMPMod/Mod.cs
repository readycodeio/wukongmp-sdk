using CSharpModBase;
using CSharpModBase.Input;
using WukongApi;

namespace WukongMPMod
{
    // ReSharper disable once UnusedType.Global
    public class Mod : ICSharpMod
    {
        public string Name => "WukongMP";
        public string Version => "0.0.1";

        private WukongMP _wukongMp;

        public void Init()
        {
            Logging.LogDebug("Init WukongMP mod");

            _wukongMp = WukongMP.Instance;

            _wukongMp.InitAsync();

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
        }

        public void DeInit()
        {
            Logging.LogDebug("DeInit");
            _wukongMp.Unpatch();
        }
    }
}