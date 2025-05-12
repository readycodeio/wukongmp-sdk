using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using b1;
using b1.BGW;
using BtlB1;
using BtlShare;
using HarmonyLib;
using ReadyM.Relay.Common.ECS.Components;
using UnrealEngine.Engine;
using UnrealEngine.Runtime;
using WukongApi.ECS;

namespace WukongApi.Patches;

[HarmonyPatch(typeof(BUS_PlayerInputActionComp), "TriggerMagicSkill")]
[HarmonyPatchCategory(Constants.ConnectedPatches)]
public static class PatchTriggerMagicSkill
{
    public static bool Prefix(int SkillID)
    {
        if (!WukongMP.Instance.ShouldRunConnectedPatches())
            return true;

        var client = WukongMP.Instance.Client;
        return GameUtils.IsSkillWhitelisted(SkillID) && client.IsSkillEnabled(SkillID);
    }
}

[HarmonyPatch(typeof(BUS_PlayerInputActionComp), "TriggerVigorSkill")]
[HarmonyPatchCategory(Constants.ConnectedPatches)]
public static class PatchTriggerVigorSkill
{
    public static bool Prefix()
    {
        return false;
    }
}

[HarmonyPatch(typeof(BUS_PlayerInputActionComp), "TriggerItemSkill")]
[HarmonyPatchCategory(Constants.ConnectedPatches)]
public static class PatchTriggerItemSkill
{
    public static bool Prefix(BUS_PlayerInputActionComp __instance)
    {
        if (!WukongMP.Instance.ShouldRunConnectedPatches())
            return true;

        var client = WukongMP.Instance.Client;
        var lastSkill = Traverse.Create(__instance).Field("ComboCacheData").Property<int>("LastItemSkillID").Value;

        if (!client.RoomState.GourdAllowed && !client.RoomState.ConsumablesAllowed)
        {
            return false;
        }
        else if (client.RoomState.GourdAllowed && lastSkill == Constants.GourdSkillId)
        {
            return true;
        }

        return client.RoomState.ConsumablesAllowed && lastSkill == Constants.ConsumableBuffSkillId;
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
        if (!WukongMP.Instance.ShouldRunConnectedPatches())
            return true;

        var client = WukongMP.Instance.Client;
        return client.RoomState.GourdAllowed;
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
        return false;
    }
}

