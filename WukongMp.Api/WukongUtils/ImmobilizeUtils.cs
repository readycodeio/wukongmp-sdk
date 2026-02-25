using System.Collections.Generic;
using b1;
using b1.BGW;
using b1.ECS;
using BtlB1;
using UnrealEngine.Engine;
using UnrealEngine.Runtime;
using WukongMp.Api.State;

namespace WukongMp.Api.WukongUtils;

internal static class ImmobilizeUtils // TODO: API should accept Entity, not BGUCharacterCS; this will be an internal utils class
{
    internal static void CastImmobilize(BGUCharacterCS caster)
    {
        Logging.LogDebug("Received cast immobilize for character {Nickname}", caster.GetName());
        var playerEvents = BUS_EventCollectionCS.Get(caster);
        playerEvents.Evt_CastImmobilize.Invoke(0);
    }

    internal static void TriggerImmobilize(BGUCharacterCS? pawn, BGUCharacterCS? caster, bool hasBuff)
    {
        Logging.LogDebug("Received trigger immobilize for character {Pawn}", pawn?.GetName());

        if (pawn == null)
        {
            Logging.LogError("Could not find immobilized pawn");
            return;
        }

        if (caster == null)
        {
            Logging.LogError("Could not find caster pawn");
            return;
        }

        var castImmobilizeData = (BUC_CastImmobilizeData)caster.GetDataByChunk(TypeManager.GetTypeIndex<BUC_CastImmobilizeData>());
        var passiveSkillData = (BUC_PassiveSkillData)caster.GetDataByChunk(TypeManager.GetTypeIndex<BUC_PassiveSkillData>());

        passiveSkillData.TryGetCachedDesc<FUStImmobilizeSkillConfigDesc>(castImmobilizeData.ResId, out var cachedImmobilizeConfigDesc);
        if (cachedImmobilizeConfigDesc == null)
        {
            Logging.LogError("cachedImmobilizeConfigDesc is null");
            return;
        }

        var immobilizeConfigInstance = CreateImmobilizeConfig(pawn, caster, cachedImmobilizeConfigDesc, castImmobilizeData.ResId, hasBuff, castImmobilizeData);
        BUS_EventCollectionCS.Get(pawn)?.Evt_TriggerImmobilize.Invoke(immobilizeConfigInstance);
    }

    internal static void RelieveImmobilize(WukongPawnState pawnState, BGUCharacterCS pawn)
    {
        Logging.LogDebug("Received relieve immobilize for player {Nickname}", pawn.GetName());
        var playerEvents = BUS_EventCollectionCS.Get(pawn);

        // TODO: This should be taken care of by the game event origin system
        var entity = pawnState.GetEntityByTamerMonster(pawn);
        if (entity.HasValue)
        {
            ref var localTamer = ref entity.Value.GetLocalTamer();
            localTamer.RunImmobilizePatches = true;
        }
        else
        {
            var mainEntity = pawnState.GetEntityByPlayerActor(pawn);
            mainEntity?.GetLocalState().RunImmobilizePatches = true;
        }
        
        playerEvents?.Evt_RelieveImmobilized.Invoke();
    }

    public static ImmobilizeConfigInstance CreateImmobilizeConfig(AActor character, AActor casterActor, FUStImmobilizeSkillConfigDesc cachedImmobilizeConfigDesc, int castImmobilizeDataResId, bool hasBuff, BUC_CastImmobilizeData castImmobilizeData)
    {
        var immobilizeConfigInstance = new ImmobilizeConfigInstance();
        var actorResID3 = BGU_DataUtil.GetActorResID(character);
        immobilizeConfigInstance.DurationSecond = cachedImmobilizeConfigDesc.DurationMs * 0.001f;
        immobilizeConfigInstance.AlmostEndAheadTimeSecond = cachedImmobilizeConfigDesc.AlmostEndAheadTimeMs * 0.001f;
        immobilizeConfigInstance.MinDurationSecond = cachedImmobilizeConfigDesc.MinimalDurationMs * 0.001f;
        immobilizeConfigInstance.RepeatedImmobilizedDef = cachedImmobilizeConfigDesc.RepeatedImmobilizedDef * 0.0001f;
        immobilizeConfigInstance.CasterActor = casterActor;
        immobilizeConfigInstance.bEnableGreatSageTalent = cachedImmobilizeConfigDesc.GreatSageTalentActiveBuff > 0 && hasBuff;
        immobilizeConfigInstance.BeginFX = GetFxAssetByResId(character, cachedImmobilizeConfigDesc.BeginFXs, actorResID3, castImmobilizeDataResId, castImmobilizeData);
        immobilizeConfigInstance.AlmostEndFX = GetFxAssetByResId(character, cachedImmobilizeConfigDesc.AlmostEndFXs, actorResID3, castImmobilizeDataResId, castImmobilizeData);
        immobilizeConfigInstance.EndFX = GetFxAssetByResId(character, cachedImmobilizeConfigDesc.EndFXs, actorResID3, castImmobilizeDataResId, castImmobilizeData);
        immobilizeConfigInstance.QuickFX = GetFxAssetByResId(character, cachedImmobilizeConfigDesc.QuickEndFXs, actorResID3, castImmobilizeDataResId, castImmobilizeData);
        immobilizeConfigInstance.BreakingFXsTriggerRatio = cachedImmobilizeConfigDesc.BreakingFXsTriggerRatio * 0.0001f;
        immobilizeConfigInstance.BreakingFX = GetFxAssetByResId(character, cachedImmobilizeConfigDesc.BreakingFXs, actorResID3, castImmobilizeDataResId, castImmobilizeData);
        foreach (var beginEffect in cachedImmobilizeConfigDesc.BeginEffects)
        {
            immobilizeConfigInstance.BeginEffects.Add(new FSpellEffectForData(beginEffect));
        }

        foreach (var endEffect in cachedImmobilizeConfigDesc.EndEffects)
        {
            immobilizeConfigInstance.EndEffects.Add(new FSpellEffectForData(endEffect));
        }

        foreach (var breakEffect in cachedImmobilizeConfigDesc.BreakEffects)
        {
            immobilizeConfigInstance.BreakEffects.Add(new FSpellEffectForData(breakEffect));
        }

        foreach (var deadEffect in cachedImmobilizeConfigDesc.DeadEffects)
        {
            immobilizeConfigInstance.DeadEffects.Add(new FSpellEffectForData(deadEffect));
        }

        return immobilizeConfigInstance;
    }

    public static UBGWDataAsset? GetFxAssetByResId(UObject context, IList<FPlayFXByResID> fXs, int targetResId, int ownerResId, BUC_CastImmobilizeData CastImmobilizeData)
    {
        string text = "";
        foreach (var fx in fXs)
        {
            if (fx.ResID == targetResId)
            {
                text = fx.FXPathByDBC;
                break;
            }

            if (fx.ResID == ownerResId)
            {
                text = fx.FXPathByDBC;
            }
        }

        if (string.IsNullOrEmpty(text))
        {
            return null;
        }

        UBGWDataAsset uBGWDataAsset = CastImmobilizeData.TryGetDBCFromCache(text);
        if (uBGWDataAsset == null)
        {
            uBGWDataAsset = BGW_PreloadAssetMgr.Get(context).TryGetCachedResourceObj<UBGWDataAsset>(text, ELoadResourceType.SyncLoadAndCache);
            CastImmobilizeData.TryAddDBCCache(text, uBGWDataAsset);
        }

        return uBGWDataAsset;
    }
}