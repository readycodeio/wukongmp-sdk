using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using b1;
using b1.BGW;
using BtlB1;
using BtlShare;
using HarmonyLib;
using PreludeLib.Attributes;
using ReadyM.Api.Mapping.Tags;
using ReadyM.Wukong.Common.ECS.Components;
using UnrealEngine.Engine;
using UnrealEngine.Runtime;
using WukongMp.Api.Configuration;
using WukongMp.Api.ECS.Entities;
using WukongMp.Api.ECS.GameEvents;
using WukongMp.Api.WukongUtils;

namespace WukongMp.Api.Patches;

[HarmonyPatch(typeof(BUS_PlayerInputActionComp), "TriggerMagicSkill")]
[HarmonyPatchCategory(PatchCategory.Connected)]
internal static class PatchTriggerMagicSkill
{
    public static bool Prefix(int SkillID)
    {
        if (!DI.Instance.AreaState.InRoom)
            return true;

        return DI.Instance.GameplayConfiguration.IsSkillEnabled(SkillID);
    }
}

[HarmonyPatch(typeof(BUS_PlayerInputActionComp), "TriggerItemSkill")]
[HarmonyPatchCategory(PatchCategory.Connected)]
internal static class PatchTriggerItemSkill
{
    public static bool Prefix(BUS_PlayerInputActionComp __instance)
    {
        var areaState = DI.Instance.AreaState;
        if (areaState.CurrentArea == null)
            return true;

        var lastSkill = Traverse.Create(__instance).Field("ComboCacheData").Property<int>("LastItemSkillID").Value;
        return DI.Instance.GameplayConfiguration.IsSkillEnabled(lastSkill);
    }
}

[HarmonyPatch(typeof(BUS_CastImmobilizeComp), "OnCastImmobilize")]
[HarmonyPatchCategory(PatchCategory.Connected)]
internal static class PatchOnCastImmobilize
{
    public static bool Prefix(int ConfigID, BUS_CastImmobilizeComp __instance)
    {
        if (!DI.Instance.AreaState.InRoom)
            return true;

        // get properties
        var getter = AccessTools.PropertyGetter(typeof(BUS_CastImmobilizeComp), "CastImmobilizeData");
        var castImmobilizeData = (BUC_CastImmobilizeData)getter.Invoke(__instance, null);
        getter = AccessTools.PropertyGetter(typeof(BUS_CastImmobilizeComp), "TargetInfoData");
        var targetInfoData = (IBUC_TargetInfoData)getter.Invoke(__instance, null);
        getter = AccessTools.PropertyGetter(typeof(BUS_CastImmobilizeComp), "PassiveSkillData");
        var passiveSkillData = (IBUC_PassiveSkillData)getter.Invoke(__instance, null);
        getter = AccessTools.PropertyGetter(typeof(BUS_CastImmobilizeComp), "BuffData");
        var buffData = (IBUC_BuffData)getter.Invoke(__instance, null);

        var castingCharacter = __instance.GetOwner();

        if (castingCharacter.IsNullOrDestroyed())
        {
            Logging.LogError("Owner is null or destroyed");
            return false;
        }

        if (!DI.Instance.MappingPolicyDir.IsMainCharacterMapped(castingCharacter, out var castingMainEntity))
            return false;

        DI.Instance.MappedEvent.NotifyEcsIfApplicable(new CastImmobilizeEvent(castingMainEntity.Value), castingMainEntity.Value.Entity);
        if (!DI.Instance.MappingPolicyDir.ForEvent<CastImmobilizeEvent>().CanGameEventRunLocally(castingMainEntity.Value.Entity))
            return false;

        Debug.Assert(DI.Instance.AreaState.IsMasterClient, "DI.Instance.AreaState.IsMasterClient");
        // inlined OnCastImmobilize code with some modifications follows

        if (ConfigID == 0)
        {
            ConfigID = castImmobilizeData.ResId;
        }

        if (!passiveSkillData.TryGetCachedDesc<FUStImmobilizeSkillConfigDesc>(ConfigID, out var cachedDesc) || BGW_LogUtil.LogIfNull(castingCharacter as ABGUCharacter, "CurCharacter is null"))
        {
            return false;
        }

        var aBguCharacter = targetInfoData.GetSkillBaseTarget().LockTargetActor as ABGUCharacter;
        if (aBguCharacter == null)
        {
            aBguCharacter = targetInfoData.GetTargetInfo().LockTargetActor as ABGUCharacter;
        }

        if (aBguCharacter == null || !BGUFuncLibSelectTargetsCS.BGUIsSelectTargetByTeamFilter(castingCharacter, aBguCharacter, cachedDesc.TargetFilter) || !BGUFuncLibSelectTargetsCS.BGUIsSelectTargetByAffiliationFilter(castingCharacter, aBguCharacter, cachedDesc.AffiliationTypeFilter))
        {
            Logging.LogDebug("CurrentTarget As BGUCharacter is null in PatchOnCastImmobilize");
            return false;
        }

        var num = cachedDesc.TargetCount <= 0 ? 1 : cachedDesc.TargetCount;
        List<AActor> outActors = [];
        if (num > 1)
        {
            IList<int> list = [cachedDesc.RangeRadius];
            var owner2 = __instance.GetOwner();
            var baseLoc = aBguCharacter.BGUGetActorLocation();
            var targetFilter = cachedDesc.TargetFilter;
            var targetTypeFilter = cachedDesc.TargetTypeFilter;
            var affiliationTypeFilter = cachedDesc.AffiliationTypeFilter;
            BGUFuncLibSelectTargetsCS.BGUSelectTargetsInShape(castingCharacter, out outActors, owner2, baseLoc, ERangeType.Circle, -1, targetFilter, targetTypeFilter, affiliationTypeFilter, in list);
        }

        if (outActors.Contains(aBguCharacter))
        {
            outActors.Remove(aBguCharacter);
        }

        outActors.Insert(0, aBguCharacter);

        var num2 = 0;
        foreach (var item in outActors)
        {
            if (num2 >= num)
            {
                break;
            }

            if (BGUFunctionLibraryCS.BGUHasUnitState(item, EBGUUnitState.Dead))
            {
                continue;
            }

            if (BGUFunctionLibraryCS.BGUHasUnitSimpleState(item, EBGUSimpleState.ImmueImmobilizing))
            {
                var actorResID = BGU_DataUtil.GetActorResID(item);
                var fXAssetByResID = ImmobilizeUtils.GetFxAssetByResId(castingCharacter, cachedDesc.FailedFXs, actorResID, castImmobilizeData.ResId, castImmobilizeData);
                if (fXAssetByResID != null)
                {
                    BUS_EventCollectionCS.Get(item)?.Evt_RequestSpawnFXByDispConfigDA.Invoke(fXAssetByResID, out var _);
                }

                continue;
            }

            num2++;
            var actorResID2 = BGU_DataUtil.GetActorResID(item);
            if (BGW_LogUtil.LogIfNull(BGW_GameDB.GetUnitCommDesc(actorResID2), "BGW_GameDB.GetUnitCommDesc is null, ResID:%d", actorResID2))
            {
                continue;
            }

            var hasBuff = buffData.HasBuff(cachedDesc.GreatSageTalentActiveBuff);
            var immobilizeConfigInstance = ImmobilizeUtils.CreateImmobilizeConfig(item, castingCharacter, cachedDesc, castImmobilizeData.ResId, hasBuff, castImmobilizeData);
            BUS_EventCollectionCS.Get(item)?.Evt_TriggerImmobilize.Invoke(immobilizeConfigInstance);

            // broadcast trigger immobilize on targets
            if (DI.Instance.MappingPolicyDir.IsCharacterMapped(item, out var entity))
            {
                var sent = DI.Instance.MappedEvent.NotifyEcsIfApplicable(new TriggerImmobilizeEvent(entity.Value, castingMainEntity.Value, hasBuff), default(EmptyContext));
                if (sent)
                    Logging.LogDebug("Broadcasting trigger immobilize for target {Target}", item.GetName());
            }
        }

        return false;
    }
}

