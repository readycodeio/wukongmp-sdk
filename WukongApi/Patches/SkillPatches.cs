using b1;
using BtlB1;
using BtlShare;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using UnrealEngine.Engine;
using UnrealEngine.Runtime;

namespace WukongApi.Patches
{
    //[HarmonyPatch(typeof(BUS_PlayerInputActionComp), "TriggerMagicSkill")]
    //[HarmonyPatchCategory(Constants.ConnectedPatches)]
    //public static class PatchTriggerMagicSkill
    //{
    //    public static bool Prefix()
    //    {
    //        return false;
    //    }
    //}

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
                if (castingPlayerState != null)
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

                if (photon.IsMasterClient)
                {
                    photon.BroadcastImmobilize(playerState.PhotonId, -1, ImmobilizeActionType.Relieve, false);
                    return true;
                }

                if (playerState != null && !playerState.RunImmobilizePatches)
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
    }
}
