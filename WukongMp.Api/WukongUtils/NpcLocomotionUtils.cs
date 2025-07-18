using b1;
using WukongMp.Api.Old;
using WukongMp.Api.Patches;

namespace WukongMp.Api.WukongUtils;

public static class NpcLocomotionUtils
{
    public static void SetStateTrigger(BGUCharacterCS? character, EBUStateTrigger trigger, float time, bool needForceUpdate)
    {
        GameLoopPatch.QueueOnGameThread(() =>
        {
            var events = BUS_EventCollectionCS.Get(character);

            if (events == null)
            {
                Logging.LogError("Failed to get event collection for pawn {PathName}", character?.PathName);
                return;
            }

            events.Evt_UnitStateTrigger.Invoke(trigger, time, needForceUpdate);
        }, nameof(SetStateTrigger));
    }

    public static void SetSimpleState(BGUCharacterCS? character, EBGUSimpleState state, bool isForce)
    {
        GameLoopPatch.QueueOnGameThread(() =>
        {
            var events = BUS_EventCollectionCS.Get(character);

            if (events == null)
            {
                Logging.LogError("Failed to get event collection for pawn {PathName}", character?.PathName);
                return;
            }

            events.Evt_UnitSetSimpleState.Invoke(state, isForce);
        }, nameof(SetSimpleState));
    }

    public static void SetFsmState(BGUCharacterCS? character, string stateName)
    {
        GameLoopPatch.QueueOnGameThread(() =>
        {
            var events = BUS_EventCollectionCS.Get(character);

            if (events == null)
            {
                Logging.LogError("Failed to get event collection for character {Pawn}", character?.PathName);
                return;
            }

            events.Evt_TriggerFsmEvent.Invoke(stateName.MakeGameplayTag());
        }, nameof(SetFsmState), BGW_TickGroupMask.TG_BeforeStartPhsic);
    }

    public static void SetMotionMatchingState(BGUCharacterCS? character, EState_MM motionMatchingState)
    {
        GameLoopPatch.QueueOnGameThread(() =>
        {
            var events = BUS_EventCollectionCS.Get(character);

            if (events == null)
            {
                Logging.LogError("Failed to get event collection for pawn {PathName}", character?.PathName);
                return;
            }

            events.Evt_ChangeMotionMatchingState.Invoke(motionMatchingState);
        }, nameof(SetMotionMatchingState));
    }
}