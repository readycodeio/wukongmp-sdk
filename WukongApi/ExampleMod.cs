using CSharpModBase;

namespace WukongApi
{
    // NOTE: Keep this empty as this is a dependency for multiple mods
    public class ExampleMod : ICSharpMod
    {
        public string Name => "ExampleMod";
        public string Version => "0.0.1";

        public void Init()
        {
            Logging.LogDebug("Example mod Init");
        }

        public void DeInit()
        {
            Logging.LogDebug("Example mod DeInit");
        }
    }
}