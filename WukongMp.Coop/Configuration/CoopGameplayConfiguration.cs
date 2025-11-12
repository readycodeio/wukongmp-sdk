using WukongMp.Api.Configuration;

namespace WukongMp.Coop.Configuration
{
    internal class CoopGameplayConfiguration
    {
        private readonly GameplayConfiguration _configuration;

        public CoopGameplayConfiguration(GameplayConfiguration configuration)
        {
            _configuration = configuration;
            ConfigureCoopGameplay();
        }

        public void ConfigureCoopGameplay()
        {
            _configuration.IsSupportMultiLockEnabled = true;
            _configuration.IsStrongDamageImmueEnabled = false;
            _configuration.EnableCustomCameraArmLength = false;
            _configuration.EnableSpawnedTamers = false;
        }
    }
}
