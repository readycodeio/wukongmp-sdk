using ReadyM.Api.Mapping.Events;
using WukongMp.Api.ECS.Entities;
using WukongMp.Api.ECS.GameEvents;

namespace WukongMp.Api.WukongUtils;

public static class TeleportUtils
{
    public static void CheckForTeleportFinish(IMappedEventManager mappedEvent, MainCharacterEntity mainEntity)
    {
        ref var localMainComp = ref mainEntity.GetLocalState();
        
        if (localMainComp.TeleportFinishFrames >= 0)
        {
            if (localMainComp.TeleportFinishFrames == 0)
            {
                mappedEvent.InvokeInGameAndNotifyEcs(new TeleportFinishEvent(mainEntity.Entity));
            }

            localMainComp.TeleportFinishFrames--;
        }
    }
}