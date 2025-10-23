using System;
using System.Collections.Generic;
using System.Reflection;
using b1;
using b1.BGW;
using BtlB1;
using BtlShare;
using HarmonyLib;
using PreludeLib.Attributes;
using UnrealEngine.Engine;
using UnrealEngine.Runtime;
using WukongMp.Api.Compat;
using WukongMp.Api.Configuration;
using WukongMp.Api.DTO;
using WukongMp.Api.ECS.Entities;
using WukongMp.Api.WukongUtils;

namespace WukongMp.Api.Patches;

[HarmonyPatch(typeof(BUS_PlayerInputActionComp), "TriggerMagicSkill")]
[HarmonyPatchCategory(Constants.ConnectedPatches)]
public static class PatchTriggerMagicSkill
{
    public static bool Prefix(int SkillID)
    {
        if (!DI.Instance.AreaState.InRoom)
            return true;

        return !SkillsUtils.IsSkillBlacklisted(SkillID) && (DI.Instance.PVP?.IsSkillEnabledInPVP(SkillID) ?? true);
    }
}

[HarmonyPatch(typeof(BUS_PlayerInputActionComp), "TriggerItemSkill")]
[HarmonyPatchCategory(Constants.ConnectedPatches)]
public static class PatchTriggerItemSkill
{
    public static bool Prefix(BUS_PlayerInputActionComp __instance)
    {
        if (Constants.IsCoop)
            return true;

        var areaState = DI.Instance.AreaState;
        var lastSkill = Traverse.Create(__instance).Field("ComboCacheData").Property<int>("LastItemSkillID").Value;

        if (areaState.CurrentArea == null)
            return true;

        var areaEntity = areaState.CurrentArea;
        ref var room = ref areaEntity.Value.GetRoom();

        switch (lastSkill)
        {
            case Constants.GourdSkillId when !room.GourdAllowed:
            case Constants.ConsumableBuffSkillId when !room.ConsumablesAllowed:
                return false;
            default:
                return true;
        }
    }
}

[HarmonyPatch]
[HarmonyPatchCategory(Constants.ConnectedPatches)]
public static class PatchDoPoleDrink
{
    [HarmonyTargetMethodHint("b1.BUS_PoleDrinkComp", "DoPoleDrink")]
    private static MethodBase TargetMethod()
    {
        return AccessTools.Method("b1.BUS_PoleDrinkComp:DoPoleDrink");
    }

    public static bool Prefix()
    {
        if (!DI.Instance.AreaState.InRoom)
            return true;

        var areaState = DI.Instance.AreaState;

        // FIXME: We no longer need the `InRoom` check because this is the same as checking isf `areaState.CurrentArea` is not null
        if (areaState.CurrentArea == null)
            return true;

        var areaEntity = areaState.CurrentArea;
        ref var room = ref areaEntity.Value.GetRoom();

        return room.GourdAllowed;
    }
}

