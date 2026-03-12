using System;
using b1;
using LiteNetLib.Utils;
using ReadyM.Api.Multiplayer.ECS.Values;
using ReadyM.Api.Multiplayer.Generators;
using ReadyM.Api.Serialization;

namespace WukongMp.Api.DTO;

[DeriveINetSerializable]
[DeriveJsonSerializable]
internal partial struct PlayerTransEndData(
    NetworkId netId,
    int unitResId, 
    int unitBornSkillId,
    bool enableBlendViewTarget,
    EPlayerTransEndType transEndType) 
    : INetSerializable, IEquatable<PlayerTransEndData>
{
    public NetworkId NetId = netId;
    public int UnitResId = unitResId;
    public int UnitBornSkillId = unitBornSkillId;
    public bool EnableBlendViewTarget = enableBlendViewTarget;
    public EPlayerTransEndType TransEndType = transEndType;

    public bool Equals(PlayerTransEndData other)
        => UnitResId == other.UnitResId && 
           UnitBornSkillId == other.UnitBornSkillId && 
           EnableBlendViewTarget == other.EnableBlendViewTarget && 
           TransEndType == other.TransEndType;

    public override bool Equals(object? obj)
        => obj is PlayerTransEndData other && Equals(other);

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = UnitResId;
            hashCode = (hashCode * 397) ^ UnitBornSkillId;
            hashCode = (hashCode * 397) ^ EnableBlendViewTarget.GetHashCode();
            hashCode = (hashCode * 397) ^ (int)TransEndType;
            return hashCode;
        }
    }
}