[HarmonyPatch(typeof(BUS_BeImmobilizedComp), "OnTickWithGroup")]
[HarmonyPatchCategory(PatchCategory.Connected)]
internal static class PatchImmobilizeOnTickWithGroup
{
    public static bool Prefix()
    {
        if (!DI.Instance.AreaState.InRoom)
            return true;

        if (DI.Instance.AreaState.IsMasterClient)
        {
            return true;
        }

        return false;
    }
}

[HarmonyPatch(typeof(BUS_BeImmobilizedComp), "RelieveImmobilized")]
[HarmonyPatchCategory(PatchCategory.Connected)]
internal static class PatchRelieveImmobilized
{
    public static bool Prefix(BUS_BeImmobilizedComp __instance)
    {
        if (!DI.Instance.AreaState.InRoom)
            return true;

        var owner = __instance.GetOwner();

        if (owner.IsNullOrDestroyed())
        {
            Logging.LogError("Owner is null or destroyed");
            return false;
        }

        if (!DI.Instance.MappingPolicyDir.IsCharacterMapped(owner, out var entity))
            return true;

        DI.Instance.MappedEvent.NotifyEcsIfApplicable(new RelieveImmobilizeEvent(entity.Value), default(EmptyContext));

        return DI.Instance.MappingPolicyDir.ForEvent<RelieveImmobilizeEvent, EmptyContext>().CanGameEventRunLocally(default);
    }
}

[HarmonyPatch(typeof(BUS_BeImmobilizedComp), "OnTriggerImmobilizedBreak")]
[HarmonyPatchCategory(PatchCategory.Connected)]
internal static class PatchOnTriggerImmobilizedBreak
{
    public static bool Prefix(BUS_BeImmobilizedComp __instance)
    {
        if (!DI.Instance.AreaState.InRoom)
            return true;

        var owner = __instance.GetOwner() as BGUCharacterCS;

        if (owner.IsNullOrDestroyed())
        {
            Logging.LogError("Owner is null or destroyed");
            return false;
        }

        // TODO: This used to be forcibly replaced by RelieveImmobilize, find out why
        if (DI.Instance.MappingPolicyDir.IsCharacterMapped(owner, out var entity))
        {
            DI.Instance.MappedEvent.NotifyEcsIfApplicable(new RelieveImmobilizeEvent(entity.Value), default(EmptyContext));
        }
        
        return DI.Instance.MappingPolicyDir.ForEvent<RelieveImmobilizeEvent, EmptyContext>().CanGameEventRunLocally(default);
    }
}

