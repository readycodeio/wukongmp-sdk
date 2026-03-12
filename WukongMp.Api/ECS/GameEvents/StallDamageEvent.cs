using ReadyM.Api.Multiplayer.ECS.Values;
using ReadyM.Api.Multiplayer.Mapping.Tags;

namespace WukongMp.Api.ECS.GameEvents;

internal readonly struct StallDamageEvent(NetworkId target, float damage) : IAlwaysPropagatesToEcsOnly
{
    public NetworkId Target { get; } = target;
    public float Damage { get; } = damage;
}