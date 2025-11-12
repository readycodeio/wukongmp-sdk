using System;

namespace WukongMp.Api.Configuration
{
    public class GameplayConfiguration
    {
        public bool IsSupportMultiLockEnabled { get; set; } = false;
        public bool IsStrongDamageImmueEnabled { get; set; } = false;
        public bool EnableCustomCameraArmLength { get; set; } = false;
        public bool EnableSpawnedTamers { get; set; } = false;


        public event Func<bool>? DisableTamerAttackQuery;
        public bool ShouldDisableTamerAttack() => DisableTamerAttackQuery?.Invoke() ?? false;
    }
}