[HarmonyPatch(typeof(BUS_PhantomRushComp), "OnTriggerPhantomRush")]
[HarmonyPatchCategory(PatchCategory.Connected)]
internal static class PatchOnTriggerPhantomRush
{
    public static bool Prefix(
        BUS_PhantomRushComp __instance,
        IBUC_SimpleStateData ___SimpleStateData,
        IBUC_UnitStateData ___UnitStateData,
        BUC_PhantomRushData ___PhantomRushData,
        IBUC_SkillInstsData ___SkillInstsData,
        ESkillDirection PhantomRushDir)
    {
        if (!DI.Instance.AreaState.InRoom)
            return true;

        var playerState = DI.Instance.PlayerState;
        var areaState = DI.Instance.AreaState;

        var areaEntity = areaState.CurrentArea;
        if (areaEntity == null)
            return true;

        var owner = __instance.GetOwner();

        if (owner.IsNullOrDestroyed())
        {
            Logging.LogError("Owner is null or destroyed");
            return false;
        }

        if (owner == playerState.LocalMainCharacter?.Pawn)
            return true;

        // Modified original implementation
        var GetActualUseConfigIDMethod = AccessTools.Method(typeof(BUS_PhantomRushComp), "GetActualUseConfigID");
        if (GetActualUseConfigIDMethod == null)
        {
            Logging.LogError("GetActualUseConfigID method info is null");
            return false;
        }

        var BUSEventCollection = BUS_EventCollectionCS.Get(owner);
        var BGSEventCollection = BGS_GSEventCollection.Get(owner);
        var aCharacter = owner as ACharacter;
        if (aCharacter == null || ___SimpleStateData.HasSimpleState(EBGUSimpleState.PhantomRush))
        {
            Logging.LogDebug("aCharacter is null or PhantomRush is already active");
            return false;
        }

        var phantomRushSkillConfigDesc = BGW_GameDB.GetPhantomRushSkillConfigDesc((int)GetActualUseConfigIDMethod.Invoke(__instance, null), owner);
        if (phantomRushSkillConfigDesc == null)
        {
            Logging.LogError("phantomRushSkillConfigDesc is null");
            return false;
        }

        __instance.PreloadAssetMgr.TryGetCachedResourceObj<BGWDataAsset_PhantomRushRelatedeSkillConfig>(phantomRushSkillConfigDesc.PhantomRushRelatedSkillConfigPath, ELoadResourceType.AsyncLoadAndCache, EAssetPriority.Medium);
        var Snapshot = default(FPoseSnapshot);
        aCharacter.Mesh.SnapshotPose(ref Snapshot);
        ___PhantomRushData.PoseSnapshot = Snapshot;
        var animInstance = aCharacter.Mesh.GetAnimInstance();
        var cBI = default(FContinueBehaviorInfo);
        if (animInstance != null)
        {
            var currentActiveMontage = animInstance.GetCurrentActiveMontage();
            if (currentActiveMontage != null)
            {
                if (___SimpleStateData.HasSimpleState(EBGUSimpleState.InAnimationSyncing))
                {
                    cBI.CBT = EContinueBehaviorType.AnimationSyncing;
                    cBI.MontagePos = animInstance.Montage_GetPosition(currentActiveMontage);
                    cBI.BeatbackMontage = currentActiveMontage;
                }
                else if (___UnitStateData.HasState(EBGUUnitState.Attacking))
                {
                    cBI.MontagePos = animInstance.Montage_GetPosition(currentActiveMontage);
                    cBI.CBT = EContinueBehaviorType.Skill;
                    cBI.SkillID = ___SkillInstsData.CurrentCastingSkillID;
                }
                else if (___UnitStateData.HasState(EBGUUnitState.Beatback))
                {
                    cBI.CBT = EContinueBehaviorType.Beatback;
                    cBI.MontagePos = animInstance.Montage_GetPosition(currentActiveMontage);
                    cBI.BeatbackMontage = currentActiveMontage;
                }
            }
        }

        BUSEventCollection.Evt_UnitSetSimpleState.Invoke(EBGUSimpleState.ForceSkill);
        BUSEventCollection.Evt_UnitCastSkillTry.Invoke(new FCastSkillInfo(phantomRushSkillConfigDesc.PhantomRushSkillID, ECastSkillSourceType.PhantomRush, _HasSetSkillBaseTarget: false, PhantomRushDir)
        {
            NeedCheckSkillCanCast = true
        });
        BUSEventCollection.Evt_UnitSetSimpleState.Invoke(EBGUSimpleState.ForceSkill, IsRemove: true);
        if (___SkillInstsData.GetLastSkillCastResult() != 0)
        {
            Logging.LogDebug("GetLastSkillCastResult was not success");
            return false;
        }

        BUSEventCollection.Evt_ClearAbnormalState.Invoke([
            EAbnormalStateType.Abnormal_Burn,
            EAbnormalStateType.Abnormal_Freeze,
            EAbnormalStateType.Abnormal_Poison,
            EAbnormalStateType.Abnormal_Thunder
        ]);
        var phantomRushSummonID = phantomRushSkillConfigDesc.PhantomRushSummonID;
        BUSEventCollection.Evt_SummonSkillCastByPhantomRush.Invoke(phantomRushSummonID, cBI);
        BUSEventCollection.Evt_UnitSetSimpleState.Invoke(EBGUSimpleState.PhantomRush);
        foreach (var phantomRushBeginAddBuffID in phantomRushSkillConfigDesc.PhantomRushBeginAddBuffIDList)
        {
            BUSEventCollection.Evt_BuffAdd.Invoke(phantomRushBeginAddBuffID, owner, owner, -1f, EBuffSourceType.PhantomRush);
        }

        ___PhantomRushData.PhantomRushTimer = phantomRushSkillConfigDesc.PhantomRushDuration;
        ___PhantomRushData.PhantomRushNoMagicProtectTimer = 1f;
        BGSEventCollection?.Evt_BGS_ClearAttachedProjectiles_OnUnit.Invoke(owner);

        return false;
    }

