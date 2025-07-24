using b1;
using b1.ECS;
using System;

namespace WukongMp.Api.WukongUtils
{
    public static class ProjectileUtils
    {
        private static Type? _projectileCtrlDataType = null;

        public static void SetProjectileTarget(BGUCharacterCS player, string projectileName, BGUCharacterCS target, string socketName)
        {
            Logging.LogWarning("Setting projectile target for player: {PlayerName}, projectile: {ProjectileName}, target: {TargetName}, socket: {SocketName}",
                player.GetName(), projectileName, target.GetName(), socketName);

            var projectile = GetProjectileByName(player, projectileName);
            if (projectile == null)
            {
                Logging.LogError("Projectile not found: {ProjectileName} for player: {PlayerName}", projectileName, player.GetName());
                return;
            }

            var events = BUS_EventCollectionCS.Get(projectile);
            events?.Evt_SwitchMovementTarget.Invoke(target, socketName);
        }

        public static void DestroyProjectile(BGUCharacterCS player, string projectileName, EBGUBulletDestroyReason reason)
        {
            Logging.LogWarning("Destroying projectile for player: {PlayerName}, projectile: {ProjectileName}, reason: {Reason}",
                player.GetName(), projectileName, reason);

            var projectile = GetProjectileByName(player, projectileName);
            if (projectile == null)
            {
                Logging.LogError("Projectile not found: {ProjectileName} for player: {PlayerName}", projectileName, player.GetName());
                return;
            }
            var events = BUS_EventCollectionCS.Get(projectile);
            events?.Evt_OnProjectileDead.Invoke(reason);
        }

        public static void SwitchProjectileInfo(BGUCharacterCS player, string projectileName, int bulletSwitchID, int switchIdx)
        {
            Logging.LogWarning("Switching projectile info for player: {PlayerName}, projectile: {ProjectileName}, bulletSwitchID: {BulletSwitchID}, switchIdx: {SwitchIdx}",
                player.GetName(), projectileName, bulletSwitchID, switchIdx);
            var projectile = GetProjectileByName(player, projectileName);
            if (projectile == null)
            {
                Logging.LogError("Projectile not found: {ProjectileName} for player: {PlayerName}", projectileName, player.GetName());
                return;
            }
            var events = BUS_EventCollectionCS.Get(player);
            events?.Evt_OnSwitchOneProjectile.Invoke(projectile, bulletSwitchID, switchIdx, null);
        }

        private static BGUProjectileBaseActor? GetProjectileByName(BGUCharacterCS player, string projectileName)
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
