using System;
using Friflo.Engine.ECS;
using ReadyM.Api.Mapping.Events;

namespace WukongMp.Api.ECS.GameEvents;

[DeriveIGameEvent, RunOnMasterClientOnly(nameof(Caster))]
internal readonly partial struct CastImmobilizeEvent(Entity caster) : IEquatable<CastImmobilizeEvent>
{
    public readonly Entity Caster = caster;

    public bool Equals(CastImmobilizeEvent other)
        => Caster == other.Caster;

    public override bool Equals(object? obj)
        => obj is CastImmobilizeEvent other && Equals(other);

    public override int GetHashCode()
        => Caster.GetHashCode();
}