    public static void Postfix(BUS_PhantomRushComp __instance, IBUC_SimpleStateData ___SimpleStateData, ESkillDirection PhantomRushDir)
    {
        if (!DI.Instance.AreaState.InRoom)
            return;

        // PhantomRush not triggered - skip
        if (!___SimpleStateData.HasSimpleState(EBGUSimpleState.PhantomRush))
        {
            return;
        }

        var playerState = DI.Instance.PlayerState;
        var owner = __instance.GetOwner();

        if (owner.IsNullOrDestroyed())
        {
            Logging.LogError("Owner is null or destroyed");
            return;
        }

        var mainEntity = DI.Instance.PawnState.GetEntityByPlayerActor(owner);
        if (mainEntity != null && mainEntity != playerState.LocalMainCharacter && playerState.LocalPlayerEntity != null)
        {
            DI.Instance.ModeManager.SetPlayerVisibility(mainEntity.Value, false);
        }
    }
}

[HarmonyPatch(typeof(BUS_SkillInstsCompSvr), "OnUnitCastSkillTry", typeof(FCastSkillInfo))]
[HarmonyPatchCategory(PatchCategory.Connected)]
internal static class PatchOnUnitCastSkillTry
{
    public static void Postfix(FCastSkillInfo CSI, BUC_SkillInstsData ___SkillInstsData, BUS_SkillInstsCompSvr __instance)
    {
        if (!DI.Instance.AreaState.InRoom)
            return;

        var owner = __instance.GetOwner();

        if (___SkillInstsData.GetLastSkillCastResult() != 0)
        {
            Logging.LogDebug("GetLastSkillCastResult was not success");
            return;
        }

        if (CSI.SourceType == ECastSkillSourceType.PhantomRush)
        {
            if (DI.Instance.MappingPolicyDir.IsMainCharacterMapped(owner, out var entity))
            {
                var sent = DI.Instance.MappedEvent.NotifyEcsIfApplicable(new PhantomRushEvent(entity.Value, CSI.SkillDirection), entity.Value.Entity);
                if (sent)
                    Logging.LogDebug("Sending phantom rush with direction: {Direction}", CSI.SkillDirection);
            }
        }
        else if (CSI is { SourceType: ECastSkillSourceType.CBG, SkillID: 471236 })
        {
            if (DI.Instance.MappingPolicyDir.IsMonsterTamerMapped(owner as BGUCharacterCS, out var entity))
            {
                var sent = DI.Instance.MappedEvent.NotifyEcsIfApplicable(new CastSkillEvent(entity.Value, CSI.SkillID, CSI.SourceType), entity.Value.Entity);
                if (sent)
                    Logging.LogDebug("Sent CBG skill cast for skill {SkillId}", CSI.SkillID);
            }
        }
    }
}

[HarmonyPatch(typeof(BUS_PhantomRushComp), "ExitPhantomRush")]
[HarmonyPatchCategory(PatchCategory.Connected)]
internal static class PatchExitPhantomRush
{
    public static void Prefix(BUS_PhantomRushComp __instance)
    {
        if (!DI.Instance.AreaState.InRoom)
            return;

        var playerState = DI.Instance.PlayerState;
        var owner = __instance.GetOwner();

        if (owner.IsNullOrDestroyed())
        {
            Logging.LogError("Owner is null or destroyed");
            return;
        }

        if (!DI.Instance.MappingPolicyDir.IsMainCharacterMapped(owner, out var mainEntity))
            return;

        var main = mainEntity.Value.GetState();

        var sent = DI.Instance.MappedEvent.NotifyEcsIfApplicable(new ExitPhantomRushEvent(mainEntity.Value), mainEntity.Value.Entity);
        if (sent)
            Logging.LogDebug("Broadcasting phantom rush exit for player {Nickname}", mainEntity.Value.GetNickname().Nickname);

        // show other players again
        var playerEntity = DI.Instance.PlayerState.GetPlayerById(main.PlayerId);
        if (mainEntity != playerState.LocalMainCharacter && playerEntity.HasValue)
        {
            DI.Instance.ModeManager.SetPlayerVisibility(mainEntity.Value, true);
        }
    }
}

