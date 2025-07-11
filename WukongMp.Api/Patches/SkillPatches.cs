using System;
using System.Collections.Generic;
using System.Reflection;
using b1;
using b1.BGW;
using BtlB1;
using BtlShare;
using HarmonyLib;
using ReadyM.Relay.Common.ECS;
using UnrealEngine.Engine;
using UnrealEngine.Runtime;
using WukongMp.Api.Configuration;
using WukongMp.Api.DTO;
using WukongMp.Api.ECS;
using WukongMp.Api.Old;
using WukongMp.Api.Old.Api;
using WukongMp.Api.WukongUtils;

namespace WukongMp.Api.Patches;

[HarmonyPatch(typeof(BUS_PlayerInputActionComp), "TriggerMagicSkill")]
[HarmonyPatchCategory(Constants.ConnectedPatches)]
public static class PatchTriggerMagicSkill
{
    public static bool Prefix(int SkillID)
    {
        if (!DI.Instance.RelayClient.InRoom)
            return true;

        return SkillsUtils.IsSkillWhitelisted(SkillID) && (DI.Instance.PVP?.IsSkillEnabledInPVP(SkillID) ?? true);
    }
}

[HarmonyPatch(typeof(BUS_PlayerInputActionComp), "TriggerVigorSkill")]
[HarmonyPatchCategory(Constants.ConnectedPatches)]
public static class PatchTriggerVigorSkill
{
    public static bool Prefix()
    {
        return Constants.IsCoop;
    }
}

[HarmonyPatch(typeof(BUS_PlayerInputActionComp), "TriggerItemSkill")]
[HarmonyPatchCategory(Constants.ConnectedPatches)]
public static class PatchTriggerItemSkill
{
    public static bool Prefix(BUS_PlayerInputActionComp __instance)
    {
        if (!DI.Instance.RelayClient.InRoom)
            return true;

        var roomState = DI.Instance.RoomState;
        var lastSkill = Traverse.Create(__instance).Field("ComboCacheData").Property<int>("LastItemSkillID").Value;

        if (!roomState.GourdAllowed && !roomState.ConsumablesAllowed)
        {
            return false;
        }
        else if (roomState.GourdAllowed && lastSkill == Constants.GourdSkillId)
        {
            return true;
        }

        return roomState.ConsumablesAllowed && lastSkill == Constants.ConsumableBuffSkillId;
    }
}

[HarmonyPatch]
[HarmonyPatchCategory(Constants.ConnectedPatches)]
public static class PatchDoPoleDrink
{
    private static MethodBase TargetMethod()
    {
        return AccessTools.Method("b1.BUS_PoleDrinkComp:DoPoleDrink");
    }

    public static bool Prefix()
    {
        if (!DI.Instance.RelayClient.InRoom)
            return true;

        return DI.Instance.RoomState.GourdAllowed;
    }
}

[HarmonyPatch]
[HarmonyPatchCategory(Constants.ConnectedPatches)]
public static class PatchFaBaoSkill
{
    private static MethodBase TargetMethod()
    {
        return AccessTools.Method("b1.BUIACastFaBaoSkill:OnTriggerInputAction");
    }

    public static bool Prefix()
    {
        return Constants.IsCoop;
    }
}