[HarmonyPatch(typeof(BUS_CastImmobilizeComp), "OnCastImmobilize")]
[HarmonyPatchCategory(Constants.ConnectedPatches)]
public static class PatchOnCastImmobilize
{
    public static bool Prefix(int ConfigID, BUS_CastImmobilizeComp __instance)
    {
        if (!DI.Instance.AreaState.InRoom)
            return true;

        var playerState = DI.Instance.PlayerState;

        // get properties
        MethodInfo getter = AccessTools.PropertyGetter(typeof(BUS_CastImmobilizeComp), "CastImmobilizeData");
        BUC_CastImmobilizeData CastImmobilizeData = (BUC_CastImmobilizeData)getter.Invoke(__instance, null);
        getter = AccessTools.PropertyGetter(typeof(BUS_CastImmobilizeComp), "TargetInfoData");
        IBUC_TargetInfoData TargetInfoData = (IBUC_TargetInfoData)getter.Invoke(__instance, null);
        getter = AccessTools.PropertyGetter(typeof(BUS_CastImmobilizeComp), "PassiveSkillData");
        IBUC_PassiveSkillData PassiveSkillData = (IBUC_PassiveSkillData)getter.Invoke(__instance, null);
        getter = AccessTools.PropertyGetter(typeof(BUS_CastImmobilizeComp), "BuffData");
        IBUC_BuffData BuffData = (IBUC_BuffData)getter.Invoke(__instance, null);

        AActor castingCharacter = __instance.GetOwner();

        if (castingCharacter.IsNullOrDestroyed())
        {
            Logging.LogError("Owner is null or destroyed");
            return false;
        }

        var castingMainEntity = DI.Instance.PawnState.GetEntityByPlayerPawn(castingCharacter);

        if (!DI.Instance.AreaState.IsMasterClient)
        {
            if (!castingMainEntity.HasValue)
                return false;

            ref var castingMain = ref castingMainEntity.Value.GetState();

            // Broadcast that you have cast a spell
            if (castingMain.PlayerId == playerState.LocalMainCharacter?.GetState().PlayerId)
            {
                // target doesn't matter, not evaluated
                DI.Instance.Rpc.SendCastImmobilize(castingMainEntity.Value.GetMeta().NetId);
            }

            return false;
        }

        if (ConfigID == 0)
        {
            ConfigID = CastImmobilizeData.ResId;
        }

        if (!PassiveSkillData.TryGetCachedDesc<FUStImmobilizeSkillConfigDesc>(ConfigID, out var cachedDesc) || BGW_LogUtil.LogIfNull(castingCharacter as ABGUCharacter, "CurCharacter is null"))
        {
            return false;
        }

        var aBGUCharacter = TargetInfoData.GetSkillBaseTarget().LockTargetActor as ABGUCharacter;
        if (aBGUCharacter == null)
        {
            aBGUCharacter = TargetInfoData.GetTargetInfo().LockTargetActor as ABGUCharacter;
        }

        if (aBGUCharacter == null || !BGUFuncLibSelectTargetsCS.BGUIsSelectTargetByTeamFilter(castingCharacter, aBGUCharacter, cachedDesc.TargetFilter) || !BGUFuncLibSelectTargetsCS.BGUIsSelectTargetByAffiliationFilter(castingCharacter, aBGUCharacter, cachedDesc.AffiliationTypeFilter))
        {
            Logging.LogDebug("CurrentTarget As BGUCharacter is null in PatchOnCastImmobilize");
            return false;
        }

        int num = ((cachedDesc.TargetCount <= 0) ? 1 : cachedDesc.TargetCount);
        List<AActor> outActors = new();
        if (num > 1)
        {
            List<int> list = [cachedDesc.RangeRadius];
            AActor owner2 = __instance.GetOwner();
            FVector baseLoc = aBGUCharacter.BGUGetActorLocation();
            int targetFilter = cachedDesc.TargetFilter;
            int targetTypeFilter = cachedDesc.TargetTypeFilter;
            int affiliationTypeFilter = cachedDesc.AffiliationTypeFilter;
            IList<int> Prams = list;
            BGUFuncLibSelectTargetsCS.BGUSelectTargetsInShape(castingCharacter, out outActors, owner2, baseLoc, ERangeType.Circle, -1, targetFilter, targetTypeFilter, affiliationTypeFilter, in Prams);
        }

        if (outActors.Contains(aBGUCharacter))
        {
            outActors.Remove(aBGUCharacter);
        }

        outActors.Insert(0, aBGUCharacter);

        int num2 = 0;
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
                int actorResID = BGU_DataUtil.GetActorResID(item);
                var fXAssetByResID = ImmobilizeUtils.GetFxAssetByResId(castingCharacter, cachedDesc.FailedFXs, actorResID, CastImmobilizeData.ResId, CastImmobilizeData);
                if (fXAssetByResID != null)
                {
                    BUS_EventCollectionCS.Get(item)?.Evt_RequestSpawnFXByDispConfigDA.Invoke(fXAssetByResID, out var _);
                }

                continue;
            }

