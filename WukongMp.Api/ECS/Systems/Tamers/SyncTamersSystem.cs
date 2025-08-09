using System.Linq;
using b1;
using Friflo.Engine.ECS.Systems;
using ReadyM.Relay.Common.Wukong.ECS.Components;
using UnrealEngine.Engine;
using WukongMp.Api.ECS.Components;
using WukongMp.Api.WukongUtils;

namespace WukongMp.Api.ECS.Systems;

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
            Logging.LogWarning("Failed to find all tamers in the world.");
            return;
        }

        Query.ForEachEntity((ref tamer, ref localTamer, entity) =>
        {
            if (!localTamer.IsTamerSynced)
            {
                if (tamer.Guid is null)
                {
                    Logging.LogWarning("Entity {EntityId} has a TamerComponent with a null Guid. Cannot sync tamer.", entity.Id);
                    return;
                }

                if (allTamers.TryGetValue(tamer.Guid, out var actor))
                {
                    localTamer.Tamer = actor;
                    localTamer.IsTamerSynced = true;

                    ref var nameComp = ref entity.GetComponent<NicknameComponent>();
                    nameComp.Nickname = actor.GetClass().GetName();
#if TESTING
                    MarkerUtils.CreateMarkerForCharacter(entity);
#endif
                    Logging.LogDebug("Found matching tamer with guid: {Guid}", tamer.Guid);
                }
                else
                {
                    // spawn tamer
                    Logging.LogDebug("Matching tamer not found for guid: {Guid}, spawning...", tamer.Guid);
                    // SpawningUtils.SpawnUnitLocally(netId, tamer.Guid, tamer.UnitPath, team.TeamId, trans.Position.X, trans.Position.Y, trans.Position.Z);
                }
            }
        });
    }
}