[HarmonyPatch(typeof(BUFFPlayerWinePartnerAttr), "Apply")]
[HarmonyPatchCategory(PatchCategory.Connected)]
internal static class PatchBuffPlayerWinePartnerAttr
{
    public static bool Prefix(AActor Target, out float OutAbs, out float OutMul)
    {
        OutAbs = 0.0f;
        OutMul = 0.0f;

        if (!DI.Instance.AreaState.InRoom)
            return true;

        var abguCharacter = Target as ABGUCharacter;
        if (abguCharacter != null)
        {
            var readOnlyData = BGU_DataUtil.GetReadOnlyData<IBPC_PlayerRoleData, BPC_PlayerRoleData>(abguCharacter.GetController());
            if (readOnlyData is { RoleData: null })
                return false;
        }

        return true;
    }
}

[HarmonyPatch]
[HarmonyPatchCategory(PatchCategory.Connected)]
internal static class TransformationPatch
{
    [HarmonyTargetMethodHint("b1.BUS_PlayerTransComp", "TransferData")]
    private static MethodBase TargetMethod()
    {
        return AccessTools.Method("b1.BUS_PlayerTransComp:TransferData");
    }

    public static void Postfix(UActorCompBaseCS __instance, ABGUCharacter ToReplaceUnitInst)
    {
        if (!DI.Instance.AreaState.InRoom)
            return;

        var oldOwner = __instance.GetOwner() as APawn;
        if (oldOwner == null)
        {
            Logging.LogDebug("Skipping transformation because the owner is not a pawn");
            return;
        }

        var newOwner = ToReplaceUnitInst as BGUCharacterCS;
        if (newOwner == null)
        {
            Logging.LogDebug("Skipping transformation because the new owner is not a BGUCharacterCS");
            return;
        }

        var mainEntity = DI.Instance.PawnState.GetEntityByPlayerActor(oldOwner);

        if (mainEntity == null)
        {
            Logging.LogDebug("Skipping transformation of {OldOwner} because player state is null", oldOwner.GetName());
            return;
        }

        var main = mainEntity.Value.GetState();
        mainEntity.Value.SetPawn(newOwner, true);
        // update equipment
        EquipmentUtils.SetActorEquipment(newOwner, main.Equipment);
        Logging.LogDebug("Transformed {OldOwner} to {NewOwner}", oldOwner?.GetName(), newOwner?.GetName());
    }
}

[HarmonyPatch(typeof(BPC_BattleMainInfoData), "GetCommonDisabledState")]
[HarmonyPatchCategory(PatchCategory.Connected)]
internal class PatchLogs4
{
    public static bool Prefix(BPC_BattleMainInfoData __instance, ref bool __result, out bool IsDisabled)
    {
        if (!DI.Instance.AreaState.InRoom)
        {
            IsDisabled = false;
            return true;
        }

        if (__instance.OwnerCharacter?.GetName() != GameUtils.GetControlledPawn()?.GetName())
        {
            __result = true;
            IsDisabled = false;
            return false;
        }

        IsDisabled = false;
        return true;
    }
}

[HarmonyPatchCategory(PatchCategory.Connected)]
internal class PatchOnTransBeginSpawnNewOne
{
    [HarmonyTargetMethodHint("b1.BUS_PlayerTransComp", "OnTransBeginSpawnNewOne")]
    private static MethodBase TargetMethod()
    {
        return AccessTools.Method("b1.BUS_PlayerTransComp:OnTransBeginSpawnNewOne");
    }

    public static void Prefix(
        UActorCompBaseCS __instance,
        int ToReplaceUnitResID,
        int ToReplaceUnitBornSkillID,
        bool EnableBlendViewTarget,
        EPlayerTransBeginType TransBeginType)
    {
        if (!DI.Instance.AreaState.InRoom)
            return;

        var playerState = DI.Instance.PlayerState;

        var pawn = __instance.GetOwner();
        if (DI.Instance.MappingPolicyDir.IsMainCharacterMapped(pawn, out var mainEntity))
        {
            var sent = DI.Instance.MappedEvent.NotifyEcsIfApplicable(new PlayerTransBeginEvent(mainEntity.Value, ToReplaceUnitResID, ToReplaceUnitBornSkillID, EnableBlendViewTarget, TransBeginType), mainEntity.Value.Entity);
            if (sent)
            {
                Logging.LogDebug("OnTransBeginSpawnNewOne: Sending transform for player {Name} to unit with id {UnitId}", playerState.LocalMainCharacter?.GetNickname().Nickname, ToReplaceUnitResID);
            }
            
            if (DI.Instance.MappedField.CanLoadFromGame<MainCharacterComponent>(mainEntity.Value, out var loadState))
            {
                loadState.SetFromGame(MainCharacterComponent.Fields.IsTransformed, true);
            }
        }
    }
}

[HarmonyPatchCategory(PatchCategory.Connected)]
internal class PatchOnTransBackSpawnNewOne
{
    [HarmonyTargetMethodHint("b1.BUS_PlayerTransComp", "OnTransBackSpawnNewOne")]
    private static MethodBase TargetMethod()
    {
        return AccessTools.Method("b1.BUS_PlayerTransComp:OnTransBackSpawnNewOne");
    }

