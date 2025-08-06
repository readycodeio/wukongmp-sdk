using b1;
using b1.ECS;
using BtlB1;
using UnrealEngine.Engine;

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
            Logging.LogError("Failed to cast immobilizedCharacter to BGUCharacterCS");
            return;
        }

        if (caster == null)
        {
            Logging.LogError("Failed to cast castingCharacter to BGUCharacterCS");
            return;
        }

        var castImmobilizeData = (BUC_CastImmobilizeData)caster.GetDataByChunk(TypeManager.GetTypeIndex<BUC_CastImmobilizeData>());

        var cachedImmobilizeConfigDesc = castImmobilizeData.GetCachedImmobilizeConfigDesc(castImmobilizeData.ResId);
        if (cachedImmobilizeConfigDesc == null)
        {
            Logging.LogError("cachedImmobilizeConfigDesc is null");
            return;
        }

        var immobilizeConfigInstance = ImmobilizeUtils.CreateImmobilizeConfig(pawn, caster, cachedImmobilizeConfigDesc, castImmobilizeData.ResId, hasBuff);
        BUS_EventCollectionCS.Get(pawn)?.Evt_TriggerImmobilize.Invoke(immobilizeConfigInstance);
    }

    internal static void RelieveImmobilize(BGUCharacterCS pawn)
    {
        Logging.LogDebug("Received relieve immobilize for player {Nickname}", pawn.GetName());
        var playerEvents = BUS_EventCollectionCS.Get(pawn);

        var entity = DI.Instance.PawnState.GetEntityByTamerMonster(pawn);
        if (entity.HasValue)
        {
            ref var localTamer = ref entity.Value.GetLocalTamer();
            localTamer.RunImmobilizePatches = true;
        }
        else
        {
            var mainEntity = DI.Instance.PawnState.GetByEntityByPlayerPawn(pawn);
            if (mainEntity != null)
            {
                mainEntity.Value.GetLocalState().RunImmobilizePatches = true;
            }
        }

        playerEvents?.Evt_RelieveImmobilized.Invoke();
    }

    public static ImmobilizeConfigInstance CreateImmobilizeConfig(AActor character, AActor casterActor, FUStImmobilizeSkillConfigDesc cachedImmobilizeConfigDesc, int castImmobilizeDataResId, bool hasBuff)
    {
        var immobilizeConfigInstance = new ImmobilizeConfigInstance();
        var actorResID3 = BGU_DataUtil.GetActorResID(character);
        immobilizeConfigInstance.DurationSecond = cachedImmobilizeConfigDesc.DurationMs * 0.001f;
        immobilizeConfigInstance.AlmostEndAheadTimeSecond = cachedImmobilizeConfigDesc.AlmostEndAheadTimeMs * 0.001f;
        immobilizeConfigInstance.MinDurationSecond = cachedImmobilizeConfigDesc.MinimalDurationMs * 0.001f;
        immobilizeConfigInstance.RepeatedImmobilizedDef = cachedImmobilizeConfigDesc.RepeatedImmobilizedDef * 0.0001f;
        immobilizeConfigInstance.CasterActor = casterActor;
        immobilizeConfigInstance.bEnableGreatSageTalent = cachedImmobilizeConfigDesc.GreatSageTalentActiveBuff > 0 && hasBuff;
        immobilizeConfigInstance.BeginFX = AssetUtils.GetFxAssetByResId(character, cachedImmobilizeConfigDesc.BeginFXs, actorResID3, castImmobilizeDataResId);
        immobilizeConfigInstance.AlmostEndFX = AssetUtils.GetFxAssetByResId(character, cachedImmobilizeConfigDesc.AlmostEndFXs, actorResID3, castImmobilizeDataResId);
        immobilizeConfigInstance.EndFX = AssetUtils.GetFxAssetByResId(character, cachedImmobilizeConfigDesc.EndFXs, actorResID3, castImmobilizeDataResId);
        immobilizeConfigInstance.QuickFX = AssetUtils.GetFxAssetByResId(character, cachedImmobilizeConfigDesc.QuickEndFXs, actorResID3, castImmobilizeDataResId);
        immobilizeConfigInstance.BreakingFXsTriggerRatio = cachedImmobilizeConfigDesc.BreakingFXsTriggerRatio * 0.0001f;
        immobilizeConfigInstance.BreakingFX = AssetUtils.GetFxAssetByResId(character, cachedImmobilizeConfigDesc.BreakingFXs, actorResID3, castImmobilizeDataResId);
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
}