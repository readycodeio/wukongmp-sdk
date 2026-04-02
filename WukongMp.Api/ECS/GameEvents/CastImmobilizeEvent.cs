using System;
using Friflo.Engine.ECS;
using WukongMp.Api.Mapping.Tags;

namespace WukongMp.Api.ECS.GameEvents;

internal readonly struct CastImmobilizeEvent(Entity caster) : IEquatable<CastImmobilizeEvent>, IRunOnMasterClientOnly
{
    public readonly Entity Caster = caster;

    public bool Equals(CastImmobilizeEvent other)
        => Caster == other.Caster;

    public override bool Equals(object? obj)
        => obj is CastImmobilizeEvent other && Equals(other);

    public override int GetHashCode()
        => Caster.GetHashCode();
}