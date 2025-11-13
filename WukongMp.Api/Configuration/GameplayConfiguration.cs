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

        public event Func<bool>? DisableTamerAttackQuery;
        public bool ShouldDisableTamerAttack() => DisableTamerAttackQuery?.Invoke() ?? false;

        public event Func<int, bool>? IsSkillEnabledQuery;
        public bool IsSkillEnabled(int skillId) => IsSkillEnabledQuery?.Invoke(skillId) ?? true;
    }
}