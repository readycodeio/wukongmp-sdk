using System.Reflection;
using b1;
using BtlShare;
using Friflo.Engine.ECS;
using HarmonyLib;
using PreludeLib.Attributes;
using ReadyM.Api.Multiplayer.ECS.Values;
using UnrealEngine.Engine;
using WukongMp.Api.Configuration;
using WukongMp.Api.DTO;
using WukongMp.Api.ECS.GameEvents;

namespace WukongMp.Api.Patches;

[HarmonyPatch]
[HarmonyPatchCategory(Constants.ConnectedPatches)]
public class PatchOnSwitchBulletTarget
{
    [HarmonyTargetMethodHint("b1.BUS_ProjectileCtrComp", "OnSwitchBulletTarget")]
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

        if (owner == DI.Instance.PlayerState.LocalMainCharacter.Value.Pawn)
        {
            Entity? newTarget = null;
            if (InnerTarget is BGUPlayerCharacterCS)
            {
                var mainCharacterEntity = DI.Instance.PawnState.GetEntityByPlayerActor(InnerTarget);

                if (mainCharacterEntity == null)
                {
                    Logging.LogError("Player character entity not found for actor: {ActorName}", InnerTarget.GetName());
                    return false;
                }
                newTarget = mainCharacterEntity.Value;
            }
            else
            {
                var tamerEntity = DI.Instance.PawnState.GetEntityByTamerMonster(InnerTarget);
                if (tamerEntity.HasValue)
                {
                    newTarget = tamerEntity.Value;
                }
                else
                {
                    Logging.LogError("Could not find tamer entity for projectile target");
                }
            }
            var projectileClass = ProjectileActor.GetClass();
            if (projectileClass != null && newTarget.HasValue)
            {
                Logging.LogDebug("New projectile target sent for {Projectile} (Owner {NickName}) as: {Target}", projectileClass.GetName(), DI.Instance.PlayerState.LocalMainCharacter.Value.GetState().CharacterNickName, InnerTarget.GetName());
                var characterId = DI.Instance.PlayerState.LocalMainCharacter.Value;
                DI.Instance.MappedEvent.PropagateToEcs(new ProjectileTargetEvent(characterId, projectileClass.GetName(), newTarget.Value, SocketName));
            }
        }
        return true;
    }
}

[HarmonyPatch]
[HarmonyPatchCategory(Constants.ConnectedPatches)]
public class PatchOnSwitchBulletInfoIfNeed
{
    [HarmonyTargetMethodHint("b1.BUS_ProjectileCtrComp", "SwitchBulletInfoIfNeed")]
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

        var owner = __instance.GetOwner();
        if (owner.IsNullOrDestroyed())
        {
            Logging.LogError("Owner is null or destroyed");
            return false;
        }

        if (ProjectileActor == null)
        {
            return true;
        }

        if (owner == DI.Instance.PlayerState.LocalMainCharacter.Value.Pawn)
        {
            var projectileClass = ProjectileActor.GetClass();
            if (projectileClass != null)
            {
                Logging.LogDebug("Switch projectile info sent for {Projectile} (Owner {NickName}) with switch id: {SwitchID}", projectileClass.GetName(), DI.Instance.PlayerState.LocalMainCharacter.Value.GetState().CharacterNickName, BulletSwitchID);
                var entity = DI.Instance.PlayerState.LocalMainCharacter.Value;
                DI.Instance.MappedEvent.PropagateToEcs(new ProjectileSwitchEvent(entity, projectileClass.GetName(), BulletSwitchID, SwitchIdx));
            }
        }
        return true;
    }
}

