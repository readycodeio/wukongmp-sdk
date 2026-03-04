using System;
using Friflo.Engine.ECS;
using WukongMp.Api.Mapping.Tags;

namespace WukongMp.Api.ECS.GameEvents;

public readonly struct RelieveImmobilizeEvent(Entity affected)
    : IEquatable<RelieveImmobilizeEvent>, IMasterClientManaged
{
    public readonly Entity Affected = affected;

    public bool Equals(RelieveImmobilizeEvent other)
        => Affected == other.Affected;

    public override bool Equals(object? obj)
        => obj is RelieveImmobilizeEvent other && Equals(other);

    public override int GetHashCode()
        => Affected.GetHashCode();
}