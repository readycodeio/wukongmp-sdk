using System;

namespace WukongMp.Api.Configuration
{
    public class GameplayConfiguration
    {
        public bool IsSupportMultiLockEnabled { get; set; } = false;
        public bool IsStrongDamageImmueEnabled { get; set; } = false;
        public bool EnableCustomCameraArmLength { get; set; } = false;
        public bool EnableSpawnedTamers { get; set; } = false;

        [Obsolete("To be replaced by data sync direction after refactoring")]
        public bool SyncTamerTeamFromGameToEcs { get; set; } = false;

        [Obsolete("To be replaced by data sync direction after refactoring")]
        public bool OverrideLocalPlayerTeamFromGlobalEntity { get; set; } = false;

        private Func<bool>? DisableTamerAttackQuery;
        public void SetDisableTamerAttackQuery(Func<bool> query)
        {
            DisableTamerAttackQuery = query;
        }
        public void ClearDisableTamerAttackQuery()
        {
            DisableTamerAttackQuery = null;
        }
        public bool ShouldDisableTamerAttack() => DisableTamerAttackQuery?.Invoke() ?? false;

        private Func<int, bool>? IsSkillEnabledQuery;
        public void SetIsSkillEnabledQuery(Func<int, bool> query)
        {
            IsSkillEnabledQuery = query;
        }
        public void ClearIsSkillEnabledQuery()
        {
            IsSkillEnabledQuery = null;
        }
        public bool IsSkillEnabled(int skillId) => IsSkillEnabledQuery?.Invoke(skillId) ?? true;
    }
}