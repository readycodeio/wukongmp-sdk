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

        // return of owner if not player
        if (!owner.IsA<BGUPlayerCharacterCS>())
        {
            return true;
        }

        // send only if the owner is the local player
        if (owner == players.LocalPlayerState.Pawn)
        {
            var newTargetId = default(NetworkIdComponent);
            if (InnerTarget is BGUPlayerCharacterCS)
            {
                var playerState = players.GetPlayerByActor(InnerTarget);
                if( playerState == null)
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

