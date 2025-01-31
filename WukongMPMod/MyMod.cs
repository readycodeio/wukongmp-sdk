using CSharpModBase;
using CSharpModBase.Input;
using WukongCSharpMod;

namespace WukongMPMod
{
    public class MyMod : ICSharpMod
    {
        public string Name => "WukongMP";
        public string Version => "0.0.1";

        public WukongMP WukongMP;
        
        public void Init()
        {
            Logging.LogDebug("Init");

            WukongMP = WukongCSharpMod.WukongMP.Instance;
            WukongMP.Patch();
            
            // InitWorldCallbacks();

            Utils.RegisterKeyBind(ModifierKeys.Alt, Key.H, () =>
            {
                Logging.LogDebug("Alt + H");

                WukongMP.Init();
            });

            Utils.RegisterKeyBind(ModifierKeys.Alt, Key.C, () =>
            {
                Logging.LogDebug("Alt + C");

                WukongMP.DumpPlayerState();
            });

            Utils.RegisterKeyBind(ModifierKeys.Alt, Key.V, () =>
            {
                Logging.LogDebug("Alt + V");

                WukongMP.SpawnClone();
            });

            Utils.RegisterKeyBind(ModifierKeys.Alt, Key.M, () =>
            {
                Logging.LogDebug("Alt + M");

                WukongMP.EnableMultiplayer();
            });
        }

        public void DeInit()
        {
            Logging.LogDebug("DeInit");
            
            WukongMP.Unpatch();
        }
    }
}