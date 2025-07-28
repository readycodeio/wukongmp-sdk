using b1;
using BtlShare;
using HarmonyLib;
using ReadyM.Relay.Common.ECS;
using System.Collections.Generic;
using System.Reflection;
using UnrealEngine.Engine;
using WukongMp.Api.Configuration;
using WukongMp.Api.DTO;

namespace WukongMp.Api.Patches;

[HarmonyPatch]
[HarmonyPatchCategory(Constants.ConnectedPatches)]
public class PatchOnSwitchBulletTarget
{
    private static MethodBase TargetMethod()
    {
        return AccessTools.Method("b1.BUS_ProjectileCtrComp:OnSwitchBulletTarget");
    }

    public static bool Prefix(UActorCompBaseCS __instance, BGUProjectileBaseActor? ProjectileActor, AActor? InnerTarget, string SocketName = "")
    {
        if (!DI.Instance.RelayClient.InRoom)
            return true;

        var players = DI.Instance.Players;
        var owner = __instance?.GetOwner();
        if (owner.IsNullOrDestroyed())
        {
            Logging.LogError("Owner is null or destroyed");
            return false;
        }

        if (ProjectileActor == null || InnerTarget == null)
        {
            return true;
        }

        if (owner == players.LocalPlayerState.Pawn)
        {
            var newTargetId = default(NetworkIdComponent);
            if (InnerTarget is BGUPlayerCharacterCS)
            {
                var playerState = players.GetPlayerByActor(InnerTarget);
                if (playerState == null)
                {
                    Logging.LogError("Player state not found for actor: {ActorName}", InnerTarget.GetName());
                    return false;
                }
                newTargetId = NetworkIdComponent.FromPlayerId(playerState.PlayerId);
            }
            else
            {
                var entity = DI.Instance.PawnRegistry.GetMonsterByActor(InnerTarget);
                if (entity.HasValue)
                {
                    newTargetId = entity.Value.GetComponent<NetworkIdComponent>();
                }
            }
            Logging.LogDebug("New projectile target sent for {Projectile} (Owner {NickName}) as: {Target}", ProjectileActor.GetClass().GetName(), players.LocalPlayerState.NickName, InnerTarget.GetName());
            DI.Instance.Rpc.SendProjectileTarget(new ProjectileTargetData(ProjectileActor.GetClass().GetName(), newTargetId, SocketName));
            return true;
        }
        return true;
    }
}

[HarmonyPatch]
[HarmonyPatchCategory(Constants.ConnectedPatches)]
public class PatchOnSwitchBulletInfoIfNeed
{
    private static MethodBase TargetMethod()
    {
        return AccessTools.Method("b1.BUS_ProjectileCtrComp:SwitchBulletInfoIfNeed");
    }

    public static bool Prefix(UActorCompBaseCS __instance, BGUProjectileBaseActor? ProjectileActor, int BulletSwitchID, int SwitchIdx)
    {
        if (!DI.Instance.RelayClient.InRoom)
            return true;

        var players = DI.Instance.Players;
        var owner = __instance?.GetOwner();
        if (owner.IsNullOrDestroyed())
        {
            Logging.LogError("Owner is null or destroyed");
            return false;
        }

        if (ProjectileActor == null)
        {
            return true;
        }

        if (owner == players.LocalPlayerState.Pawn)
        {
            Logging.LogDebug("Switch projectile info sent for {Projectile} (Owner {NickName}) with switch id: {SwitchID}", ProjectileActor.GetClass().GetName(), players.LocalPlayerState.NickName, BulletSwitchID);
            DI.Instance.Rpc.SendSwitchOneProjectile(new ProjectileSwitchData(ProjectileActor.GetClass().GetName(), BulletSwitchID, SwitchIdx));
            return true;
        }
        return true;
    }
}