[HarmonyPatch(typeof(BUS_ProjectileLifeComp), "OnProjectileDead")]
[HarmonyPatchCategory(Constants.ConnectedPatches)]
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
        if (projectile != null && DI.Instance.PlayerState.LocalMainCharacter.Value.Pawn == master)
        {
            var projectileClass = projectile.GetClass();
            if (projectileClass != null)
            {
                Logging.LogDebug("BUS_ProjectileLifeComp OnProjectileDead send with reason: {Reason}", Reason);
                var owner = DI.Instance.PlayerState.LocalMainCharacter.Value;
                DI.Instance.MappedEvent.PropagateToEcs(new ProjectileDeadEvent(owner, projectileClass.GetName(), Reason));
            }
        }
    }
}

[HarmonyPatch(typeof(BUS_ObjActorMovementComp), "OnSetMoveMode")]
[HarmonyPatchCategory(Constants.ConnectedPatches)]
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

        var masterData = BGU_DataUtil.GetReadOnlyData<IBUC_MasterData, BUC_MasterData>(projectile);
        if (masterData == null)
        {
            return;
        }

        var entity = DI.Instance.PlayerState.LocalMainCharacter;
        if (!entity.HasValue)
            return;

        var master = masterData.GetMasterActor();

        if (entity.Value.Pawn == master)
        {
            var projectileClass = projectile.GetClass();
            if (projectileClass != null)
            {
                Logging.LogDebug("New move mode sent for {Projectile} (Owner {NickName}) as: {MoveMode}", projectileClass.GetName(), DI.Instance.PlayerState.LocalMainCharacter.Value.GetState().CharacterNickName, MoveMode);
                DI.Instance.MappedEvent.PropagateToEcs(new ProjectileMoveModeEvent(entity.Value, projectileClass.GetName(), MoveMode));
            }
        }
    }
}

[HarmonyPatch(typeof(BUEffectBulletSwitchSelf), "ApplyBySkill_Implement")]
[HarmonyPatchCategory(Constants.ConnectedPatches)]
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

        if (masterActor is BGUPlayerCharacterCS && masterActor != DI.Instance.PlayerState.LocalMainCharacter.Value.Pawn)
        {
            Logging.LogDebug("Skipping BUEffectBulletSwitchSelf ApplyBySkill_Implement called for non local player");
            return false;
        }
        return true;
    }
}

[HarmonyPatch(typeof(BPS_MultiTargetProjectileCtrComp), "CheckTargetValid")]
[HarmonyPatchCategory(Constants.ConnectedPatches)]
public static class PatchCheckTargetValid
{
    public static bool Prefix(AActor Target, ref bool __result)
    {
        if (!DI.Instance.AreaState.InRoom)
            return true;

        if (BGUFunctionLibraryCS.BGUHasUnitSimpleState(Target, EBGUSimpleState.PhantomRush))
        {
            __result = false;
            return false;
        }
        return true;
    }
}

[HarmonyPatch(typeof(BPS_MultiTargetProjectileCtrComp), "SearchTargetTick")]
[HarmonyPatchCategory(Constants.ConnectedPatches)]
public static class PatchSearchTargetTick
{
    private static MethodInfo? _changeToFollowMasterMethod;
    public static void Prefix(BPS_MultiTargetProjectileCtrComp __instance, BPC_MultiTargetProjectileCtrData ___MultiTargetProjectileCtrData, IBUC_TargetInfoData ___TargetInfoData)
    {
        if (!DI.Instance.AreaState.InRoom)
            return;

        var targetList = ___TargetInfoData?.GetMultiTargetInfoList();
        if (targetList == null || targetList.Count == 0)
            return;

        var target = targetList[0].LockTargetActor;
        if (target.IsNullOrDestroyed())
            return;

        if (BGUFunctionLibraryCS.BGUHasUnitSimpleState(target, EBGUSimpleState.PhantomRush))
        {
            _changeToFollowMasterMethod ??= AccessTools.Method(typeof(BPS_MultiTargetProjectileCtrComp), "ChangeToFollowMaster");
            if (_changeToFollowMasterMethod == null)
            {
                return;
            }
            _changeToFollowMasterMethod.Invoke(__instance, null);
        }
    }
}
