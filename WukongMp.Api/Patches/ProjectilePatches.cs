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
                Logging.LogDebug("InnerTarget is a player character: {NickName}", playerState.NickName);
                newTargetId = NetworkIdComponent.FromPlayerId(playerState.PlayerId);
            }
            else
            {
                Logging.LogDebug("InnerTarget is a monster or other actor: {ActorName}", InnerTarget.GetName());
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

        Logging.LogDebug("BUS_ProjectileLifeComp OnProjectileDead called with reason: {Reason}", Reason);

        var players = DI.Instance.Players;
        var master = ___MasterData.GetMasterActor();
        var projectile = __instance.GetOwner() as BGUProjectileBaseActor;
        if (projectile != null && players.LocalPlayerState.Pawn == master)
        {
            DI.Instance.Rpc.SendProjectileDead(new ProjectileDeadData(projectile.GetClass().GetName(), Reason));
        }
    }
}
