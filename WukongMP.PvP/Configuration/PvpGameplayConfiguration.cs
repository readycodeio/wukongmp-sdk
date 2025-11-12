using System;
using WukongMp.Api.Configuration;
using WukongMp.Api.State;

namespace WukongMp.PvP.Configuration
{
    internal class PvpGameplayConfiguration : IDisposable
    {
        private readonly GameplayConfiguration _configuration;
        private readonly WukongAreaState _areaState;

        public PvpGameplayConfiguration(GameplayConfiguration configuration, WukongAreaState areaState)
        {
            _configuration = configuration;
            _areaState = areaState;

            ConfigurePvpGameplay();
        }

        public void ConfigurePvpGameplay()
        {
            _configuration.IsSupportMultiLockEnabled = false;
            _configuration.IsStrongDamageImmueEnabled = true;
            _configuration.EnableCustomCameraArmLength = true;
            _configuration.EnableSpawnedTamers = true;

            _configuration.DisableTamerAttackQuery += ShouldDisableTamerAttack;
        }

        public void Dispose()
        {
            _configuration.DisableTamerAttackQuery -= ShouldDisableTamerAttack;
        }

        private bool ShouldDisableTamerAttack()
        {
            return _areaState.PvpState.HasValue && !_areaState.PvpState.Value.InPvP;
        }
    }
}
