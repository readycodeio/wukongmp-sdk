using b1;

namespace WukongMp.Api.WukongUtils;

public static class NpcLocomotionUtils
{
    public static void SetStateTrigger(BGUCharacterCS? character, EBUStateTrigger trigger, float time, bool needForceUpdate)
    {
        var events = BUS_EventCollectionCS.Get(character);

        if (events == null)
        {
            Logging.LogDebug("Failed to get event collection for pawn {PathName}", character?.PathName);
            return;
        }

        events.Evt_UnitStateTrigger.Invoke(trigger, time, needForceUpdate);
    }

    public static void SetSimpleState(BGUCharacterCS? character, EBGUSimpleState state, bool isForce)
    {
        var events = BUS_EventCollectionCS.Get(character);

        if (events == null)
        {
            Logging.LogDebug("Failed to get event collection for pawn {PathName}", character?.PathName);
            return;
        }

        events.Evt_UnitSetSimpleState.Invoke(state, isForce);
    }

    public static void SetFsmState(BGUCharacterCS? character, string stateName)
    {
        var events = BUS_EventCollectionCS.Get(character);

        if (events == null)
        {
            Logging.LogDebug("Failed to get event collection for character {Pawn}", character?.PathName);
            return;
        }

        events.Evt_TriggerFsmEvent.Invoke(stateName.MakeGameplayTag());
    }

    public static void SetMotionMatchingState(BGUCharacterCS? character, EState_MM motionMatchingState)
    {
        var events = BUS_EventCollectionCS.Get(character);

        if (events == null)
        {
            Logging.LogDebug("Failed to get event collection for pawn {PathName}", character?.PathName);
            return;
        }

        events.Evt_ChangeMotionMatchingState.Invoke(motionMatchingState);
    }
}