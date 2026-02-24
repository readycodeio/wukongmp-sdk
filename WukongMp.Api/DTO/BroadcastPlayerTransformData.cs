using LiteNetLib.Utils;
using ReadyM.Api.Multiplayer.ECS.Values;
using ReadyM.Api.Serialization;
using UnrealEngine.Runtime;
using WukongMp.Api.Serialization;

namespace WukongMp.Api.DTO;

[DeriveJsonSerializable]
public partial struct BroadcastPlayerTransformData(
    NetworkId netId,
    FVector location,
    FRotator rotation) : INetSerializable
{
    public NetworkId NetId = netId;
    public FVector Location = location;
    public FRotator Rotation = rotation;

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(NetId);
        SerializationHelpers.SerializeFVector(writer, Location);
        SerializationHelpers.SerializeFRotator(writer, Rotation);
    }

    public void Deserialize(NetDataReader reader)
    {
        NetId = reader.Get<NetworkId>();
        Location = (FVector)SerializationHelpers.DeserializeFVector(reader);
        Rotation = (FRotator)SerializationHelpers.DeserializeFRotator(reader);
    }
}