    public static void Prefix(
        UActorCompBaseCS __instance,
        int ToReplaceUnitResID,
        int ToReplaceUnitBornSkillID,
        bool EnableBlendViewTarget,
        EPlayerTransEndType TransEndType,
        out object? __state)
    {
        if (!DI.Instance.AreaState.InRoom)
        {
            __state = null;
            return;
        }

        var pawn = __instance.GetOwner();

        if (DI.Instance.MappingPolicyDir.IsMainCharacterMapped(pawn, out var mainEntity))
        {
            var sent = DI.Instance.MappedEvent.NotifyEcsIfApplicable(new PlayerTransEndEvent(mainEntity.Value, ToReplaceUnitResID, ToReplaceUnitBornSkillID, EnableBlendViewTarget, TransEndType), mainEntity.Value.Entity);
            if (sent)
                Logging.LogDebug("OnTransBackSpawnNewOne: Sending transform for player {Name} to unit with id {UnitId}", mainEntity.Value.GetNickname().Nickname, ToReplaceUnitResID);
        }

        __state = mainEntity;
    }

    public static void Postfix(object? __state)
    {
        if (!DI.Instance.AreaState.InRoom)
            return;

        var state = (MainCharacterEntity?)__state;
        if (state.HasValue)
        {
            if (DI.Instance.MappedField.CanLoadFromGame<MainCharacterComponent>(state.Value, out var loadState))
            {
                loadState.SetFromGame(MainCharacterComponent.Fields.IsTransformed, false);
            }

            // TODO: Used to load HpMax, not Hp, possibly healing the player to full. Check if this is intended and if not - change it back.
            if (DI.Instance.MappedField.CanLoadFromGame<HpComponent>(state.Value, out var load))
            {
                var attrContainer = BGU_DataUtil.GetReadOnlyData<BUC_AttrContainer>(state.Value.Pawn);
                load.LoadFromGame(HpComponent.Fields.Hp.In<BUC_AttrContainer>(), attrContainer);
            }
        }
    }
}

[HarmonyPatchCategory(PatchCategory.Connected)]
internal class PatchSpawnAndPossess
{
    [HarmonyTargetMethodHint("b1.BUS_PlayerTransComp", "SpawnAndPossessTransUnit")]
    private static MethodBase TargetMethod()
    {
        return AccessTools.Method("b1.BUS_PlayerTransComp:SpawnAndPossessTransUnit");
    }

    public static bool Prefix(
        UActorCompBaseCS __instance,
        BUC_PlayerTransData ___PlayerTransData,
        BGUCharacterCS ___OwnerAsCharacterCS,
        AActor ___Owner,
        ref APawn? __result,
        UClass CharacterClass,
        FTransform BornTransform,
        BGUFuncLibPlayer.SpawnControlledPawnBlendParam SpawnControlledPawnBlendParam,
        int ToReplaceUnitResID)
    {
        if (!DI.Instance.AreaState.InRoom)
            return true;

        var bgwEventCollection = Traverse.Create(__instance).Property<BGW_EventCollection>("BGWEventCollection").Value;
        var busEventCollection = Traverse.Create(__instance).Property<BUS_GSEventCollection>("BUSEventCollection").Value;

        APawn? newPawn = null;
        var unitTransType = ___PlayerTransData.TransTypeCached == EPlayerTransEndType.None ? EPlayerTransEndType.CastSpell : ___PlayerTransData.TransTypeCached;
        bgwEventCollection.Evt_BGW_UnitTrans(___Owner, unitTransType);
        busEventCollection.Evt_NotifyUnitTrans_BeforePosses.Invoke(unitTransType);
        var instigator = ___OwnerAsCharacterCS.Instigator;
        var controller = instigator != null ? instigator.GetController() : null;

        if (controller == null)
        {
            Logging.LogError("Controller is null, cannot transform");
            __result = null;
            return false;
        }

        var playerController = controller as ABGPPlayerController;

        SpawnTransform(controller, CharacterClass, BornTransform, Pawn =>
        {
            newPawn = Pawn;
            if (playerController != null)
            {
                BPS_EventCollectionCS.Get(playerController)?.Evt_PlayerActorSpawn.Invoke();
                BPS_EventCollectionCS.Get(playerController)?.Evt_BPS_SwitchPlayerTransState.Invoke(___Owner, ToReplaceUnitResID);
            }
        }, SpawnControlledPawnBlendParam);

        if (playerController != null)
        {
            if (!SpawnControlledPawnBlendParam.EnableBlendViewTarget)
                playerController.SetViewTargetWithBlend(___Owner);
        }

        __result = newPawn;
        return false;
    }

