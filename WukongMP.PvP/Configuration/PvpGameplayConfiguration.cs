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
            _configuration.SyncTamerTeamFromGameToEcs = false;

            _configuration.DisableTamerAttackQuery += ShouldDisableTamerAttack;
            _configuration.IsSkillEnabledQuery += IsSkillEnabled;
        }

        public void Dispose()
        {
            _configuration.DisableTamerAttackQuery -= ShouldDisableTamerAttack;
            _configuration.IsSkillEnabledQuery -= IsSkillEnabled;
        }

        private bool ShouldDisableTamerAttack()
        {
            return _areaState.PvpState is { InPvP: false };
        }

        private bool IsSkillEnabled(int skillId)
        {
            var areaEntity = _areaState.CurrentArea;
            if (areaEntity == null)
                return true;

            // Only Immobilize checked here, Phantom Rush is not a skill in code
            if (skillId == Constants.ImmobilizeSkillId && !areaEntity.Value.GetRoom().ImmobilizeAllowed)
                return false;

            // more skills here
            return true;
        }
    }
}
