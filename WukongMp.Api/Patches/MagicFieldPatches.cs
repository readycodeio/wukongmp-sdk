using System.Reflection;
using b1;
using BtlB1;
using HarmonyLib;
using PreludeLib.Attributes;
using ReadyM.Api.Mapping.Tags;
using UnrealEngine.Runtime;
using WukongMp.Api.Configuration;
using WukongMp.Api.ECS.GameEvents;
using WukongMp.Api.WukongUtils;

namespace WukongMp.Api.Patches;

[HarmonyPatch(typeof(BUS_MFOverlapCompImpl), "OnMagicFieldDead")]
[HarmonyPatchCategory(PatchCategory.Connected)]
internal static class PatchOnMagicFieldDead
{
    public static void Postfix(BUS_MFOverlapCompImpl __instance, EBGUBulletDestroyReason Reason)
    {
        if (!DI.Instance.AreaState.InRoom)
            return;

        var owner = __instance.GetOwner();
        var className = owner.GetClass().GetName();
        if (className.Contains(Constants.SupremeInspectorFirewallName))
        {
            Logging.LogDebug("OnMagicFieldDead send for {Class}", className);
            DI.Instance.MappedEvent.NotifyEcsIfApplicable(new MagicFieldDeadEvent(className, Reason), default(EmptyContext));
        }
    }
}

[HarmonyPatch]
[HarmonyPatchCategory(PatchCategory.Connected)]
internal class PatchOSpawnAProjectileObj
{
    [HarmonyTargetMethodHint("b1.BGS_ProjectileManager", "SpawnAProjectileObj")]
    private static MethodBase TargetMethod()
    {
        return AccessTools.Method("b1.BGS_ProjectileManager:SpawnAProjectileObj");
    }

    public static void Prefix(FGSProjectileSpawnInfo ProjectileSpawnInfo)
    {
        if (!DI.Instance.AreaState.InRoom)
            return;

        if (DI.Instance.AreaState.IsMasterClient)
            return;

        ABGUCharacter? aBGUCharacter = ProjectileSpawnInfo.Spawner as ABGUCharacter;
        if (aBGUCharacter.IsNullOrDestroyed())
            return;

        FUStProjectileCommDesc projectileCommDesc = BGW_GameDB.GetProjectileCommDesc(ProjectileSpawnInfo.ProjectileID, aBGUCharacter);
        if (projectileCommDesc == null)
            return;

        string projectileBPTemplatePath = projectileCommDesc.ProjectileBPTemplatePath;
        var spawnPosition = ProjectileSpawnInfo.SpawnPosition;
        Logging.LogDebug("SpawnAProjectileObj send for {Projectile} at position {Position}", projectileBPTemplatePath, spawnPosition);
        if (projectileBPTemplatePath.Contains(Constants.SupremeInspectorFirewallName))
        {
            var previousPosition = ProjectileSpawnInfo.SpawnPosition;
            Logging.LogDebug("Modifying Supreme Inspector Firewall spawn position from {PrevPosition} to {Position}", previousPosition, Constants.SupremeInspectorFirewallLocation);
            ProjectileSpawnInfo.SpawnPosition = Constants.SupremeInspectorFirewallLocation;
            var localMainCharacter = DI.Instance.PlayerState.LocalMainCharacter;
            if (localMainCharacter.HasValue)
            {
                Logging.LogDebug("Teleporting local player to firewall location");
                PlayerUtils.TeleportLocalPlayer(localMainCharacter.Value, Constants.SupremeInspectorFirewallLocation, localMainCharacter.Value.Pawn?.GetActorRotation() ?? FRotator.ZeroRotator);
            }
        }
    }
}