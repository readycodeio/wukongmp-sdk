using Friflo.Engine.ECS;
using ReadyM.Api.Idents;
using ReadyM.Wukong.Common.ECS.Components;
using WukongMp.Api.WukongUtils;

namespace WukongMp.Api.ECS.Jobs;

/// <summary>
/// This job is used to clear tamers for a specific player, typically when they leave the game or the room.
/// </summary>
/// <param name="playerId"></param>
public readonly struct ClearPlayerTamerRefCountJob(PlayerId playerId) : IEach<TamerComponent>
{
    public void Execute(ref TamerComponent tamer)
    {
        TamerUtils.SubtractSpawnedUnitRefCount(ref tamer, playerId);
    }
}
