using LiteNetLib.Utils;
using ReadyM.Api.Multiplayer.ECS.Values;
using ReadyM.Api.Multiplayer.Generators;
using ReadyM.Api.Serialization;

namespace WukongMp.Api.DTO;

[DeriveINetSerializable]
[DeriveJsonSerializable]
public partial struct AoTargetData(NetworkId character, NetworkId target, bool isPlayer, byte sourceType, float degreeLimit) : INetSerializable
{
    public NetworkId Character = character;
    public NetworkId Target = target;
    public bool IsPlayer = isPlayer;
    public byte SourceType = sourceType;
    public float DegreeLimit = degreeLimit;
}