            num2++;
            int actorResID2 = BGU_DataUtil.GetActorResID(item);
            if (BGW_LogUtil.LogIfNull(BGW_GameDB.GetUnitCommDesc(actorResID2), "BGW_GameDB.GetUnitCommDesc is null, ResID:%d", actorResID2))
            {
                continue;
            }

            var hasBuff = BuffData.HasBuff(cachedDesc.GreatSageTalentActiveBuff);
            ImmobilizeConfigInstance immobilizeConfigInstance = ImmobilizeUtils.CreateImmobilizeConfig(item, castingCharacter, cachedDesc, CastImmobilizeData.ResId, hasBuff, CastImmobilizeData);
            BUS_EventCollectionCS.Get(item)?.Evt_TriggerImmobilize.Invoke(immobilizeConfigInstance);

            // broadcast
            var immobilizedMainEntity = DI.Instance.PawnState.GetEntityByPlayerPawn(item);
            var immobilizedTamerEntity = DI.Instance.PawnState.GetEntityByTamerMonster(item);

            if ((immobilizedMainEntity != null || immobilizedTamerEntity.HasValue) && castingMainEntity != null)
            {
                Logging.LogDebug("Broadcasting trigger immobilize");
                var netId = immobilizedMainEntity == null
                    ? immobilizedTamerEntity!.Value.GetMeta().NetId
                    : immobilizedMainEntity.Value.GetMeta().NetId;

                DI.Instance.Rpc.SendTriggerImmobilize(new TriggerImmobilizeData(netId, castingMainEntity.Value.GetMeta().NetId, hasBuff));
            }
        }

        return false;
    }
}

[HarmonyPatch(typeof(BUS_BeImmobilizedComp), "OnTickWithGroup")]
[HarmonyPatchCategory(Constants.ConnectedPatches)]
public static class PatchImmobilizeOnTickWithGroup
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
[HarmonyPatchCategory(Constants.ConnectedPatches)]
public static class PatchRelieveImmobilized
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

        var mainEntity = DI.Instance.PawnState.GetEntityByPlayerPawn(owner);
        var tamerEntity = DI.Instance.PawnState.GetEntityByTamerMonster(owner);

        if (mainEntity == null && !tamerEntity.HasValue)
        {
            return true;
        }

        var netId = mainEntity != null ? mainEntity.Value.GetMeta().NetId : tamerEntity!.Value.GetMeta().NetId;

        if (DI.Instance.AreaState.IsMasterClient)
        {
            DI.Instance.Rpc.SendRelieveImmobilize(netId);
            return true;
        }

        if (mainEntity != null)
        {
            ref var localMain = ref mainEntity.Value.GetLocalState();

            if (!localMain.RunImmobilizePatches)
            {
                return false;
            }

            localMain.RunImmobilizePatches = false;
            return true;
        }

        ref var localTamer = ref tamerEntity!.Value.GetLocalTamer();

        if (!localTamer.RunImmobilizePatches)
        {
            return false;
        }

        localTamer.RunImmobilizePatches = false;
        return true;
    }
}

[HarmonyPatch(typeof(BUS_BeImmobilizedComp), "OnTriggerImmobilizedBreak")]
[HarmonyPatchCategory(Constants.ConnectedPatches)]
public static class PatchOnTriggerImmobilizedBreak
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

        if (DI.Instance.AreaState.IsMasterClient)
        {
            var mainEntity = DI.Instance.PawnState.GetEntityByPlayerPawn(owner);

            if (mainEntity != null)
            {
                DI.Instance.Rpc.SendRelieveImmobilize(mainEntity.Value.GetMeta().NetId);
                BUS_EventCollectionCS.Get(mainEntity.Value.GetLocalState().Pawn)?.Evt_RelieveImmobilized.Invoke();
                return false;
            }

            var entity = DI.Instance.PawnState.GetEntityByTamerMonster(owner);

            if (entity.HasValue)
            {
                ref var meta = ref entity.Value.GetMeta();
                ref var localTamer = ref entity.Value.GetLocalTamer();

                DI.Instance.Rpc.SendRelieveImmobilize(meta.NetId);
                BUS_EventCollectionCS.Get(localTamer.Pawn)?.Evt_RelieveImmobilized.Invoke();
            }

            Logging.LogDebug("Character state is null - continuing standard execution");
            return true;
        }

        return false;
    }
}

