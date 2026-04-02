using b1;
using WukongMp.Api.ECS.Entities;

namespace WukongMp.Api.WukongUtils;

internal static class TransformationUtils
{
    public static void TransformPlayer(in MainCharacterEntity mainEntity, int toReplaceUnitResID, int toReplaceUnitBornSkillID, bool enableBlendViewTarget, EPlayerTransBeginType transBeginType)
    {
        ref var mainComp = ref mainEntity.GetState();

        var events = BUS_EventCollectionCS.Get(mainEntity.Pawn);

        if (events == null)
        {
            Logging.LogError("Failed to get event collection for player {Nickname}", mainComp.CharacterNickname);
            return;
        }

        Logging.LogDebug("Transforming player {Nickname} to unitId {UnitId} with trans type {Type}", mainComp.CharacterNickname, toReplaceUnitResID, transBeginType);
        events.Evt_TransBeginSpawnNewOne.Invoke(toReplaceUnitResID, toReplaceUnitBornSkillID, enableBlendViewTarget, transBeginType);
    }

    public static void TransformPlayerBack(in MainCharacterEntity mainEntity, int toReplaceUnitResID, int toReplaceUnitBornSkillID, bool enableBlendViewTarget, EPlayerTransEndType transEndType)
    {
        ref var mainComp = ref mainEntity.GetState();
        var events = BUS_EventCollectionCS.Get(mainEntity.Pawn);

        if (events == null)
        {
            Logging.LogError("Failed to get event collection for player {Nickname}", mainComp.CharacterNickname);
            return;
        }

        Logging.LogDebug("Transforming player {Nickname} from unitId {UnitId} with trans type {Type}", mainComp.CharacterNickname, toReplaceUnitResID, transEndType);
        events.Evt_TransBackSpawnNewOne.Invoke(toReplaceUnitResID, toReplaceUnitBornSkillID, enableBlendViewTarget, transEndType);
    }
}