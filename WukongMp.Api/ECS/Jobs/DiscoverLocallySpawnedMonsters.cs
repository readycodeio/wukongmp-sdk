using b1;
using Friflo.Engine.ECS;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.Api.Multiplayer.Idents;
using ReadyM.Relay.Common.Wukong.ECS.Components;
using WukongMp.Api.ECS.Components;
using WukongMp.Api.WukongUtils;

namespace WukongMp.Api.ECS.Jobs;

/// <summary>
/// This job is used to discover already spawned monsters.
/// </summary>
public readonly struct DiscoverLocallySpawnedMonsters() : IEach<LocalTamerComponent, MetadataComponent>
{
    public void Execute(ref LocalTamerComponent tamer, ref MetadataComponent metadata)
    {
        if (tamer.Tamer?.GetMonster() != null)
        {
            Logging.LogDebug("Monster {Guid} is already spawned", BGU_DataUtil.GetActorGuid(tamer.Tamer));
            TamerUtils.MarkMonsterLocallySpawned(ref tamer, metadata);
        }
    }
}
