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
            Logging.LogInformation("Example mod Init");
        }

        public void DeInit()
        {
            Logging.LogInformation("Example mod DeInit");
        }
    }
}