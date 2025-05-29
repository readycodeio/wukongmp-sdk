using b1;
using b1.ECS;
using WukongMp.Api.ECS;
using WukongMp.Api.Old;
using WukongMp.Api.Old.Api;

namespace WukongMp.Api;

public static class ImmobilizeApi
{
    public static void CastImmobilize(BGUCharacterCS castingCharacterState)
    {
        if (WukongMP.Instance.Client.IsMasterClient)
        {
            Logging.LogDebug("Received cast immobilize for character {Nickname}", castingCharacterState.GetName());
            var playerEvents = BUS_EventCollectionCS.Get(castingCharacterState);
            playerEvents.Evt_CastImmobilize.Invoke(0);
        }
    }

    public static void TriggerImmobilize(BGUCharacterCS? pawn, BGUCharacterCS? caster, bool hasBuff)
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

        var immobilizeConfigInstance = GameUtils.CreateImmobilizeConfig(pawn, caster, cachedImmobilizeConfigDesc, castImmobilizeData.ResId, hasBuff);
        BUS_EventCollectionCS.Get(pawn)?.Evt_TriggerImmobilize.Invoke(immobilizeConfigInstance);
    }

    public static void RelieveImmobilize(BGUCharacterCS pawn)
    {
        Logging.LogDebug("Received relieve immobilize for player {Nickname}", pawn.GetName());
        var playerEvents = BUS_EventCollectionCS.Get(pawn);

        var entity = WukongMpMod.Instance.GetMonsterByActor(pawn);
        if (entity.HasValue)
        {
            ref var tamerComponent = ref entity.Value.GetComponent<LocalTamerComponent>();
            tamerComponent.RunImmobilizePatches = true;
        }
        else
        {
            var player = WukongMP.Instance.Client.GetPlayerByActor(pawn);
            if (player != null)
            {
                player.RunImmobilizePatches = true;
            }
        }

        playerEvents?.Evt_RelieveImmobilized.Invoke();
    }
}