[HarmonyPatch(typeof(BUS_PhantomRushComp), "OnTriggerPhantomRush")]
[HarmonyPatchCategory(Constants.ConnectedPatches)]
public static class PatchOnTriggerPhantomRush
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

        ref var room = ref areaEntity.Value.GetRoom();

        if (!room.PhantomRushAllowed)
        {
            return false;
        }

        AActor owner = __instance.GetOwner();

        if (owner.IsNullOrDestroyed())
        {
            Logging.LogError("Owner is null or destroyed");
            return false;
        }

        if (owner == playerState.LocalMainCharacter?.GetLocalState().Pawn)
            return true;

        // Modified original implementation
        MethodInfo GetActualUseConfigIDMethod = AccessTools.Method(typeof(BUS_PhantomRushComp), "GetActualUseConfigID");
        if (GetActualUseConfigIDMethod == null)
        {
            Logging.LogError("GetActualUseConfigID method info is null");
            return false;
        }

        BUS_GSEventCollection BUSEventCollection = BUS_EventCollectionCS.Get(owner);
        BGS_GSEventCollection BGSEventCollection = BGS_GSEventCollection.Get(owner);
        var aCharacter = owner as ACharacter;
        if (aCharacter == null || ___SimpleStateData.HasSimpleState(EBGUSimpleState.PhantomRush))
        {
            Logging.LogDebug("aCharacter is null or PhantomRush is already active");
            return false;
        }

        FUStPhantomRushSkillConfigDesc phantomRushSkillConfigDesc = BGW_GameDB.GetPhantomRushSkillConfigDesc((int)GetActualUseConfigIDMethod.Invoke(__instance, null), owner);
        if (phantomRushSkillConfigDesc == null)
        {
            Logging.LogError("phantomRushSkillConfigDesc is null");
            return false;
        }

        __instance.PreloadAssetMgr.TryGetCachedResourceObj<BGWDataAsset_PhantomRushRelatedeSkillConfig>(phantomRushSkillConfigDesc.PhantomRushRelatedSkillConfigPath, ELoadResourceType.AsyncLoadAndCache, EAssetPriority.Medium);
        FPoseSnapshot Snapshot = default(FPoseSnapshot);
        aCharacter.Mesh.SnapshotPose(ref Snapshot);
        ___PhantomRushData.PoseSnapshot = Snapshot;
        UAnimInstance animInstance = aCharacter.Mesh.GetAnimInstance();
        FContinueBehaviorInfo cBI = default(FContinueBehaviorInfo);
        if (animInstance != null)
        {
            UAnimMontage currentActiveMontage = animInstance.GetCurrentActiveMontage();
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
        int phantomRushSummonID = phantomRushSkillConfigDesc.PhantomRushSummonID;
        BUSEventCollection.Evt_SummonSkillCastByPhantomRush.Invoke(phantomRushSummonID, cBI);
        BUSEventCollection.Evt_UnitSetSimpleState.Invoke(EBGUSimpleState.PhantomRush);
        foreach (int phantomRushBeginAddBuffID in phantomRushSkillConfigDesc.PhantomRushBeginAddBuffIDList)
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

        var mainEntity = DI.Instance.PawnState.GetEntityByPlayerPawn(owner);
        if (mainEntity != null && mainEntity != playerState.LocalMainCharacter && playerState.LocalPlayerEntity != null)
        {
            DI.Instance.ModeManager.SetPlayerVisibility(playerState.LocalPlayerEntity.Value, mainEntity.Value, false);
        }
    }
}

