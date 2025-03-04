using b1;
using b1.BGW;
using BtlB1;
using BtlShare;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using UnrealEngine.Engine;
using UnrealEngine.Runtime;

namespace WukongApi.Patches
{
    [HarmonyPatch(typeof(BUS_PlayerInputActionComp), "TriggerMagicSkill")]
    [HarmonyPatchCategory(Constants.ConnectedPatches)]
    public static class PatchTriggerMagicSkill
    {
        public static bool Prefix(int SkillID)
        {
            if (GameUtils.IsSkillWhitelisted(SkillID))
                return true;
            return false;
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

            // get properties
            MethodInfo getter = AccessTools.PropertyGetter(typeof(BUS_CastImmobilizeComp), "CastImmobilizeData");
            BUC_CastImmobilizeData CastImmobilizeData = (BUC_CastImmobilizeData)getter.Invoke(__instance, null);
            getter = AccessTools.PropertyGetter(typeof(BUS_CastImmobilizeComp), "TargetInfoData");
            IBUC_TargetInfoData TargetInfoData = (IBUC_TargetInfoData)getter.Invoke(__instance, null);
            getter = AccessTools.PropertyGetter(typeof(BUS_CastImmobilizeComp), "BuffData");
            IBUC_BuffData BuffData = (IBUC_BuffData)getter.Invoke(__instance, null);

            var photon = WukongMP.Instance.Photon;
            AActor castingCharacter = __instance.GetOwner();
            var castingPlayerState = photon.GetByActor(castingCharacter);

            if (!photon.IsMasterClient)
            {
                // Broadcast that you have cast a spell
                if (castingPlayerState != null && castingPlayerState.PhotonId == photon.LocalPlayerState.PhotonId)
                {
                    photon.BroadcastImmobilize(castingPlayerState.PhotonId, -1, ImmobilizeActionType.Cast, false);
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
            ABGUCharacter aBGUCharacter = null;
            aBGUCharacter = TargetInfoData.GetSkillBaseTarget().LockTargetActor as ABGUCharacter;
            if (aBGUCharacter == null)
            {
                aBGUCharacter = TargetInfoData.GetTargetInfo().LockTargetActor as ABGUCharacter;
            }
            if (BGW_LogUtil.LogIfNull(aBGUCharacter, "CurrentTarget As BGUCharacter is null") || !BGUFuncLibSelectTargetsCS.BGUIsSelectTargetByTeamFilter(castingCharacter, aBGUCharacter, cachedImmobilizeConfigDesc.TargetFilter) || !BGUFuncLibSelectTargetsCS.BGUIsSelectTargetByAffiliationFilter(castingCharacter, aBGUCharacter, cachedImmobilizeConfigDesc.AffiliationTypeFilter))
            {
                Logging.LogError("CurrentTarget As BGUCharacter is null in PatchOnCastImmobilize");
                return false;
            }
            int num = ((cachedImmobilizeConfigDesc.TargetCount <= 0) ? 1 : cachedImmobilizeConfigDesc.TargetCount);
            List<AActor> OutActors = new List<AActor>();
            if (num > 1)
            {
                List<int> list = new List<int> { cachedImmobilizeConfigDesc.RangeRadius };
                AActor owner2 = __instance.GetOwner();
                FVector baseLoc = BGUFuncLibActorTransformCS.BGUGetActorLocation(aBGUCharacter);
                int targetFilter = cachedImmobilizeConfigDesc.TargetFilter;
                int targetTypeFilter = cachedImmobilizeConfigDesc.TargetTypeFilter;
                int affiliationTypeFilter = cachedImmobilizeConfigDesc.AffiliationTypeFilter;
                IList<int> Prams = list;
                BGUFuncLibSelectTargetsCS.BGUSelectTargetsInShape(castingCharacter, out OutActors, owner2, baseLoc, ERangeType.Circle, -1, targetFilter, targetTypeFilter, affiliationTypeFilter, in Prams);
            }
            if (OutActors.Contains(aBGUCharacter))
            {
                OutActors.Remove(aBGUCharacter);
                OutActors.Insert(0, aBGUCharacter);
            }
            else
            {
                OutActors.Insert(0, aBGUCharacter);
            }
            int num2 = 0;
            foreach (AActor item in OutActors)
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
                    UBGWDataAsset fXAssetByResID = GameUtils.GetFXAssetByResID(castingCharacter, cachedImmobilizeConfigDesc.FailedFXs, actorResID, CastImmobilizeData.ResId);
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
                var immobilizedPlayerState = photon.GetByActor(item);
                if (immobilizedPlayerState != null && castingPlayerState != null)
                {
                    Logging.LogError($"Broadcasting trigger immobilize for player {immobilizedPlayerState.NickName}");
                    photon.BroadcastImmobilize(immobilizedPlayerState.PhotonId, castingPlayerState.PhotonId, ImmobilizeActionType.Trigger, hasBuff);
                }
            }

            return false;
        }

        [HarmonyPatch(typeof(BUS_BeImmobilizedComp), "OnTickWithGroup")]
        [HarmonyPatchCategory(Constants.ConnectedPatches)]
        public static class PatchImmobilizeOnTickWithGroup
        {
            public static bool Prefix()
            {
                if (!WukongMP.Instance.ShouldRunConnectedPatches())
                    return true;

                var photon = WukongMP.Instance.Photon;
                if (photon.IsMasterClient)
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

                var photon = WukongMP.Instance.Photon;

                var playerState = photon.GetByActor(__instance.GetOwner());

                if (playerState == null)
                {
                    return true;
                }

                if (photon.IsMasterClient)
                {
                    photon.BroadcastImmobilize(playerState.PhotonId, -1, ImmobilizeActionType.Relieve, false);
                    return true;
                }

                if (!playerState.RunImmobilizePatches)
                {
                    return false;
                }

                playerState.RunImmobilizePatches = false;
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

                var photon = WukongMP.Instance.Photon;
                var playerState = photon.GetByActor(__instance.GetOwner());

                if (photon.IsMasterClient)
                {
                    photon.BroadcastImmobilize(playerState.PhotonId, -1, ImmobilizeActionType.Relieve, false);
                    BUS_EventCollectionCS.Get(playerState.Pawn)?.Evt_RelieveImmobilized.Invoke();
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

                var photon = WukongMP.Instance.Photon;
                if (__instance.GetOwner() == photon.LocalPlayerState.Pawn)
                    return true;

                // Modified original impelmentation
                AActor owner = __instance.GetOwner();
                if (owner == null)
                {
                    Logging.LogError($"Owner is null");
                    return false;
                }
                MethodInfo GetActualUseConfigIDMethod = AccessTools.Method(typeof(BUS_PhantomRushComp), "GetActualUseConfigID");
                if (GetActualUseConfigIDMethod == null)
                {
                    Logging.LogError($"GetActualUseConfigID method info is null");
                    return false;
                }
                BUS_GSEventCollection BUSEventCollection = BUS_EventCollectionCS.Get(owner);
                BGS_GSEventCollection BGSEventCollection = BGS_GSEventCollection.Get(owner);
                ACharacter aCharacter = owner as ACharacter;
                if (aCharacter == null || ___SimpleStateData.HasSimpleState(EBGUSimpleState.PhantomRush))
                {
                    Logging.LogError($"aCharacter is null or PhantomRush is already active");
                    return false;
                }
                FUStPhantomRushSkillConfigDesc phantomRushSkillConfigDesc = BGW_GameDB.GetPhantomRushSkillConfigDesc((int)GetActualUseConfigIDMethod.Invoke(__instance, null), owner);
                if (phantomRushSkillConfigDesc == null)
                {
                    Logging.LogError($"phantomRushSkillConfigDesc is null");
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
                    Logging.LogError($"GetLastSkillCastResult was not success");
                    return false;
                }
                BUSEventCollection.Evt_ClearAbnormalState.Invoke(new HashSet<EAbnormalStateType>
                {
                    EAbnormalStateType.Abnormal_Burn,
                    EAbnormalStateType.Abnormal_Freeze,
                    EAbnormalStateType.Abnormal_Poison,
                    EAbnormalStateType.Abnormal_Thunder
                });
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

                var photon = WukongMP.Instance.Photon;
                if (__instance.GetOwner() == photon.LocalPlayerState.Pawn)
                {
                    Logging.LogError($"Sending phantom rush with direction: {PhantomRushDir}");
                    photon.SendPhantomRush(PhantomRushDir);
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

                var photon = WukongMP.Instance.Photon;
                var playerState = photon.GetByActor(__instance.GetOwner());

                if (playerState == null)
                    return;

                if ((photon.IsMasterClient || __instance.GetOwner() == photon.LocalPlayerState.Pawn) && !playerState.RecivedPhantomRushExit)
                {
                    Logging.LogError($"Broadcasting phantom rush exit for player {playerState.NickName}");
                    photon.ExitPhantomRush(playerState.PhotonId);
                    playerState.RecivedPhantomRushExit = false;
                }
            }
        }
    }
}
