using System.Reflection;
using b1;
using BtlShare;
using HarmonyLib;
using PreludeLib.Attributes;
using UnrealEngine.Engine;
using WukongMp.Api.Configuration;
using WukongMp.Api.ECS.GameEvents;

namespace WukongMp.Api.Patches;

[HarmonyPatch]
[HarmonyPatchCategory(PatchCategory.Connected)]
internal class PatchOnSwitchBulletTarget
{
    [HarmonyTargetMethodHint("b1.BUS_ProjectileCtrComp", "OnSwitchBulletTarget")]
    private static MethodBase TargetMethod()
    {
        return AccessTools.Method("b1.BUS_ProjectileCtrComp:OnSwitchBulletTarget");
    }

    public static bool Prefix(UActorCompBaseCS? __instance, BGUProjectileBaseActor? ProjectileActor, AActor? InnerTarget, string SocketName = "")
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

        if (DI.Instance.MappingPolicyDir.IsMainCharacterMapped(owner, out var entity))
        {
            if (DI.Instance.MappingPolicyDir.IsCharacterMapped(InnerTarget, out var targetEntity))
            {
                var projectileClass = ProjectileActor.GetClass();
                if (projectileClass != null)
                {
                    var sent = DI.Instance.MappedEvent.NotifyEcsIfApplicable(new ProjectileTargetEvent(entity.Value, projectileClass.GetName(), targetEntity.Value, SocketName), entity.Value.Entity);
                    if (sent)
                        Logging.LogDebug("New projectile target sent for {Projectile} (Owner {NickName}) as: {Target}", projectileClass.GetName(), entity.Value.GetState().CharacterNickname, InnerTarget.GetName());
                }
            }
            else
            {
                Logging.LogError("Target entity not found for actor: {ActorName}", InnerTarget.GetName());
            }
        }

        return true;
    }
}

[HarmonyPatch]
[HarmonyPatchCategory(PatchCategory.Connected)]
internal class PatchOnSwitchBulletInfoIfNeed
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

        if (DI.Instance.MappingPolicyDir.IsMainCharacterMapped(owner, out var entity))
        {
            var projectileClass = ProjectileActor.GetClass();
            if (projectileClass != null)
            {
                var sent = DI.Instance.MappedEvent.NotifyEcsIfApplicable(new ProjectileSwitchEvent(entity.Value, projectileClass.GetName(), BulletSwitchID, SwitchIdx), entity.Value.Entity);
                if (sent)
                    Logging.LogDebug("Switch projectile info sent for {Projectile} (Owner {NickName}) with switch id: {SwitchID}", projectileClass.GetName(), entity.Value.GetState().CharacterNickname, BulletSwitchID);
            }
        }

        return true;
    }
}

[HarmonyPatch(typeof(BUS_ProjectileLifeComp), "OnProjectileDead")]
[HarmonyPatchCategory(PatchCategory.Connected)]
internal static class PatchOnProjectileDead
{
    public static void Postfix(BUS_ProjectileLifeComp __instance, IBUC_MasterData ___MasterData, EBGUBulletDestroyReason Reason)
    {
        if (!DI.Instance.AreaState.InRoom)
            return;

        if (DI.Instance.PlayerState.LocalMainCharacter == null)
            return;

        var master = ___MasterData.GetMasterActor();
        var projectile = __instance.GetOwner() as BGUProjectileBaseActor;

        if (projectile != null && DI.Instance.MappingPolicyDir.IsMainCharacterMapped(master, out var entity))
        {
            var projectileClass = projectile.GetClass();
            if (projectileClass != null)
            {
                var sent = DI.Instance.MappedEvent.NotifyEcsIfApplicable(new ProjectileDeadEvent(entity.Value, projectileClass.GetName(), Reason), entity.Value.Entity);
                if (sent)
                    Logging.LogDebug("BUS_ProjectileLifeComp OnProjectileDead send with reason: {Reason}", Reason);
            }
        }
    }
}

[HarmonyPatch(typeof(BUS_ObjActorMovementComp), "OnSetMoveMode")]
[HarmonyPatchCategory(PatchCategory.Connected)]
internal static class PatchOnSetMoveMode
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

        var master = masterData.GetMasterActor();

        if (DI.Instance.MappingPolicyDir.IsMainCharacterMapped(master, out var entity))
        {
            var projectileClass = projectile.GetClass();
            if (projectileClass != null)
            {
                var sent = DI.Instance.MappedEvent.NotifyEcsIfApplicable(new ProjectileMoveModeEvent(entity.Value, projectileClass.GetName(), MoveMode), entity.Value.Entity);
                if (sent)
                    Logging.LogDebug("New move mode sent for {Projectile} (Owner {NickName}) as: {MoveMode}", projectileClass.GetName(), entity.Value.GetState().CharacterNickname, MoveMode);
            }
        }
    }
}

[HarmonyPatch(typeof(BUEffectBulletSwitchSelf), "ApplyBySkill_Implement")]
[HarmonyPatchCategory(PatchCategory.Connected)]
internal static class PatchApplyBySkill_Implement
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
[HarmonyPatchCategory(PatchCategory.Connected)]
internal static class PatchCheckTargetValid
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
[HarmonyPatchCategory(PatchCategory.Connected)]
internal static class PatchSearchTargetTick
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