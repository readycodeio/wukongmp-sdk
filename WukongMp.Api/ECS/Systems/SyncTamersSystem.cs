using b1;
using ReadyM.Relay.Common.ECS;
using ReadyM.Relay.Common.Wukong.Components;
using UnrealEngine.Engine;

namespace WukongMp.Api.ECS.Systems;

public sealed class SyncTamersSystem : SystemBase
{
    public override void OnUpdate()
    {
        Entities.ForEach((EntityId _,
            ref TamerComponent tamer,
            ref LocalTamerComponent localTamer) =>
        {
            if (!localTamer.IsSynced)
            {
                bool found = false;
                var allTamers = UGameplayStatics.GetAllActorsOfClass<BUTamerActor>(GameUtils.GetWorld());
                foreach (var actor in allTamers)
                {
                    if (actor != null && BGU_DataUtil.GetActorGuid(actor) == tamer.Guid)
                    {
                        found = true;
                        localTamer.Tamer = actor;
                        localTamer.IsSynced = true;
                        Logging.LogDebug("Found matching tamer with guid: {Guid}", tamer.Guid);
                    }
                }

                if (!found)
                {
                    // spawn tamer
                    Logging.LogDebug("Matching tamer not found for guid: {Guid}", tamer.Guid);
                }
            }
        });
    }
}