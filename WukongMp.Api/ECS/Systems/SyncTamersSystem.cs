using System.Linq;
using b1;
using Friflo.Engine.ECS.Systems;
using ReadyM.Relay.Common.Wukong.Components;
using UnrealEngine.Engine;

namespace WukongMp.Api.ECS.Systems;

public sealed class SyncTamersSystem : QuerySystem<TamerComponent, LocalTamerComponent>
{
    protected override void OnUpdate()
    {
        var allTamers = UGameplayStatics.GetAllActorsOfClass<BUTamerActor>(GameUtils.GetWorld())
            ?.Where(x => x != null)
            .ToDictionary(x => BGU_DataUtil.GetActorGuid(x), x => x);

        if (allTamers is null)
        {
            Logging.LogWarning("Failed to find all tamers in the world.");
            return;
        }

        Query.ForEachEntity((ref tamer, ref localTamer, entity) =>
        {
            if (!localTamer.IsSynced)
            {
                if (allTamers.TryGetValue(tamer.Guid, out var actor))
                {
                    CommandBuffer.AddComponent(entity.Id, localTamer with { Tamer = actor, IsSynced = true });
                    Logging.LogDebug("Found matching tamer with guid: {Guid}", tamer.Guid);
                }
                else
                {
                    // spawn tamer
                    Logging.LogDebug("Matching tamer not found for guid: {Guid}", tamer.Guid);
                }
            }
        });
    }
}