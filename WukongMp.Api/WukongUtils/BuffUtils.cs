using b1;
using BtlShare;

namespace WukongMp.Api.WukongUtils;

internal static class BuffUtils
{
    public static void AddBuff(BGUCharacterCS? character, int buffId, float duration)
    {
        var events = BUS_EventCollectionCS.Get(character);

        if (events == null)
        {
            Logging.LogDebug("Failed to get event collection for character {Character}", character?.GetName());
            return;
        }

        events.Evt_BuffAdd.Invoke(buffId, character, character, duration);
    }

    public static void RemoveBuff(BGUCharacterCS? character, int buffId, EBuffEffectTriggerType removeTriggerType, int inLayer, bool withTriggerRemoveEffect)
    {
        var events = BUS_EventCollectionCS.Get(character);

        if (events == null)
        {
            Logging.LogDebug("Failed to get event collection for character {Character}", character?.GetName());
            return;
        }

        events.Evt_BuffRemove.Invoke(buffId, removeTriggerType, inLayer, withTriggerRemoveEffect);
    }

    public static void RemoveAllBuffs(BGUCharacterCS? character, EBuffEffectTriggerType removeTriggerType, bool withTriggerRemoveEffect)
    {
        var events = BUS_EventCollectionCS.Get(character);

        if (events == null)
        {
            Logging.LogDebug("Failed to get event collection for character {Character}", character?.GetName());
            return;
        }

        events.Evt_BuffAllRemove.Invoke(removeTriggerType, withTriggerRemoveEffect);
    }
}