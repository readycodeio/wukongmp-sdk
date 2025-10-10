using System.Linq;
using b1;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using ReadyM.Relay.Common.Wukong.ECS.Components;
using UnrealEngine.Engine;
using WukongMp.Api.ECS.Components;
using WukongMp.Api.WukongUtils;

namespace WukongMp.Api.ECS.Systems.Tamers;

public sealed class SyncTamersSystem : QuerySystem<TamerComponent, LocalTamerComponent>
{
    private const ulong TickInterval = 10; // Check every 10 ticks
    private ulong tickCounter;

    protected override void OnUpdate()
    {
        if (tickCounter++ % TickInterval != 0)
            return;

        var allTamers =
            UGameplayStatics.GetAllActorsOfClass<BUTamerActor>(GameUtils.GetWorld())
                .Where(x => x != null)
                .GroupBy(x => x.GetFinalGuid())
                .ToDictionary(g => g.Key, g => g.Last());

        Query.ForEachEntity((ref TamerComponent tamerComp, ref LocalTamerComponent localTamerComp, Entity entity) =>
        {
            if (!localTamerComp.IsTamerSynced)
            {
                if (tamerComp.Guid is null)
                {
                    Logging.LogError("Entity {EntityId} has a TamerComponent with a null Guid. Cannot sync tamer.", entity.Id);
                    return;
                }

                if (allTamers.TryGetValue(tamerComp.Guid, out var actor))
                {
                    localTamerComp.Tamer = actor;
                    localTamerComp.IsTamerSynced = true;
#if TESTING
                    ref var nameComp = ref entity.GetComponent<NicknameComponent>();
                    nameComp.Nickname = actor.GetClass()?.GetName();
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