[HarmonyPatch(typeof(BUS_CastImmobilizeComp), "OnCastImmobilize")]
[HarmonyPatchCategory(Constants.ConnectedPatches)]
public static class PatchOnCastImmobilize
{
    public static bool Prefix(int ConfigID, BUS_CastImmobilizeComp __instance)
    {
        if (!DI.Instance.RelayClient.InRoom)
            return true;

        var players = DI.Instance.Players;

        // get properties
        MethodInfo getter = AccessTools.PropertyGetter(typeof(BUS_CastImmobilizeComp), "CastImmobilizeData");
        BUC_CastImmobilizeData CastImmobilizeData = (BUC_CastImmobilizeData)getter.Invoke(__instance, null);
        getter = AccessTools.PropertyGetter(typeof(BUS_CastImmobilizeComp), "TargetInfoData");
        IBUC_TargetInfoData TargetInfoData = (IBUC_TargetInfoData)getter.Invoke(__instance, null);
        getter = AccessTools.PropertyGetter(typeof(BUS_CastImmobilizeComp), "BuffData");
        IBUC_BuffData BuffData = (IBUC_BuffData)getter.Invoke(__instance, null);

        AActor castingCharacter = __instance.GetOwner();

        if (castingCharacter.IsNullOrDestroyed())
        {
            Logging.LogError("Owner is null or destroyed");
            return false;
        }

        var castingPlayerState = players.GetPlayerByActor(castingCharacter);

        if (!DI.Instance.RelayClient.IsMasterClient)
        {
            // Broadcast that you have cast a spell
            if (castingPlayerState != null && castingPlayerState.PlayerId == players.LocalPlayerState.PlayerId)
            {
                // target doesn't matter, not evaluated
                DI.Instance.Rpc.SendCastImmobilize(NetworkIdComponent.FromPlayerId(castingPlayerState.PlayerId));
            }

            return false;
        }

        if (ConfigID == 0)
        {
            ConfigID = CastImmobilizeData.ResId;
        }

        FUStImmobilizeSkillConfigDesc cachedImmobilizeConfigDesc = CastImmobilizeData.GetCachedImmobilizeConfigDesc(ConfigID);
        if (cachedImmobilizeConfigDesc == null || BGW_LogUtil.LogIfNull(__instance.GetOwner() as ABGUCharacter, "CurCharacter is null"))
        {
            return false;
        }

        var aBGUCharacter = TargetInfoData.GetSkillBaseTarget().LockTargetActor as ABGUCharacter;
        if (aBGUCharacter == null)
        {
            aBGUCharacter = TargetInfoData.GetTargetInfo().LockTargetActor as ABGUCharacter;
        }

        if (BGW_LogUtil.LogIfNull(aBGUCharacter, "CurrentTarget As BGUCharacter is null") || !BGUFuncLibSelectTargetsCS.BGUIsSelectTargetByTeamFilter(castingCharacter, aBGUCharacter, cachedImmobilizeConfigDesc.TargetFilter) || !BGUFuncLibSelectTargetsCS.BGUIsSelectTargetByAffiliationFilter(castingCharacter, aBGUCharacter, cachedImmobilizeConfigDesc.AffiliationTypeFilter))
        {
            Logging.LogDebug("CurrentTarget As BGUCharacter is null in PatchOnCastImmobilize");
            return false;
        }

        DebugHelper.AssertNotNull(aBGUCharacter, "CurrentTarget As BGUCharacter is null");
        int num = cachedImmobilizeConfigDesc.TargetCount <= 0 ? 1 : cachedImmobilizeConfigDesc.TargetCount;
        List<AActor> outActors = [];
        if (num > 1)
        {
            List<int> list = [cachedImmobilizeConfigDesc.RangeRadius];
            AActor owner2 = __instance.GetOwner();

            if (owner2.IsNullOrDestroyed())
            {
                Logging.LogError("Owner is null or destroyed");
                return false;
            }

            FVector baseLoc = aBGUCharacter.BGUGetActorLocation();
            int targetFilter = cachedImmobilizeConfigDesc.TargetFilter;
            int targetTypeFilter = cachedImmobilizeConfigDesc.TargetTypeFilter;
            int affiliationTypeFilter = cachedImmobilizeConfigDesc.AffiliationTypeFilter;
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
                UBGWDataAsset? fXAssetByResID = AssetUtils.GetFxAssetByResId(castingCharacter, cachedImmobilizeConfigDesc.FailedFXs, actorResID, CastImmobilizeData.ResId);
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

            var hasBuff = BuffData.HasBuff(cachedImmobilizeConfigDesc.GreatSageTalentActiveBuff);
            ImmobilizeConfigInstance immobilizeConfigInstance = ImmobilizeUtils.CreateImmobilizeConfig(item, castingCharacter, cachedImmobilizeConfigDesc, CastImmobilizeData.ResId, hasBuff);
            BUS_EventCollectionCS.Get(item)?.Evt_TriggerImmobilize.Invoke(immobilizeConfigInstance);

            // broadcast
            var immobilizedPlayer = players.GetPlayerByActor(item);
            var immobilizedMonster = DI.Instance.PawnRegistry.GetMonsterByActor(item);

            if ((immobilizedPlayer != null || immobilizedMonster.HasValue) && castingPlayerState != null)
            {
                Logging.LogDebug("Broadcasting trigger immobilize");
                var netId = immobilizedPlayer == null
                    ? immobilizedMonster!.Value.GetComponent<NetworkIdComponent>()
                    : NetworkIdComponent.FromPlayerId(immobilizedPlayer.PlayerId);

                DI.Instance.Rpc.SendTriggerImmobilize(new TriggerImmobilizeData(netId, NetworkIdComponent.FromPlayerId(castingPlayerState.PlayerId), hasBuff));
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
        if (!DI.Instance.RelayClient.InRoom)
            return true;

        if (DI.Instance.RelayClient.IsMasterClient)
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
        if (!DI.Instance.RelayClient.InRoom)
            return true;

        var players = DI.Instance.Players;

        var owner = __instance.GetOwner();

        if (owner.IsNullOrDestroyed())
        {
            Logging.LogError("Owner is null or destroyed");
            return false;
        }

        var playerState = players.GetPlayerByActor(owner);
        var entity = DI.Instance.PawnRegistry.GetMonsterByActor(owner);

        if (playerState == null && !entity.HasValue)
        {
            return true;
        }

        var netId = playerState != null ? NetworkIdComponent.FromPlayerId(playerState.PlayerId) : entity!.Value.GetComponent<NetworkIdComponent>();

        if (DI.Instance.RelayClient.IsMasterClient)
        {
            DI.Instance.Rpc.SendRelieveImmobilize(netId);
            return true;
        }

        if (playerState != null)
        {
            if (!playerState.RunImmobilizePatches)
            {
                return false;
            }

            playerState.RunImmobilizePatches = false;
            return true;
        }

        ref var tamerComp = ref entity!.Value.GetComponent<LocalTamerComponent>();

        if (!tamerComp.RunImmobilizePatches)
        {
            return false;
        }

        tamerComp.RunImmobilizePatches = false;
        return true;
    }
}

[HarmonyPatch(typeof(BUS_BeImmobilizedComp), "OnTriggerImmobilizedBreak")]
[HarmonyPatchCategory(Constants.ConnectedPatches)]
public static class PatchOnTriggerImmobilizedBreak
{
    public static bool Prefix(BUS_BeImmobilizedComp __instance)
    {
        if (!DI.Instance.RelayClient.InRoom)
            return true;

        var players = DI.Instance.Players;
        var owner = __instance.GetOwner();

        if (owner.IsNullOrDestroyed())
        {
            Logging.LogError("Owner is null or destroyed");
            return false;
        }

        if (DI.Instance.RelayClient.IsMasterClient)
        {
            var playerState = players.GetPlayerByActor(owner);

            if (playerState != null)
            {
                DI.Instance.Rpc.SendRelieveImmobilize(NetworkIdComponent.FromPlayerId(playerState.PlayerId));
                BUS_EventCollectionCS.Get(playerState.Pawn)?.Evt_RelieveImmobilized.Invoke();
                return false;
            }

            var entity = DI.Instance.PawnRegistry.GetMonsterByActor(owner);

            if (entity.HasValue)
            {
                var netId = entity.Value.GetComponent<NetworkIdComponent>();
                var pawn = entity.Value.GetComponent<LocalTamerComponent>().Pawn;

                DI.Instance.Rpc.SendRelieveImmobilize(netId);
                BUS_EventCollectionCS.Get(pawn)?.Evt_RelieveImmobilized.Invoke();
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
        if (!DI.Instance.RelayClient.InRoom)
            return true;

        var players = DI.Instance.Players;

        if (!DI.Instance.RoomState.PhantomRushAllowed)
        {
            return false;
        }

        AActor owner = __instance.GetOwner();

        if (owner.IsNullOrDestroyed())
        {
            Logging.LogError("Owner is null or destroyed");
            return false;
        }

        if (owner == players.LocalPlayerState.Pawn)
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
        if (!DI.Instance.RelayClient.InRoom)
            return;

        // PhantomRush not triggered - skip
        if (!___SimpleStateData.HasSimpleState(EBGUSimpleState.PhantomRush))
        {
            return;
        }

        var players = DI.Instance.Players;
        var owner = __instance.GetOwner();

        if (owner.IsNullOrDestroyed())
        {
            Logging.LogError("Owner is null or destroyed");
            return;
        }

        var playerState = players.GetPlayerByActor(owner);
        if (playerState != null && playerState != players.LocalPlayerState)
        {
            DI.Instance.ModeManager.SetPlayerVisibility(playerState, false);
        }
    }
}

[HarmonyPatch(typeof(BUS_SkillInstsCompSvr), "OnUnitCastSkillTry", typeof(FCastSkillInfo))]
[HarmonyPatchCategory(Constants.ConnectedPatches)]
public static class PatchOnUnitCastSkillTry
{
    public static void Postfix(FCastSkillInfo CSI, BUC_SkillInstsData ___SkillInstsData, BUS_SkillInstsCompSvr __instance)
    {
        if (!DI.Instance.RelayClient.InRoom)
            return;

        var players = DI.Instance.Players;
        var owner = __instance.GetOwner();

        if (___SkillInstsData.GetLastSkillCastResult() != 0)
        {
            Logging.LogDebug("GetLastSkillCastResult was not success");
            return;
        }

        if (CSI.SourceType == ECastSkillSourceType.PhantomRush)
        {
            if (owner == players.LocalPlayerState.Pawn)
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
        if (!DI.Instance.RelayClient.InRoom)
            return;

        var players = DI.Instance.Players;
        var owner = __instance.GetOwner();

        if (owner.IsNullOrDestroyed())
        {
            Logging.LogError("Owner is null or destroyed");
            return;
        }

        var playerState = players.GetPlayerByActor(owner);

        if (playerState == null)
            return;

        if ((DI.Instance.RelayClient.IsMasterClient || owner == players.LocalPlayerState.Pawn) && !playerState.ReceivedPhantomRushExit)
        {
            Logging.LogDebug("Broadcasting phantom rush exit for player {Nickname}", playerState.NickName);
            DI.Instance.Rpc.SendExitPhantomRush(playerState.PlayerId);
            playerState.ReceivedPhantomRushExit = false;
        }

        if (playerState != players.LocalPlayerState)
        {
            DI.Instance.ModeManager.SetPlayerVisibility(playerState, true);
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

        if (!DI.Instance.RelayClient.InRoom)
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
    private static MethodBase TargetMethod()
    {
        return AccessTools.Method("b1.BUS_PlayerTransComp:TransferData");
    }

    public static void Postfix(UActorCompBaseCS __instance, ABGUCharacter ToReplaceUnitInst)
    {
        if (!DI.Instance.RelayClient.InRoom)
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

        var players = DI.Instance.Players;
        var playerState = players.GetPlayerByActor(oldOwner);

        if (playerState == null)
        {
            Logging.LogDebug("Skipping transformation of {OldOwner} because player state is null", oldOwner?.GetName());
            return;
        }

        playerState.Pawn = newOwner;
        // update equipment
        EquipmentHelpers.SetRemoteActorEquipment(newOwner, playerState.Equipment);
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
        if (!DI.Instance.RelayClient.InRoom)
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
    private static IEnumerable<MethodBase> TargetMethods()
    {
        yield return AccessTools.Method("b1.BUS_PlayerTransComp:OnTransBeginSpawnNewOne");
    }

    public static void Prefix(
        UActorCompBaseCS __instance,
        int ToReplaceUnitResID,
        int ToReplaceUnitBornSkillID,
        bool EnableBlendViewTarget,
        EPlayerTransBeginType TransBeginType)
    {
        if (!DI.Instance.RelayClient.InRoom)
            return;

        var players = DI.Instance.Players;
        if (__instance.GetOwner() == players.LocalPlayerState.Pawn)
        {
            Logging.LogDebug("OnTransBeginSpawnNewOne: Sending transform for player {Name} to unit with id {UnitId}", players.LocalPlayerState.NickName, ToReplaceUnitResID);
            DI.Instance.Rpc.SendPlayerTransBegin(new PlayerTransBeginData(ToReplaceUnitResID, ToReplaceUnitBornSkillID, EnableBlendViewTarget, TransBeginType));
        }
    }
}

[HarmonyPatchCategory(Constants.ConnectedPatches)]
public class PatchOnTransBackSpawnNewOne
{
    private static IEnumerable<MethodBase> TargetMethods()
    {
        yield return AccessTools.Method("b1.BUS_PlayerTransComp:OnTransBackSpawnNewOne");
    }

    public static void Prefix(
        UActorCompBaseCS __instance,
        int ToReplaceUnitResID,
        int ToReplaceUnitBornSkillID,
        bool EnableBlendViewTarget,
        EPlayerTransEndType TransEndType)
    {
        if (!DI.Instance.RelayClient.InRoom)
            return;

        var players = DI.Instance.Players;
        if (__instance.GetOwner() == players.LocalPlayerState.Pawn)
        {
            Logging.LogDebug("OnTransBackSpawnNewOne: Sending transform for player {Name} to unit with id {UnitId}", players.LocalPlayerState.NickName, ToReplaceUnitResID);
            DI.Instance.Rpc.SendPlayerTransEnd(new PlayerTransEndData(ToReplaceUnitResID, ToReplaceUnitBornSkillID, EnableBlendViewTarget, TransEndType));
        }
    }
}

[HarmonyPatchCategory(Constants.ConnectedPatches)]
public class PatchSpawnAndPossess
{
    private static IEnumerable<MethodBase> TargetMethods()
    {
        yield return AccessTools.Method("b1.BUS_PlayerTransComp:SpawnAndPossessTransUnit");
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
        if (!DI.Instance.RelayClient.InRoom)
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
            Logging.LogDebug("Controller is null, cannot transform");
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
            Logging.LogDebug("New pawn is null, cannot transform");
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
        if (!DI.Instance.RelayClient.InRoom)
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
        if (!DI.Instance.RelayClient.InRoom)
            return true;

        if (__instance.GetOwner() != GameUtils.GetControlledPawn())
        {
            return false;
        }

        return true;
    }
}

[HarmonyPatch(typeof(BUS_IronBodyComp), "OnIronBodyStart")]
[HarmonyPatchCategory(Constants.CoopPatches)]
public static class PatchOnIronBodyStart
{
    public static void Postfix(BUS_IronBodyComp __instance)
    {
        if (!DI.Instance.RelayClient.InRoom)
            return;

        var players = DI.Instance.Players;
        if (players.LocalPlayerState.Pawn == __instance.GetOwner())
        {
            // Send iron body trigger to others
            DI.Instance.Rpc.SendIronBodyStart();
        }
    }
}

[HarmonyPatch(typeof(BPS_BattleMainInfoComp), "OnPossessed")]
[HarmonyPatchCategory(Constants.CoopPatches)]
public static class PatchBattleMainInfoCompOnPossessed
{
    public static bool Prefix(AActor OldActor, AActor CurActor)
    {
        if (!DI.Instance.RelayClient.InRoom)
            return true;

        if (CurActor != null && CurActor == GameUtils.GetControlledPawn())
            return true;

        Logging.LogWarning("BPS_BattleMainInfoComp OnPossessed called, but the current actor is not the controlled pawn. OldActor: {OldActor}, CurActor: {CurActor}", OldActor?.GetName(), CurActor?.GetName());
        return false;
    }
}

[HarmonyPatch(typeof(BPS_InputSystem), "OnPossessed")]
[HarmonyPatchCategory(Constants.CoopPatches)]
public static class PatchInputSystemOnPossessed
{
    public static bool Prefix(AActor OldActor, AActor CurActor)
    {
        if (!DI.Instance.RelayClient.InRoom)
            return true;

        if (CurActor != null && CurActor == GameUtils.GetControlledPawn())
            return true;

        Logging.LogWarning("BPS_InputSystem OnPossessed called, but the current actor is not the controlled pawn. OldActor: {OldActor}, CurActor: {CurActor}", OldActor?.GetName(), CurActor?.GetName());
        return false;
    }
}

[HarmonyPatch(typeof(BPS_MultiTargetProjectileCtrComp), "OnPossessed")]
[HarmonyPatchCategory(Constants.CoopPatches)]
public static class PatchMultiTargetOnPossessed
{
    public static bool Prefix(AActor OldActor, AActor CurActor)
    {
        if (!DI.Instance.RelayClient.InRoom)
            return true;

        if (CurActor != null && CurActor == GameUtils.GetControlledPawn())
            return true;

        Logging.LogWarning("BPS_MultiTargetProjectileCtrComp OnPossessed called, but the current actor is not the controlled pawn. OldActor: {OldActor}, CurActor: {CurActor}", OldActor?.GetName(), CurActor?.GetName());
        return false;
    }
}

[HarmonyPatch(typeof(BUS_MagicallyChangeComp), "DoCastMagicallyChangeSkill_PendingCast")]
[HarmonyPatchCategory(Constants.CoopPatches)]
public static class PatchDoCastMagicallyChangeSkill_PendingCast
{
    public static void Postfix(BUS_MagicallyChangeComp __instance, UBGWDataAsset _Config, int _SkillID, int _RecoverSkillID)
    {
        if (!DI.Instance.RelayClient.InRoom)
            return;

        if (_Config != null)
        {
            Logging.LogDebug("BUS_MagicallyChangeComp DoCastMagicallyChangeSkill_PendingCast called with Config Path: {Path}, SkillID: {SkillID}, RecoverSkillID: {RecoverSkillID}", _Config.PathName, _SkillID, _RecoverSkillID);
            var players = DI.Instance.Players;
            if (players.LocalPlayerState.Pawn == __instance.GetOwner())
            {
                DI.Instance.Rpc.SendTriggerMagicallyChange(players.LocalPlayerState.PlayerId, _Config, _SkillID, _RecoverSkillID);
            }
        }
    }
}

[HarmonyPatch(typeof(BUS_MagicallyChangeComp), "PendingReset")]
[HarmonyPatchCategory(Constants.CoopPatches)]
public static class PatchPendingReset
{
    public static void Postfix(BUS_MagicallyChangeComp __instance, EResetReason_MagicallyChange Reason)
    {
        if (!DI.Instance.RelayClient.InRoom)
            return;

        Logging.LogDebug("BUS_MagicallyChangeComp PendingReset called with reason: {Reason}", Reason);
        var players = DI.Instance.Players;
        if (players.LocalPlayerState.Pawn == __instance.GetOwner())
        {
            DI.Instance.Rpc.SendResetMagicallyChange(Reason);
        }
    }
}
