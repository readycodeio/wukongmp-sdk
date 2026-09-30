using System;
using Friflo.Engine.ECS;
using ReadyM.Api.Mapping.Events;

namespace WukongMp.Api.ECS.GameEvents;

[DeriveIGameEvent, MasterClientManaged]
internal readonly partial struct RelieveImmobilizeEvent(Entity affected)
    : IEquatable<RelieveImmobilizeEvent>
{
    public readonly Entity Affected = affected;

    public bool Equals(RelieveImmobilizeEvent other)
        => Affected == other.Affected;

    public override bool Equals(object? obj)
        => obj is RelieveImmobilizeEvent other && Equals(other);

    public override int GetHashCode()
        => Affected.GetHashCode();
}