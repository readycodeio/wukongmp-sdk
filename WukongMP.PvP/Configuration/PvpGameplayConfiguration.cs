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
            _configuration.OverrideLocalPlayerTeamFromGlobalEntity = true;

            _configuration.SetDisableTamerAttackQuery(ShouldDisableTamerAttack);
            _configuration.SetIsSkillEnabledQuery(IsSkillEnabled);

            _configuration.EnableCustomIsPlayerInBattle = true;
            _configuration.SetIsPlayerInBattleQuery(() => _areaState.PvpState?.InPvP ?? false);
        }

        public void Dispose()
        {
            _configuration.ClearDisableTamerAttackQuery();
            _configuration.ClearIsSkillEnabledQuery();
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

            var room = areaEntity.Value.GetRoom();

            switch (skillId)
            {
                // Note: Phantom Rush is not a skill in code
                case Constants.ImmobilizeSkillId when !room.ImmobilizeAllowed:
                case Constants.GourdSkillId when !room.GourdAllowed:
                case Constants.ConsumableBuffSkillId when !room.ConsumablesAllowed:
                case Constants.IncenseTrailTalismanSkillId:
                    return false;
                default:
                    // more skills here
                    return true;
            }
        }
    }
}
