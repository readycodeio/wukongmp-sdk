using b1;
using b1.ECS;
using BtlShare;
using System;
using System.Threading.Tasks;

namespace WukongMp.Api.WukongUtils
{
    public static class ProjectileUtils
    {
        private static Type? _projectileCtrlDataType = null;

        public static async Task SetProjectileTarget(BGUCharacterCS player, string projectileName, BGUCharacterCS target, string socketName)
        {
            await Task.Delay(200);
            Logging.LogDebug("SetProjectileTarget called for projectile {ProjectileName} with target {TargetName}", projectileName, target.GetName());
            var projectile = GetPlayerProjectileByName(player, projectileName);
            if (projectile == null)
            {
                Logging.LogError("Projectile not found: {ProjectileName} for player: {PlayerName}", projectileName, player.GetName());
                return;
            }

            var events = BUS_EventCollectionCS.Get(projectile);
            events?.Evt_SwitchMovementTarget.Invoke(target, socketName);
        }

        public static async Task DestroyProjectile(BGUCharacterCS player, string projectileName, EBGUBulletDestroyReason reason)
        {
            await Task.Delay(200);
            Logging.LogDebug("DestroyProjectile called for projectile {ProjectileName} with reason {Reason}", projectileName, reason);
            var projectile = GetPlayerProjectileByName(player, projectileName);
            if (projectile == null)
            {
                Logging.LogError("Projectile not found: {ProjectileName} for player: {PlayerName}", projectileName, player.GetName());
                return;
            }
            var events = BUS_EventCollectionCS.Get(projectile);
            events?.Evt_OnProjectileDead.Invoke(reason);
        }

        public static async Task SetProjectileModeMode(BGUCharacterCS player, string projectileName, EBulletOrMagicFieldMoveModeType moveMode)
        {
            await Task.Delay(200);
            Logging.LogDebug("SetProjectileModeMode called for projectile {ProjectileName} with move mode {MoveMode}", projectileName, moveMode);
            var projectile = GetPlayerProjectileByName(player, projectileName);
            if (projectile == null)
            {
                Logging.LogError("Projectile not found: {ProjectileName} for player: {PlayerName}", projectileName, player.GetName());
                return;
            }
            var events = BUS_EventCollectionCS.Get(projectile);
            events?.Evt_SetObjMoveMode.Invoke(moveMode);
        }

        public static async Task SwitchProjectileInfo(BGUCharacterCS player, string projectileName, int bulletSwitchID, int switchIdx)
        {
            await Task.Delay(200);
            Logging.LogDebug("SwitchProjectileInfo called for projectile {ProjectileName} with switch id {MoveMode}", projectileName, bulletSwitchID);
            var projectile = GetPlayerProjectileByName(player, projectileName);
            if (projectile == null)
            {
                Logging.LogError("Projectile not found: {ProjectileName} for player: {PlayerName}", projectileName, player.GetName());
                return;
            }
            var events = BUS_EventCollectionCS.Get(player);
            events?.Evt_OnSwitchOneProjectile.Invoke(projectile, bulletSwitchID, switchIdx, null);
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
    }
}
