using System;
using Friflo.Engine.ECS;
using WukongMp.Api.Mapping.Tags;

namespace WukongMp.Api.ECS.GameEvents;

internal readonly struct TriggerImmobilizeEvent(
    Entity target,
    Entity caster,
    bool greatSageTalentActiveBuff) : IEquatable<TriggerImmobilizeEvent>, IMasterClientManaged
{
    public readonly Entity Target = target;
    public readonly Entity Caster = caster;
    public readonly bool GreatSageTalentActiveBuff = greatSageTalentActiveBuff;

    public bool Equals(TriggerImmobilizeEvent other)
        => Target == other.Target && Caster == other.Caster && GreatSageTalentActiveBuff == other.GreatSageTalentActiveBuff;

    public override bool Equals(object? obj)
        => obj is TriggerImmobilizeEvent other && Equals(other);

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = Target.GetHashCode();
            hashCode = (hashCode * 397) ^ Caster.GetHashCode();
            hashCode = (hashCode * 397) ^ GreatSageTalentActiveBuff.GetHashCode();
            return hashCode;
        }
    }
}