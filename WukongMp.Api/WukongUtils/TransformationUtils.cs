using b1;
using ReadyM.Relay.Common;
using WukongMp.Api.Old;
using WukongMp.Api.Patches;

namespace WukongMp.Api.WukongUtils;

public static class TransformationUtils
{
    public static void TransformPlayer(UserId peerId, int toReplaceUnitResID, int toReplaceUnitBornSkillID, bool enableBlendViewTarget, EPlayerTransBeginType transBeginType)
    {
        GameLoopPatch.QueueOnGameThread(() =>
        {
            var playerState = WukongMpModBase.Client.GetPlayerById(peerId);
            if (playerState == null)
            {
                Logging.LogError("Player not found: {Id}", peerId);
                return;
            }

            var events = BUS_EventCollectionCS.Get(playerState.Pawn);

            if (events == null)
            {
                Logging.LogError("Failed to get event collection for player {Nickname}", playerState.NickName);
                return;
            }

            Logging.LogTrace("Transforming player {Nickname} to unitId {UnitId} with trans type {Type}", playerState.NickName, toReplaceUnitResID, transBeginType);
            events.Evt_TransBeginSpawnNewOne.Invoke(toReplaceUnitResID, toReplaceUnitBornSkillID, enableBlendViewTarget, transBeginType);
        }, nameof(TransformPlayer));
    }

    public static void TransformPlayerBack(UserId peerId, int toReplaceUnitResID, int toReplaceUnitBornSkillID, bool enableBlendViewTarget, EPlayerTransEndType transEndType)
    {
        GameLoopPatch.QueueOnGameThread(() =>
        {
            var playerState = WukongMpModBase.Client.GetPlayerById(peerId);
            if (playerState == null)
            {
                Logging.LogError("Player not found: {Id}", peerId);
                return;
            }

            var events = BUS_EventCollectionCS.Get(playerState.Pawn);

            if (events == null)
            {
                Logging.LogError("Failed to get event collection for player {Nickname}", playerState.NickName);
                return;
            }

            Logging.LogTrace("Transforming player {Nickname} from unitId {UnitId} with trans type {Type}", playerState.NickName, toReplaceUnitResID, transEndType);
            events.Evt_TransBackSpawnNewOne.Invoke(toReplaceUnitResID, toReplaceUnitBornSkillID, enableBlendViewTarget, transEndType);
        }, nameof(TransformPlayerBack));
    }
}