using b1;
using WukongMp.Api.ECS.Entities;

namespace WukongMp.Api.WukongUtils;

public static class TransformationUtils
{
    public static void TransformPlayer(in MainCharacterEntity mainEntity, int toReplaceUnitResID, int toReplaceUnitBornSkillID, bool enableBlendViewTarget, EPlayerTransBeginType transBeginType)
    {
        ref var mainComp = ref mainEntity.GetState();
        ref var localMainComp = ref mainEntity.GetLocalState();

        var events = BUS_EventCollectionCS.Get(localMainComp.Pawn);

        if (events == null)
        {
            Logging.LogError("Failed to get event collection for player {Nickname}", mainComp.CharacterNickName);
            return;
        }

        Logging.LogTrace("Transforming player {Nickname} to unitId {UnitId} with trans type {Type}", mainComp.CharacterNickName, toReplaceUnitResID, transBeginType);
        events.Evt_TransBeginSpawnNewOne.Invoke(toReplaceUnitResID, toReplaceUnitBornSkillID, enableBlendViewTarget, transBeginType);
    }

    public static void TransformPlayerBack(in MainCharacterEntity mainEntity, int toReplaceUnitResID, int toReplaceUnitBornSkillID, bool enableBlendViewTarget, EPlayerTransEndType transEndType)
    {
        ref var mainComp = ref mainEntity.GetState();
        ref var localMainComp = ref mainEntity.GetLocalState();
        var events = BUS_EventCollectionCS.Get(localMainComp.Pawn);

        if (events == null)
        {
            Logging.LogError("Failed to get event collection for player {Nickname}", mainComp.CharacterNickName);
            return;
        }

        Logging.LogTrace("Transforming player {Nickname} from unitId {UnitId} with trans type {Type}", mainComp.CharacterNickName, toReplaceUnitResID, transEndType);
        events.Evt_TransBackSpawnNewOne.Invoke(toReplaceUnitResID, toReplaceUnitBornSkillID, enableBlendViewTarget, transEndType);
    }
}