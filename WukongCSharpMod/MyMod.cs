using CSharpModBase;

namespace WukongCSharpMod
{
    public class MyMod : ICSharpMod
    {
        public string Name => "WukongCSharpMod";
        public string Version => "0.0.1";

        public void Init()
        {
            Logging.LogDebug("Init");
        }

        public void DeInit()
        {
            Logging.LogDebug("DeInit");
        }
        
        // NOTE: Keep this empty as this is a dependency for multiple mods
    }
}
