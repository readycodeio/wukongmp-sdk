using System;
using Friflo.Engine.ECS;
using ReadyM.Api.Multiplayer.Mapping.Tags;

namespace WukongMp.Api.ECS.GameEvents;

public readonly struct TriggerImmobilizeEvent(
    Entity entity,
    Entity target,
    bool greatSageTalentActiveBuff) : IEquatable<TriggerImmobilizeEvent>, IAlwaysPropagates
{
    public readonly Entity Entity = entity;
    public readonly Entity Target = target;
    public readonly bool GreatSageTalentActiveBuff = greatSageTalentActiveBuff;

    public bool Equals(TriggerImmobilizeEvent other)
        => Entity == other.Entity && Target == other.Target && GreatSageTalentActiveBuff == other.GreatSageTalentActiveBuff;

    public override bool Equals(object? obj)
        => obj is TriggerImmobilizeEvent other && Equals(other);

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = Entity.GetHashCode();
            hashCode = (hashCode * 397) ^ Target.GetHashCode();
            hashCode = (hashCode * 397) ^ GreatSageTalentActiveBuff.GetHashCode();
            return hashCode;
        }
    }
}