using WukongMp.Api.ECS.Entities;

namespace WukongMp.Api.WukongUtils;

public static class TeleportUtils
{
    public static void CheckForTeleportFinish(MainCharacterEntity mainEntity)
    {
        ref var localMainComp = ref mainEntity.GetLocalState();
        
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