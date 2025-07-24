using b1;
using HarmonyLib;
using ReadyM.Relay.Common.ECS;
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

[HarmonyPatch(typeof(BUS_ObjActorMovementComp), "OnInitObjMoveInfo")]
[HarmonyPatchCategory(Constants.CoopPatches)]
public static class PatchOnInitObjMoveInfo
{
    public static void Postfix(BUS_ObjActorMovementComp __instance, GSObjActorMoveInfo MoveInfo)
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
            var target = MoveInfo.TargetActor;

            var newTargetId = default(NetworkIdComponent);
            if (target is BGUPlayerCharacterCS)
            {
                var playerState = players.GetPlayerByActor(target);
                if (playerState == null)
                {
                    Logging.LogError("Player state not found for actor: {ActorName}", target.GetName());
                    return;
                }
                newTargetId = NetworkIdComponent.FromPlayerId(playerState.PlayerId);
            }
            else
            {
                var entity = DI.Instance.PawnRegistry.GetMonsterByActor(target);
                if (entity.HasValue)
                {
                    newTargetId = entity.Value.GetComponent<NetworkIdComponent>();
                }
            }
            Logging.LogDebug("New projectile target sent for {Projectile} (Owner {NickName}) as: {Target}", projectile.GetClass().GetName(), players.LocalPlayerState.NickName, target.GetName());
            DI.Instance.Rpc.SendProjectileTarget(new ProjectileTargetData(projectile.GetClass().GetName(), newTargetId, MoveInfo.TargetActorSocketNameFromNotify));
        }
    }
}