    private static APawn? SpawnTransform(AController controller, UClass pawnClass, FTransform spawnTransform, Action<APawn> beforeBeginPlayCb, BGUFuncLibPlayer.SpawnControlledPawnBlendParam blendParam)
    {
        var controlledPawn = controller.GetControlledPawn();
        var playerController = controller as ABGPPlayerController;
        var newPawn = BGU_UnrealActorUtil.BGUBeginDeferredActorSpawnFromClass(controller.World, (TSubclassOf<AActor>)pawnClass, spawnTransform, ESpawnActorCollisionHandlingMethod.AlwaysSpawn, null) as APawn;
        if (newPawn == null)
        {
            Logging.LogError("New pawn is null, cannot transform");
            return null;
        }

        if (blendParam.NeedBlend && playerController != null)
            playerController.OnPossessWithViewTargetBlend(newPawn, blendParam.PossessBlendTime, (EViewTargetBlendFunction)blendParam.PossessBlendFunc, blendParam.PossessBlendExp, true, blendParam.EnableBlendViewTarget);
        else
            controller.Possess(newPawn);
        beforeBeginPlayCb(newPawn);
        var actor = (ACharacter)newPawn;

        var mainPlayerPawn = GameUtils.GetControlledPawn();
        var mainPlayerController = GameUtils.GetPlayerController()!;
        var isNonLocalTransform = false;
        var cameraRotation = FRotator.ZeroRotator;
        if (controller != mainPlayerController && mainPlayerPawn != null)
        {
            // Set player controller to transforming player
            isNonLocalTransform = true;
            cameraRotation = mainPlayerController.GetControlRotation();
            GameUtils.PossessPawn(mainPlayerController, newPawn, mainPlayerPawn);
        }

        actor.CapsuleComponent.SetGenerateOverlapEvents(false);
        actor.CapsuleComponent.SetGenerateOverlapEvents(false);
        BGU_UnrealActorUtil.BGUFinishSpawningActorAndECSBeginPlay(controller, newPawn, spawnTransform);
        if (isNonLocalTransform && mainPlayerPawn != null)
        {
            // Set player controller back to main player
            GameUtils.PossesPawnWithViewTarget(DI.Instance.FreeCameraManager, mainPlayerController, mainPlayerPawn, newPawn, cameraRotation);
            controller.Possess(newPawn);
        }

        if (playerController != null)
        {
            BPS_GSEventCollection.Get(playerController).Evt_BPS_OnControlledPawnChange.Invoke(newPawn);
            BGS_EventCollectionCS.Get(playerController)?.Evt_NotifyPossessEntityChanged.Invoke(controlledPawn.ToEntity(), newPawn.ToEntity());
        }

        actor.CapsuleComponent.SetGenerateOverlapEvents(true);
        actor.CapsuleComponent.SetGenerateOverlapEvents(true);
        UGSE_ActorFuncLib.UpdateActorOverlaps(actor);
        return newPawn;
    }
}

[HarmonyPatch(typeof(BUS_TransGuideComp), "UpdateTransGuideData")]
[HarmonyPatchCategory(PatchCategory.Connected)]
internal class PatchUpdateTransGuideData
{
    public static bool Prefix(BUS_TransGuideComp __instance)
    {
        if (!DI.Instance.AreaState.InRoom)
            return true;

        if (__instance.GetOwner() != GameUtils.GetControlledPawn())
        {
            return false;
        }

        return true;
    }
}

[HarmonyPatch(typeof(BUS_TransPlayerDataBindComp), "OnPostTransBindData")]
[HarmonyPatchCategory(PatchCategory.Connected)]
internal class PatchOnPostTransBindData
{
    public static bool Prefix(BUS_TransPlayerDataBindComp __instance)
    {
        if (!DI.Instance.AreaState.InRoom)
            return true;

        if (__instance.GetOwner() != GameUtils.GetControlledPawn())
        {
            return false;
        }

        return true;
    }
}

// TODO: Possibly synced by a buff, maybe we can disable this
[HarmonyPatch(typeof(BUS_IronBodyComp), "OnIronBodyStart")]
[HarmonyPatchCategory(PatchCategory.Connected)]
internal static class PatchOnIronBodyStart
{
    public static void Postfix(BUS_IronBodyComp __instance)
    {
        if (!DI.Instance.AreaState.InRoom)
            return;

        var owner = __instance.GetOwner();

        if (DI.Instance.MappingPolicyDir.IsCharacterMapped(owner, out var entity))
        {
            DI.Instance.MappedEvent.NotifyEcsIfApplicable(new IronBodyStartEvent(entity.Value), entity.Value);
        }
    }
}

[HarmonyPatch(typeof(BPS_BattleMainInfoComp), "OnPossessed")]
[HarmonyPatchCategory(PatchCategory.Connected)]
internal static class PatchBattleMainInfoCompOnPossessed
{
    public static bool Prefix(AActor? OldActor, AActor? CurActor)
    {
        if (!DI.Instance.AreaState.InRoom)
            return true;

        if (CurActor != null && CurActor == GameUtils.GetControlledPawn())
            return true;

        Logging.LogDebug("BPS_BattleMainInfoComp OnPossessed called, but the current actor is not the controlled pawn. OldActor: {OldActor}, CurActor: {CurActor}", OldActor?.GetName(), CurActor?.GetName());
        return false;
    }
}

