using b1;
using BtlShare;
using WukongMp.Api.Old;
using WukongMp.Api.Patches;

namespace WukongMp.Api.WukongUtils;

public static class BuffUtils
{
    public static void AddBuff(BGUCharacterCS? character, int buffId, float duration)
    {
        GameLoopPatch.QueueOnGameThread(() =>
        {
            var events = BUS_EventCollectionCS.Get(character);

            if (events == null)
            {
                Logging.LogError("Failed to get event collection for character {Character}", character?.GetName());
                return;
            }

            events.Evt_BuffAdd.Invoke(buffId, character, character, duration);
        }, nameof(AddBuff));
    }

    public static void RemoveBuff(BGUCharacterCS? character, int buffId, EBuffEffectTriggerType removeTriggerType, int inLayer, bool withTriggerRemoveEffect)
    {
        GameLoopPatch.QueueOnGameThread(() =>
        {
            var events = BUS_EventCollectionCS.Get(character);

            if (events == null)
            {
                Logging.LogError("Failed to get event collection for character {Character}", character?.GetName());
                return;
            }

            events.Evt_BuffRemove.Invoke(buffId, removeTriggerType, inLayer, withTriggerRemoveEffect);
        }, nameof(RemoveBuff));
    }

    public static void RemoveAllBuffs(BGUCharacterCS? character, EBuffEffectTriggerType removeTriggerType, bool withTriggerRemoveEffect)
    {
        GameLoopPatch.QueueOnGameThread(() =>
        {
            var events = BUS_EventCollectionCS.Get(character);

            if (events == null)
            {
                Logging.LogError("Failed to get event collection for character {Character}", character?.GetName());
                return;
            }

            events.Evt_BuffAllRemove.Invoke(removeTriggerType, withTriggerRemoveEffect);
        }, nameof(RemoveAllBuffs));
    }
}