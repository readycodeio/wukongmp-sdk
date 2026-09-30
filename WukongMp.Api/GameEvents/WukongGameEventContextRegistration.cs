using ReadyM.Api.ECS.Worlds;
using ReadyM.Api.Mapping.Events;
using WukongMp.Api.State;

namespace WukongMp.Api.GameEvents;

/// <summary>Registers <see cref="WukongPlayerContext"/>; the ownership and master-client contexts come from Core's
/// client registration.</summary>
internal sealed class WukongGameEventContextRegistration(
    WukongPlayerState playerState,
    WukongAreaState areaState,
    Store world
) : IGameEventContextRegistration
{
    public void Register(GameEventContextRegistry registry)
        => registry.Register(new WukongPlayerContext(playerState, areaState, world));
}