[HarmonyPatch(typeof(BUS_SkillInstsCompSvr), "OnUnitCastSkillTry", typeof(FCastSkillInfo))]
[HarmonyPatchCategory(Constants.ConnectedPatches)]
public static class PatchOnUnitCastSkillTry
{
    public static void Postfix(FCastSkillInfo CSI, BUC_SkillInstsData ___SkillInstsData, BUS_SkillInstsCompSvr __instance)
    {
        if (!DI.Instance.AreaState.InRoom)
            return;

        var playerState = DI.Instance.PlayerState;
        var owner = __instance.GetOwner();

        if (___SkillInstsData.GetLastSkillCastResult() != 0)
        {
            Logging.LogDebug("GetLastSkillCastResult was not success");
            return;
        }

        if (CSI.SourceType == ECastSkillSourceType.PhantomRush)
        {
            if (owner == playerState.LocalMainCharacter?.GetLocalState().Pawn)
            {
                Logging.LogDebug("Sending phantom rush with direction: {Direction}", CSI.SkillDirection);
                DI.Instance.Rpc.SendPhantomRush(CSI.SkillDirection);
            }
        }
    }
}

[HarmonyPatch(typeof(BUS_PhantomRushComp), "ExitPhantomRush")]
[HarmonyPatchCategory(Constants.ConnectedPatches)]
public static class PatchExitPhantomRush
{
    public static void Prefix(BUS_PhantomRushComp __instance, IBUC_SimpleStateData ___SimpleStateData)
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

        var mainEntity = DI.Instance.PawnState.GetEntityByPlayerPawn(owner);

        if (mainEntity == null)
            return;

        ref var main = ref mainEntity.Value.GetState();
        ref var localMain = ref mainEntity.Value.GetLocalState();

        if ((DI.Instance.AreaState.IsMasterClient || owner == localMain.Pawn) && !localMain.ReceivedPhantomRushExit)
        {
            Logging.LogDebug("Broadcasting phantom rush exit for player {Nickname}", main.CharacterNickName);
            DI.Instance.Rpc.SendExitPhantomRush(main.PlayerId);
            localMain.ReceivedPhantomRushExit = false;
        }

        var playerId = main.PlayerId;
        var playerEntity = DI.Instance.PlayerState.GetPlayerById(playerId);

        if (mainEntity != playerState.LocalMainCharacter && playerEntity.HasValue)
        {
            DI.Instance.ModeManager.SetPlayerVisibility(playerEntity.Value, mainEntity.Value, true);
        }
    }
}

[HarmonyPatch(typeof(BUFFPlayerWinePartnerAttr), "Apply")]
[HarmonyPatchCategory(Constants.ConnectedPatches)]
public static class PatchBuffPlayerWinePartnerAttr
{
    public static bool Prefix(AActor Target, out float OutAbs, out float OutMul)
    {
        OutAbs = 0.0f;
        OutMul = 0.0f;

        if (!DI.Instance.AreaState.InRoom)
            return true;

        ABGUCharacter? abguCharacter = Target as ABGUCharacter;
        if (abguCharacter != null)
        {
            IBPC_PlayerRoleData readOnlyData = BGU_DataUtil.GetReadOnlyData<IBPC_PlayerRoleData, BPC_PlayerRoleData>(abguCharacter.GetController());
            if (readOnlyData is { RoleData: null })
                return false;
        }

        return true;
    }
}

