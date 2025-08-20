using System.Linq;
using b1;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using ReadyM.Relay.Common.Wukong.ECS.Components;
using UnrealEngine.Engine;
using WukongMp.Api.ECS.Components;
using WukongMp.Api.ECS.Entities;
using WukongMp.Api.WukongUtils;

namespace WukongMp.Api.ECS.Systems.Tamers;

public sealed class SyncTamersSystem : QuerySystem<TamerComponent, LocalTamerComponent>
{
    protected override void OnUpdate()
    {
        var allTamers = UGameplayStatics.GetAllActorsOfClass<BUTamerActor>(GameUtils.GetWorld())
            ?.Where(x => x != null)
            .GroupBy(x => BGU_DataUtil.GetActorGuid(x))
            .ToDictionary(g => g.Key, g => g.Last());

        if (allTamers is null)
        {
            Logging.LogError("Failed to find all tamers in the world.");
            return;
        }

        Query.ForEachEntity((ref TamerComponent tamerComp, ref LocalTamerComponent localTamerComp, Entity entity) =>
        {
            if (!localTamerComp.IsTamerSynced)
            {
                if (tamerComp.Guid is null)
                {
                    Logging.LogWarning("Entity {EntityId} has a TamerComponent with a null Guid. Cannot sync tamer.", entity.Id);
                    return;
                }

                if (allTamers.TryGetValue(tamerComp.Guid, out var actor))
                {
                    localTamerComp.Tamer = actor;
                    localTamerComp.IsTamerSynced = true;

                    ref var nameComp = ref entity.GetComponent<NicknameComponent>();
                    nameComp.Nickname = actor.GetClass().GetName();
#if TESTING
                    MarkerUtils.CreateMarkerForCharacter(new TamerEntity(entity));
#endif
                    Logging.LogDebug("Found matching tamer with guid: {Guid}", tamerComp.Guid);
                }

                // TODO: else spawn tamer?
                // SpawningUtils.SpawnUnitLocally(netId, tamer.Guid, tamer.UnitPath, team.TeamId, trans.Position.X, trans.Position.Y, trans.Position.Z);
            }
        });
    }
}