[HarmonyPatch(typeof(BUS_ProjectileLifeComp), "OnProjectileDead")]
[HarmonyPatchCategory(Constants.CoopPatches)]
public static class PatchOnProjectileDead
{
    public static void Postfix(BUS_ProjectileLifeComp __instance, IBUC_MasterData ___MasterData, EBGUBulletDestroyReason Reason)
    {
        if (!DI.Instance.RelayClient.InRoom)
            return;

        var players = DI.Instance.Players;
        var master = ___MasterData.GetMasterActor();
        var projectile = __instance.GetOwner() as BGUProjectileBaseActor;
        if (projectile != null && players.LocalPlayerState.Pawn == master)
        {
            Logging.LogDebug("BUS_ProjectileLifeComp OnProjectileDead send with reason: {Reason}", Reason);
            DI.Instance.Rpc.SendProjectileDead(new ProjectileDeadData(projectile.GetClass().GetName(), Reason));
        }
    }
}

[HarmonyPatch(typeof(BUS_ObjActorMovementComp), "OnSetMoveMode")]
[HarmonyPatchCategory(Constants.CoopPatches)]
public static class PatchOnSetMoveMode
{
    public static void Postfix(BUS_ObjActorMovementComp __instance, EBulletOrMagicFieldMoveModeType MoveMode)
    {
        if (!DI.Instance.RelayClient.InRoom)
            return;

        var projectile = __instance.GetOwner() as BGUProjectileBaseActor;
        if (projectile == null)
        {
            return;
        }

        IBUC_MasterData masterData = BGU_DataUtil.GetReadOnlyData<IBUC_MasterData, BUC_MasterData>(projectile);
        if (masterData == null)
        {
            return;
        }

        var players = DI.Instance.Players;
        var master = masterData.GetMasterActor();

        if (players.LocalPlayerState.Pawn == master)
        {
            Logging.LogDebug("New move mode sent for {Projectile} (Owner {NickName}) as: {MoveMode}", projectile.GetClass().GetName(), players.LocalPlayerState.NickName, MoveMode);
            DI.Instance.Rpc.SendProjectileMoveMode(new ProjectileMoveModeData(projectile.GetClass().GetName(), MoveMode));
        }
    }
}

[HarmonyPatch]
[HarmonyPatchCategory(Constants.GlobalPatches)]
public class PatchAllPlayerInput
{
    public static IEnumerable<MethodBase> TargetMethods()
    {
        var type = AccessTools.TypeByName("b1.BUS_ProjectileCtrComp");
        foreach (var method in Traverse.Create<BUS_ObjActorMovementComp>().Methods())
        {
            if (method == "GetTickGroupMask" ||
                method == "OnTickWithGroup" ||
                method == "UpdateVelocityData" ||
                method == "BulletSweepFlySpdTick"||
                method == "CreateMoveMode")
                continue;
            yield return AccessTools.Method(typeof(BUS_ObjActorMovementComp), method);
        }
    }

    public static void Prefix(MethodBase __originalMethod)
    {
        Logging.LogWarning("BUS_ObjActorMovementComp: {MethodName} called", __originalMethod.Name);
    }
}


[HarmonyPatch(typeof(BUEffectBulletSwitchSelf), "ApplyBySkill_Implement")]
[HarmonyPatchCategory(Constants.CoopPatches)]
public static class PatchApplyBySkill_Implement
{
    public static bool Prefix(int EffectID, AActor? Caster, AActor? Target)
    {
        if (!DI.Instance.RelayClient.InRoom)
            return true;

        BGUBulletBaseCS? bGUBulletBaseCS = Target as BGUBulletBaseCS;
        if (bGUBulletBaseCS == null)
        {
            return true;
        }
        BUC_MasterData readOnlyData = BGU_DataUtil.GetReadOnlyData<BUC_MasterData>(bGUBulletBaseCS);
        if (readOnlyData == null)
        {
            return true;
        }
        AActor masterActor = readOnlyData.GetMasterActor();

        var players = DI.Instance.Players;

        if (masterActor is BGUPlayerCharacterCS && players.LocalPlayerState.Pawn != masterActor)
        {
            Logging.LogDebug("Skipping BUEffectBulletSwitchSelf ApplyBySkill_Implement called for non local player");
            return false;
        }
        return true;
    }
}