[HarmonyPatch]
[HarmonyPatchCategory(Constants.ConnectedPatches)]
public static class TransformationPatch
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

        var mainEntity = DI.Instance.PawnState.GetEntityByPlayerPawn(oldOwner);

        if (mainEntity == null)
        {
            Logging.LogDebug("Skipping transformation of {OldOwner} because player state is null", oldOwner.GetName());
            return;
        }

        ref var main = ref mainEntity.Value.GetState();
        ref var localMain = ref mainEntity.Value.GetLocalState();

        localMain.Pawn = newOwner;
        // update equipment
        EquipmentUtils.SetActorEquipment(newOwner, main.Equipment);
        Logging.LogDebug("Transformed {OldOwner} to {NewOwner}", oldOwner?.GetName(), newOwner?.GetName());
    }
}

// TODO: This fixes follower transform (UI for skills no longer crashes) but also causes me and them to be unable to transform back
// Also, skill UI for myself when I transform does not appear
[HarmonyPatch(typeof(BPC_BattleMainInfoData), "GetCommonDisabledState")]
[HarmonyPatchCategory(Constants.ConnectedPatches)]
public class PatchLogs4
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

[HarmonyPatchCategory(Constants.ConnectedPatches)]
public class PatchOnTransBeginSpawnNewOne
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
        if (pawn == playerState.LocalMainCharacter?.GetLocalState().Pawn)
        {
            Logging.LogDebug("OnTransBeginSpawnNewOne: Sending transform for player {Name} to unit with id {UnitId}", playerState.LocalMainCharacter.Value.GetState().CharacterNickName, ToReplaceUnitResID);
            DI.Instance.Rpc.SendPlayerTransBegin(new PlayerTransBeginData(ToReplaceUnitResID, ToReplaceUnitBornSkillID, EnableBlendViewTarget, TransBeginType));
        }

        var entity = DI.Instance.PawnState.GetEntityByPlayerPawn(pawn);
        if (entity != null)
        {
            ref var mainComp = ref entity.Value.GetState();
            mainComp.IsTransformed = true;
        }
    }
}

[HarmonyPatchCategory(Constants.ConnectedPatches)]
public class PatchOnTransBackSpawnNewOne
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

        var playerState = DI.Instance.PlayerState;
        var pawn = __instance.GetOwner();
        if (pawn == playerState.LocalMainCharacter?.GetLocalState().Pawn)
        {
            Logging.LogDebug("OnTransBackSpawnNewOne: Sending transform for player {Name} to unit with id {UnitId}", playerState.LocalMainCharacter.Value.GetState().CharacterNickName, ToReplaceUnitResID);
            DI.Instance.Rpc.SendPlayerTransEnd(new PlayerTransEndData(ToReplaceUnitResID, ToReplaceUnitBornSkillID, EnableBlendViewTarget, TransEndType));
        }

        __state = DI.Instance.PawnState.GetEntityByPlayerPawn(pawn);
    }

    public static void Postfix(UActorCompBaseCS __instance, object? __state)
    {
        if (!DI.Instance.AreaState.InRoom)
            return;

        var state = (MainCharacterEntity?)__state;
        if (state.HasValue)
        {
            ref var mainComp = ref state.Value.GetState();
            mainComp.IsTransformed = false;
            var attrContainer = BGU_DataUtil.GetReadOnlyData<IBUC_AttrContainer, BUC_AttrContainer>(state.Value.GetLocalState().Pawn);
            mainComp.Hp = attrContainer.GetFloatValue(EBGUAttrFloat.HpMax);
        }
    }
}