[HarmonyPatch(typeof(BUS_CastImmobilizeComp), "OnCastImmobilize")]
[HarmonyPatchCategory(Constants.ConnectedPatches)]
public static class PatchOnCastImmobilize
{
    public static bool Prefix(int ConfigID, BUS_CastImmobilizeComp __instance)
    {
        if (!WukongMP.Instance.ShouldRunConnectedPatches())
            return true;

        var client = WukongMP.Instance.Client;

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

        var castingPlayerState = client.GetPlayerByActor(castingCharacter);

        if (!client.IsMasterClient)
        {
            // Broadcast that you have cast a spell
            if (castingPlayerState != null && castingPlayerState.PeerId == client.LocalPlayerState.PeerId)
            {
                // target doesn't matter, not evaluated
                client.BroadcastImmobilize(NetworkIdComponent.FromPlayerPeerId(castingPlayerState.PeerId), default, ImmobilizeActionType.Cast, false);
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

        Debug.Assert(aBGUCharacter != null, "CurrentTarget As BGUCharacter is null");
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
                UBGWDataAsset? fXAssetByResID = GameUtils.GetFxAssetByResId(castingCharacter, cachedImmobilizeConfigDesc.FailedFXs, actorResID, CastImmobilizeData.ResId);
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
            ImmobilizeConfigInstance immobilizeConfigInstance = GameUtils.CreateImmobilizeConfig(item, castingCharacter, cachedImmobilizeConfigDesc, CastImmobilizeData.ResId, hasBuff);
            BUS_EventCollectionCS.Get(item)?.Evt_TriggerImmobilize.Invoke(immobilizeConfigInstance);

            // broadcast
            var immobilizedPlayer = client.GetPlayerByActor(item);
            var immobilizedMonster = client.GetMonsterByActor(item);

            if ((immobilizedPlayer != null || immobilizedMonster.HasValue) && castingPlayerState != null)
            {
                Logging.LogDebug("Broadcasting trigger immobilize");
                var netId = immobilizedPlayer == null
                    ? client.GetEntityComponent<NetworkIdComponent>(immobilizedMonster.Value)
                    : NetworkIdComponent.FromPlayerPeerId(immobilizedPlayer.PeerId);

                client.BroadcastImmobilize(netId, NetworkIdComponent.FromPlayerPeerId(castingPlayerState.PeerId), ImmobilizeActionType.Trigger, hasBuff);
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
        if (!WukongMP.Instance.ShouldRunConnectedPatches())
            return true;

        var client = WukongMP.Instance.Client;
        if (client.IsMasterClient)
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
        if (!WukongMP.Instance.ShouldRunConnectedPatches())
            return true;

        var client = WukongMP.Instance.Client;

        var owner = __instance.GetOwner();

        if (owner.IsNullOrDestroyed())
        {
            Logging.LogError("Owner is null or destroyed");
            return false;
        }

        var playerState = client.GetPlayerByActor(owner);
        var entityId = client.GetMonsterByActor(owner);

        if (playerState == null && !entityId.HasValue)
        {
            return true;
        }

        var netId = playerState != null ? NetworkIdComponent.FromPlayerPeerId(playerState.PeerId) : client.GetEntityComponent<NetworkIdComponent>(entityId!.Value);

        if (client.IsMasterClient)
        {
            client.BroadcastImmobilize(netId, default, ImmobilizeActionType.Relieve, false);
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

        ref var tamerComp = ref client.GetEntityComponent<TamerComponent>(entityId!.Value);

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
        if (!WukongMP.Instance.ShouldRunConnectedPatches())
            return true;

        var client = WukongMP.Instance.Client;
        var owner = __instance.GetOwner();

        if (owner.IsNullOrDestroyed())
        {
            Logging.LogError("Owner is null or destroyed");
            return false;
        }

        if (client.IsMasterClient)
        {
            var playerState = client.GetPlayerByActor(owner);

            if (playerState != null)
            {
                client.BroadcastImmobilize(NetworkIdComponent.FromPlayerPeerId(playerState.PeerId), default, ImmobilizeActionType.Relieve, false);
                BUS_EventCollectionCS.Get(playerState.Pawn)?.Evt_RelieveImmobilized.Invoke();
                return false;
            }

            var entityId = client.GetMonsterByActor(owner);

            if (entityId.HasValue)
            {
                var netId = client.GetEntityComponent<NetworkIdComponent>(entityId.Value);
                var pawn = client.GetEntityComponent<TamerComponent>(entityId.Value).Pawn;

                client.BroadcastImmobilize(netId, default, ImmobilizeActionType.Relieve, false);
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
        if (!WukongMP.Instance.ShouldRunConnectedPatches())
            return true;

        var client = WukongMP.Instance.Client;
        if (!client.RoomState.PhantomRushAllowed)
        {
            return false;
        }

        AActor owner = __instance.GetOwner();

        if (owner.IsNullOrDestroyed())
        {
            Logging.LogError("Owner is null or destroyed");
            return false;
        }

        if (owner == client.LocalPlayerState.Pawn)
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
        if (!WukongMP.Instance.ShouldRunConnectedPatches())
            return;

        // PhantomRush not triggered - skip
        if (!___SimpleStateData.HasSimpleState(EBGUSimpleState.PhantomRush))
        {
            return;
        }

        var client = WukongMP.Instance.Client;
        var owner = __instance.GetOwner();

        if (owner.IsNullOrDestroyed())
        {
            Logging.LogError("Owner is null or destroyed");
            return;
        }

        var playerState = client.GetPlayerByActor(owner);
        if (playerState != null && playerState != client.LocalPlayerState)
        {
            WukongMP.SetPlayerVisibility(playerState, false);
        }
    }
}

[HarmonyPatch(typeof(BUS_SkillInstsCompSvr), "OnUnitCastSkillTry", typeof(FCastSkillInfo))]
[HarmonyPatchCategory(Constants.ConnectedPatches)]
public static class PatchOnUnitCastSkillTry
{
    public static void Postfix(FCastSkillInfo CSI, BUC_SkillInstsData ___SkillInstsData, BUS_SkillInstsCompSvr __instance)
    {
        if (!WukongMP.Instance.ShouldRunConnectedPatches())
            return;

        var client = WukongMP.Instance.Client;
        var owner = __instance.GetOwner();

        if (___SkillInstsData.GetLastSkillCastResult() != 0)
        {
            Logging.LogDebug("GetLastSkillCastResult was not success");
            return;
        }

        if (CSI.SourceType == ECastSkillSourceType.PhantomRush)
        {
            if (owner == client.LocalPlayerState.Pawn)
            {
                Logging.LogDebug("Sending phantom rush with direction: {Direction}", CSI.SkillDirection);
                client.SendPhantomRush(CSI.SkillDirection);
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
        if (!WukongMP.Instance.ShouldRunConnectedPatches())
            return;

        var client = WukongMP.Instance.Client;
        var owner = __instance.GetOwner();

        if (owner.IsNullOrDestroyed())
        {
            Logging.LogError("Owner is null or destroyed");
            return;
        }

        var playerState = client.GetPlayerByActor(owner);

        if (playerState == null)
            return;

        if ((client.IsMasterClient || owner == client.LocalPlayerState.Pawn) && !playerState.ReceivedPhantomRushExit)
        {
            Logging.LogDebug("Broadcasting phantom rush exit for player {Nickname}", playerState.NickName);
            client.ExitPhantomRush(playerState.PeerId);
            playerState.ReceivedPhantomRushExit = false;
        }

        if (playerState != client.LocalPlayerState)
        {
            WukongMP.SetPlayerVisibility(playerState, true);
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

        if (!WukongMP.Instance.ShouldRunConnectedPatches())
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