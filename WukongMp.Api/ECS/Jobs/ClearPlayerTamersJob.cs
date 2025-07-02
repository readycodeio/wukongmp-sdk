using Friflo.Engine.ECS;
using ReadyM.Relay.Common;
using ReadyM.Relay.Common.Wukong.Components;
using WukongMp.Api.WukongUtils;

namespace WukongMp.Api.ECS.Jobs;

/// <summary>
/// This job is used to clear tamers for a specific player, typically when they leave the game or the room.
/// </summary>
/// <param name="playerId"></param>
public readonly struct ClearPlayerTamersJob(PlayerId playerId) : IEach<TamerComponent>
{
    public void Execute(ref TamerComponent tamer)
    {
        TamerUtils.SubtractSpawnedUnit(playerId, ref tamer);
    }
}
