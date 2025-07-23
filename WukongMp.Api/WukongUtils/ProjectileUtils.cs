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
            if (_projectileCtrlDataType == null)
            {
                InitProjectileCtrlDataType();
                if (_projectileCtrlDataType == null)
                {
                    Logging.LogError("Failed to find type b1.BUC_ProjectileCtrData");
                    return;
                }
            }

            var projectileCtrData = (IBUC_ProjectileCtrlData)BGU_DataUtil.GetReadOnlyData(player, TypeManager.GetTypeIndex(_projectileCtrlDataType));
            if (projectileCtrData == null)
            {
                Logging.LogError("Projectile control data is null for player: {PlayerName}", player.GetName());
                return;
            }
            foreach (var projectile in projectileCtrData.ProjectileList)
            {
                if (projectile.GetClass().GetName() == projectileName)
                {
                    //var events = BUS_EventCollectionCS.Get(player);
                    //events?.Evt_SwitchBulletTarget.Invoke(projectile, target, socketName);
                    BUS_GSEventCollection bUS_GSEventCollection = BUS_EventCollectionCS.Get(projectile);
                    bUS_GSEventCollection?.Evt_SwitchMovementTarget.Invoke(target, socketName);
                    break;
                }
            }
        }

        private static void InitProjectileCtrlDataType()
        {
            var assembly = typeof(IBUC_ProjectileCtrlData).Assembly;
            _projectileCtrlDataType = assembly.GetType("b1.BUC_ProjectileCtrData", throwOnError: false, ignoreCase: false);
        }
    }
}