[HarmonyPatchCategory(Constants.ConnectedPatches)]
public class PatchSpawnAndPossess
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
        var mainPlayerController = GameUtils.GetPlayerController();
        bool isNonLocalTransform = false;
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

        if (isNonLocalTransform && newPawn is BGUCharacterCS newCharacter)
        {
            BGW_EventCollection.Get(GameUtils.GetWorld())?.Evt_RemoveActorGuid2Entity(newCharacter, BGU_DataUtil.GetActorGuid(newCharacter), newCharacter.GetResID());
        }

        if (isNonLocalTransform && mainPlayerPawn != null)
        {
            // Set player controller back to main player
            GameUtils.PossesPawnWithViewTarget(mainPlayerController, mainPlayerPawn, newPawn, cameraRotation);
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
[HarmonyPatchCategory(Constants.ConnectedPatches)]
public class PatchUpdateTransGuideData
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
[HarmonyPatchCategory(Constants.ConnectedPatches)]
public class PatchOnPostTransBindData
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
[HarmonyPatchCategory(Constants.ConnectedPatches)]
public static class PatchOnIronBodyStart
{
    public static void Postfix(BUS_IronBodyComp __instance)
    {
        if (!DI.Instance.AreaState.InRoom)
            return;

        var playerState = DI.Instance.PlayerState;
        if (playerState.LocalMainCharacter?.GetLocalState().Pawn == __instance.GetOwner())
        {
            // Send iron body trigger to others
            DI.Instance.Rpc.SendIronBodyStart();
        }
    }
}

[HarmonyPatch(typeof(BPS_BattleMainInfoComp), "OnPossessed")]
[HarmonyPatchCategory(Constants.ConnectedPatches)]
public static class PatchBattleMainInfoCompOnPossessed
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
[HarmonyPatchCategory(Constants.ConnectedPatches)]
public static class PatchInputSystemOnPossessed
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
[HarmonyPatchCategory(Constants.ConnectedPatches)]
public static class PatchMultiTargetOnPossessed
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
[HarmonyPatchCategory(Constants.ConnectedPatches)]
public static class PatchDoCastMagicallyChangeSkill_PendingCast
{
    public static void Postfix(BUS_MagicallyChangeComp __instance, UBGWDataAsset? _Config, int _SkillID, int _RecoverSkillID)
    {
        if (!DI.Instance.AreaState.InRoom)
            return;

        if (_Config != null)
        {
            Logging.LogDebug("BUS_MagicallyChangeComp DoCastMagicallyChangeSkill_PendingCast called with Config Path: {Path}, SkillID: {SkillID}, RecoverSkillID: {RecoverSkillID}", _Config.PathName, _SkillID, _RecoverSkillID);
            var playerState = DI.Instance.PlayerState;
            if (DI.Instance.State.LocalPlayerId != null && playerState.LocalMainCharacter?.GetLocalState().Pawn == __instance.GetOwner())
            {
                DI.Instance.Rpc.SendTriggerMagicallyChange(DI.Instance.State.LocalPlayerId.Value, _Config, _SkillID, _RecoverSkillID);
            }
        }
    }
}

[HarmonyPatch(typeof(BUS_MagicallyChangeComp), "PendingReset")]
[HarmonyPatchCategory(Constants.ConnectedPatches)]
public static class PatchPendingReset
{
    public static void Postfix(BUS_MagicallyChangeComp __instance, EResetReason_MagicallyChange Reason)
    {
        if (!DI.Instance.AreaState.InRoom)
            return;

        Logging.LogDebug("BUS_MagicallyChangeComp PendingReset called with reason: {Reason}", Reason);
        var players = DI.Instance.PlayerState;
        if (players.LocalMainCharacter?.GetLocalState().Pawn == __instance.GetOwner())
        {
            DI.Instance.Rpc.SendResetMagicallyChange(Reason);
        }
    }
}

[HarmonyPatch]
[HarmonyPatchCategory(Constants.ConnectedPatches)]
public static class PatchOnSweepCheckHit
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
[HarmonyPatchCategory(Constants.GlobalPatches)]
public static class PatchSetCloudInputEnable
{
    public static bool Prefix(bool bEnable)
    {
        if (!DI.Instance.AreaState.InRoom)
            return true;

        var players = DI.Instance.PlayerState;
        var cloudMoveData = BGU_DataUtil.GetUnPersistentReadOnlyData<BUC_CloudMoveData>(players.LocalMainCharacter?.GetLocalState().Pawn);
        if (cloudMoveData == null)
        {
            return true;
        }

        return cloudMoveData.IsCloudMoveEnabled == bEnable;
    }
}