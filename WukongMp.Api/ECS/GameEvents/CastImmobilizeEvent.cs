using System;
using Friflo.Engine.ECS;
using ReadyM.Relay.Client.Mapping;

namespace WukongMp.Api.ECS.GameEvents;

public readonly struct CastImmobilizeEvent(Entity caster) : IEquatable<CastImmobilizeEvent>, IRunOnMasterClientOnly
{
    public readonly Entity Caster = caster;

    public bool Equals(CastImmobilizeEvent other)
        => Caster == other.Caster;

    public override bool Equals(object? obj)
        => obj is CastImmobilizeEvent other && Equals(other);

    public override int GetHashCode()
        => Caster.GetHashCode();
}