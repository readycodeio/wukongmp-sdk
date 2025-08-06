using WukongMp.Api.ECS.Components;

namespace WukongMp.Api.WukongUtils;

public static class TeleportUtils
{
    public static void UpdatePlayerPosition(MainCharacterEntity mainEntity, float deltaTime)
    {
        ref var localMainComp = ref mainEntity.GetLocalState();
        
        localMainComp.UpdateMarkerPosition();

        if (localMainComp.TeleportFinishFrames >= 0)
        {
            if (localMainComp.TeleportFinishFrames == 0)
            {
                DI.Instance.Rpc.SendTeleportFinish();
            }

            localMainComp.TeleportFinishFrames--;
        }
    }
}