using b1;
using b1.ECS;
using BtlShare;
using System;

namespace WukongMp.Api.WukongUtils
{
    public static class ProjectileUtils
    {
        private static Type? _projectileCtrlDataType = null;

        public static void SetProjectileTarget(BGUCharacterCS player, string projectileName, BGUCharacterCS target, string socketName)
        {
            if (player.IsNullOrDestroyed())
            {
                // TODO: Remove after verification that this caused problems.
                Logging.LogWarning("Player is null in SwitchProjectileInfo");
                return;
            }
            Logging.LogDebug("SetProjectileTarget called for projectile {ProjectileName} with target {TargetName}", projectileName, target.GetName());
            var projectile = GetPlayerProjectileByName(player, projectileName);
            if (IsProjectileValid(projectile, projectileName, player.GetName()))
            {
                var events = BUS_EventCollectionCS.Get(projectile);
                events?.Evt_SwitchMovementTarget.Invoke(target, socketName);
            }
        }

        public static void DestroyProjectile(BGUCharacterCS player, string projectileName, EBGUBulletDestroyReason reason)
        {
            if (player.IsNullOrDestroyed())
            {
                Logging.LogWarning("Player is null in SwitchProjectileInfo");
                return;
            }
            Logging.LogDebug("DestroyProjectile called for projectile {ProjectileName} with reason {Reason}", projectileName, reason);
            var projectile = GetPlayerProjectileByName(player, projectileName);
            if (IsProjectileValid(projectile, projectileName, player.GetName()))
            {
                var events = BUS_EventCollectionCS.Get(projectile);
                events?.Evt_OnProjectileDead.Invoke(reason);
            }
        }

        public static void SetProjectileMode(BGUCharacterCS player, string projectileName, EBulletOrMagicFieldMoveModeType moveMode)
        {
            if (player.IsNullOrDestroyed())
            {
                Logging.LogWarning("Player is null in SwitchProjectileInfo");
                return;
            }
            Logging.LogDebug("SetProjectileMode called for projectile {ProjectileName} with move mode {MoveMode}", projectileName, moveMode);
            var projectile = GetPlayerProjectileByName(player, projectileName);
            if (IsProjectileValid(projectile, projectileName, player.GetName()))
            {
                var events = BUS_EventCollectionCS.Get(projectile);
                events?.Evt_SetObjMoveMode.Invoke(moveMode);
            }
        }

        public static void SwitchProjectileInfo(BGUCharacterCS player, string projectileName, int bulletSwitchID, int switchIdx)
        {
            if (player.IsNullOrDestroyed())
            {
                Logging.LogWarning("Player is null in SwitchProjectileInfo");
                return;
            }
            Logging.LogDebug("SwitchProjectileInfo called for projectile {ProjectileName} with switch id {MoveMode}", projectileName, bulletSwitchID);
            var projectile = GetPlayerProjectileByName(player, projectileName);
            if (IsProjectileValid(projectile, projectileName, player.GetName()))
            {
                var events = BUS_EventCollectionCS.Get(player);
                events?.Evt_OnSwitchOneProjectile.Invoke(projectile, bulletSwitchID, switchIdx, null);
            }
        }

        private static BGUProjectileBaseActor? GetPlayerProjectileByName(BGUCharacterCS player, string projectileName)
        {
            if (_projectileCtrlDataType == null)
            {
                InitProjectileCtrlDataType();
                if (_projectileCtrlDataType == null)
                {
                    Logging.LogError("Failed to find type b1.BUC_ProjectileCtrData");
                    return null;
                }
            }

            var projectileCtrData = (IBUC_ProjectileCtrlData)BGU_DataUtil.GetReadOnlyData(player, TypeManager.GetTypeIndex(_projectileCtrlDataType));
            if (projectileCtrData == null)
            {
                Logging.LogError("Projectile control data is null for player: {PlayerName}", player.GetName());
                return null;
            }
            foreach (var projectile in projectileCtrData.ProjectileList)
            {
                if (projectile.GetClass().GetName() == projectileName)
                {
                    return projectile;
                }
            }
            return null;
        }

        private static void InitProjectileCtrlDataType()
        {
            var assembly = typeof(IBUC_ProjectileCtrlData).Assembly;
            _projectileCtrlDataType = assembly.GetType("b1.BUC_ProjectileCtrData", throwOnError: false, ignoreCase: false);
        }

        private static bool IsProjectileValid(BGUProjectileBaseActor? projectile, string projectileName, string playerName)
        {
            if (projectile == null)
            {
                // TODO: Not ERROR because we are not handling all projectiles yet.
                Logging.LogWarning("Projectile not found: {ProjectileName} for player: {PlayerName}", projectileName, playerName);
                return false;
            }
            return true;
        }
    }
}
