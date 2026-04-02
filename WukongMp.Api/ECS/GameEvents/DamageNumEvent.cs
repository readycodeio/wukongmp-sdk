using System;
using b1;
using Friflo.Engine.ECS;
using ReadyM.Api.Multiplayer.Mapping.Tags;
using UnrealEngine.Runtime;

namespace WukongMp.Api.ECS.GameEvents;

internal readonly struct DamageNumEvent(
    Entity entity,
    EDamageNumberType damageType,
    int damageNum,
    float amplitude,
    FVector realHitLocation,
    FVector realHitDir,
    EDmgNumUITeamType attackerTeamType) : IEquatable<DamageNumEvent>, IOwnershipManaged
{
    public readonly Entity Entity = entity;
    public readonly EDamageNumberType DamageType = damageType;
    public readonly int DamageNum = damageNum;
    public readonly float Amplitude = amplitude;
    public readonly FVector RealHitLocation = realHitLocation;
    public readonly FVector RealHitDir = realHitDir;
    public readonly EDmgNumUITeamType AttackerTeamType = attackerTeamType;

    public bool Equals(DamageNumEvent other)
        => (
            Entity == other.Entity &&
            DamageType == other.DamageType && 
            DamageNum == other.DamageNum && 
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            Amplitude == other.Amplitude && 
            RealHitLocation == other.RealHitLocation && 
            RealHitDir == other.RealHitDir && 
            AttackerTeamType == other.AttackerTeamType
        );

    public override bool Equals(object? obj)
        => obj is DamageNumEvent other && Equals(other);

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = Entity.GetHashCode();
            hashCode = (hashCode * 397) ^ (int)DamageType;
            hashCode = (hashCode * 397) ^ DamageNum;
            hashCode = (hashCode * 397) ^ Amplitude.GetHashCode();
            hashCode = (hashCode * 397) ^ RealHitLocation.GetHashCode();
            hashCode = (hashCode * 397) ^ RealHitDir.GetHashCode();
            hashCode = (hashCode * 397) ^ (int)AttackerTeamType;
            return hashCode;
        }
    }
}