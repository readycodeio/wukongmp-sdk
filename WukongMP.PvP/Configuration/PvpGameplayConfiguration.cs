using WukongMp.Api.Configuration;

namespace WukongMp.PvP.Configuration
{
    internal class PvpGameplayConfiguration
    {
        private readonly GameplayConfiguration _configuration;

        public PvpGameplayConfiguration(GameplayConfiguration configuration)
        {
            _configuration = configuration;
            ConfigurePvpGameplay();
        }

        public void ConfigurePvpGameplay()
        {
            _configuration.IsSupportMultiLockEnabled = false;
            _configuration.IsStrongDamageImmueEnabled = true;
        }
    }
}
