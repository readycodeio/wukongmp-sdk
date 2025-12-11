using b1;
using UnrealEngine.Engine;

namespace WukongMp.Api.WukongUtils;

public static class MagicFieldUtils
{
    public static void DestroyMagicField(string magicFieldClassName, EBGUBulletDestroyReason reason)
    {
        Logging.LogDebug("DestroyMagicField called for magic field {MagicFieldClassName} with reason {Reason}", magicFieldClassName, reason);
        var magicFields = UGameplayStatics.GetAllActorsOfClass<BGUMagicFieldBaseCS>(GameUtils.GetWorld());
        foreach (var magicField in magicFields)
        {
            if (magicField.IsNullOrDestroyed())
                continue;
            var magicFieldClass = magicField.GetClass();
            if (magicFieldClass != null && magicFieldClass.GetName() == magicFieldClassName)
            {
                var events = BUS_EventCollectionCS.Get(magicField);
                events?.Evt_OnProjectileDead.Invoke(reason);
            }
        }
    }
}
