using ReadyM.Api.ECS.Worlds;
using WukongMp.Api.State;

namespace WukongMp.Api.GameEvents;

/// <summary>
/// The game event context for the policies WukongMP writes by hand: its player state, its area state and the ECS
/// world. Only <see cref="ECS.GameEvents.SpawnSummonEvent"/> reads it.
/// </summary>
internal readonly struct WukongPlayerContext(WukongPlayerState playerState, WukongAreaState areaState, Store world)
{
    public readonly WukongPlayerState PlayerState = playerState;
    public readonly WukongAreaState AreaState = areaState;
    public readonly Store World = world;
}
