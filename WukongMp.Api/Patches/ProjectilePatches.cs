using b1;
using BtlShare;
using HarmonyLib;
using ReadyM.Api.Multiplayer.ECS.Values;
using System.Reflection;
using UnrealEngine.Engine;
using WukongMp.Api.Configuration;
using WukongMp.Api.DTO;
using WukongMp.Api.ECS.Entities;

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
        if (!DI.Instance.AreaState.InRoom)
            return true;

        if (DI.Instance.PlayerState.LocalMainCharacter == null)
            return true;

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

        if (owner == DI.Instance.PlayerState.LocalMainCharacter.Value.GetLocalState().Pawn)
        {
            var newTargetId = default(NetworkId);
            if (InnerTarget is BGUPlayerCharacterCS)
            {
                var mainCharacterEntity = DI.Instance.PawnState.GetByEntityByPlayerPawn(InnerTarget);

                if (mainCharacterEntity == null)
                {
                    Logging.LogError("Player character entity not found for actor: {ActorName}", InnerTarget.GetName());
                    return false;
                }
                newTargetId = mainCharacterEntity.Value.GetMeta().NetId;
            }
            else
            {
                var tamerEntity = DI.Instance.PawnState.GetEntityByTamerMonster(InnerTarget);
                if (tamerEntity.HasValue)
                {
                    newTargetId = tamerEntity.Value.GetMeta().NetId;
                }
                else
                {
                    Logging.LogError("Could not find tamer entity for projectile target");
                }
            }
            Logging.LogDebug("New projectile target sent for {Projectile} (Owner {NickName}) as: {Target}", ProjectileActor.GetClass().GetName(), DI.Instance.PlayerState.LocalMainCharacter.Value.GetState().CharacterNickName, InnerTarget.GetName());
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
        if (!DI.Instance.AreaState.InRoom)
            return true;

        if (DI.Instance.PlayerState.LocalMainCharacter == null)
            return true;

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

        if (owner == DI.Instance.PlayerState.LocalMainCharacter.Value.GetLocalState().Pawn)
        {
            Logging.LogDebug("Switch projectile info sent for {Projectile} (Owner {NickName}) with switch id: {SwitchID}", ProjectileActor.GetClass().GetName(), DI.Instance.PlayerState.LocalMainCharacter.Value.GetState().CharacterNickName, BulletSwitchID);
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
        if (!DI.Instance.AreaState.InRoom)
            return;

        if (DI.Instance.PlayerState.LocalMainCharacter == null)
            return;

        var master = ___MasterData.GetMasterActor();
        var projectile = __instance.GetOwner() as BGUProjectileBaseActor;
        if (projectile != null && DI.Instance.PlayerState.LocalMainCharacter.Value.GetLocalState().Pawn == master)
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
        if (!DI.Instance.AreaState.InRoom)
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

        if (DI.Instance.PlayerState.LocalMainCharacter == null)
            return;

        var master = masterData.GetMasterActor();

        if (DI.Instance.PlayerState.LocalMainCharacter.Value.GetLocalState().Pawn == master)
        {
            Logging.LogDebug("New move mode sent for {Projectile} (Owner {NickName}) as: {MoveMode}", projectile.GetClass().GetName(), DI.Instance.PlayerState.LocalMainCharacter.Value.GetState().CharacterNickName, MoveMode);
            DI.Instance.Rpc.SendProjectileMoveMode(new ProjectileMoveModeData(projectile.GetClass().GetName(), MoveMode));
        }
    }
}

[HarmonyPatch(typeof(BUEffectBulletSwitchSelf), "ApplyBySkill_Implement")]
[HarmonyPatchCategory(Constants.CoopPatches)]
public static class PatchApplyBySkill_Implement
{
    public static bool Prefix(int EffectID, AActor? Caster, AActor? Target)
    {
        if (!DI.Instance.AreaState.InRoom)
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

        if (DI.Instance.PlayerState.LocalMainCharacter == null)
            return true;

        if (masterActor is BGUPlayerCharacterCS && masterActor != DI.Instance.PlayerState.LocalMainCharacter.Value.GetLocalState().Pawn)
        {
            Logging.LogDebug("Skipping BUEffectBulletSwitchSelf ApplyBySkill_Implement called for non local player");
            return false;
        }
        return true;
    }
}