[HarmonyPatch(typeof(BPS_InputSystem), "OnPossessed")]
[HarmonyPatchCategory(PatchCategory.Connected)]
internal static class PatchInputSystemOnPossessed
{
    public static bool Prefix(AActor? OldActor, AActor? CurActor)
    {
        if (!DI.Instance.AreaState.InRoom)
            return true;

        if (CurActor != null && CurActor == GameUtils.GetControlledPawn())
            return true;

        Logging.LogDebug("BPS_InputSystem OnPossessed called, but the current actor is not the controlled pawn. OldActor: {OldActor}, CurActor: {CurActor}", OldActor?.GetName(), CurActor?.GetName());
        return false;
    }
}

[HarmonyPatch(typeof(BPS_MultiTargetProjectileCtrComp), "OnPossessed")]
[HarmonyPatchCategory(PatchCategory.Connected)]
internal static class PatchMultiTargetOnPossessed
{
    public static bool Prefix(AActor? OldActor, AActor? CurActor)
    {
        if (!DI.Instance.AreaState.InRoom)
            return true;

        if (CurActor != null && CurActor == GameUtils.GetControlledPawn())
            return true;

        Logging.LogDebug("BPS_MultiTargetProjectileCtrComp OnPossessed called, but the current actor is not the controlled pawn. OldActor: {OldActor}, CurActor: {CurActor}", OldActor?.GetName(), CurActor?.GetName());
        return false;
    }
}

[HarmonyPatch(typeof(BUS_MagicallyChangeComp), "DoCastMagicallyChangeSkill_PendingCast")]
[HarmonyPatchCategory(PatchCategory.Connected)]
internal static class PatchDoCastMagicallyChangeSkill_PendingCast
{
    public static void Postfix(BUS_MagicallyChangeComp __instance, UBGWDataAsset? _Config, int _SkillID, int _RecoverSkillID, BUC_MagicallyChangeData ___MagicallyChangeData)
    {
        if (!DI.Instance.AreaState.InRoom)
            return;

        if (_Config == null)
            return;

        Logging.LogDebug("BUS_MagicallyChangeComp DoCastMagicallyChangeSkill_PendingCast called with Config Path: {Path}, SkillID: {SkillID}, RecoverSkillID: {RecoverSkillID}, CurVigorSkillID {CurVigorSkillID}", _Config.PathName, _SkillID, _RecoverSkillID, ___MagicallyChangeData.CurVigorSkillID);

        var owner = __instance.GetOwner();
        if (DI.Instance.MappingPolicyDir.IsMainCharacterMapped(owner, out var entity))
        {
            DI.Instance.MappedEvent.NotifyEcsIfApplicable(new TriggerMagicallyChangeEvent(entity.Value, _Config.PathName, _SkillID, _RecoverSkillID, ___MagicallyChangeData.CurVigorSkillID, ___MagicallyChangeData.CastReason), entity.Value.Entity);
        }
    }
}

[HarmonyPatch(typeof(BUS_MagicallyChangeComp), "PendingReset")]
[HarmonyPatchCategory(PatchCategory.Connected)]
internal static class PatchPendingReset
{
    public static void Postfix(BUS_MagicallyChangeComp __instance, EResetReason_MagicallyChange Reason)
    {
        if (!DI.Instance.AreaState.InRoom)
            return;

        Logging.LogDebug("BUS_MagicallyChangeComp PendingReset called with reason: {Reason}", Reason);

        var owner = __instance.GetOwner();
        if (DI.Instance.MappingPolicyDir.IsMainCharacterMapped(owner, out var entity))
        {
            DI.Instance.MappedEvent.NotifyEcsIfApplicable(new ResetMagicallyChangeEvent(entity.Value, Reason), entity.Value.Entity);
        }
    }
}

[HarmonyPatch]
[HarmonyPatchCategory(PatchCategory.Connected)]
internal static class PatchOnSweepCheckHit
{
    [HarmonyTargetMethodHint("b1.BUS_SweepCheckHitComp", "OnSweepCheckHit")]
    private static MethodBase TargetMethod()
    {
        return AccessTools.Method("b1.BUS_SweepCheckHitComp:OnSweepCheckHit");
    }

    public static bool Prefix(UActorCompBaseCS __instance, AActor Victim)
    {
        if (!DI.Instance.AreaState.InRoom)
            return true;

        if (__instance.GetOwner() is BGUCharacterCS casterCharacter && Victim is BGUCharacterCS targetCharacter)
        {
            if (casterCharacter.GetTeamIDInCS() == targetCharacter.GetTeamIDInCS())
            {
                return false;
            }
        }

        return true;
    }
}

[HarmonyPatch(typeof(FInputMappingContextProcessor), nameof(FInputMappingContextProcessor.SetCloudInputEnable))]
[HarmonyPatchCategory(PatchCategory.Global)]
internal static class PatchSetCloudInputEnable
{
    public static bool Prefix(bool bEnable)
    {
        if (!DI.Instance.AreaState.InRoom)
            return true;

        var players = DI.Instance.PlayerState;
        var cloudMoveData = BGU_DataUtil.GetUnPersistentReadOnlyData<BUC_CloudMoveData>(players.LocalMainCharacter?.Pawn);
        if (cloudMoveData == null)
        {
            return true;
        }

        return cloudMoveData.IsCloudMoveEnabled == bEnable;
    }
}