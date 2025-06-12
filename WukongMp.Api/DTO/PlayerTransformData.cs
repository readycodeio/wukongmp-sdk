using LiteNetLib.Utils;
using ReadyM.Relay.Common;
using UnrealEngine.Runtime;

namespace WukongMp.Api.DTO;

public struct PlayerTransformData(UserId playerId, FVector location, FRotator rotation) : INetSerializable
{
    public UserId PlayerId = playerId;
    public FVector Location = location;
    public FRotator Rotation = rotation;

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(PlayerId);
        SerializationHelpers.SerializeFVector(writer, Location);
        SerializationHelpers.SerializeFRotator(writer, Rotation);
    }

    public void Deserialize(NetDataReader reader)
    {
        PlayerId = reader.Get<UserId>();
        Location = (FVector)SerializationHelpers.DeserializeFVector(reader);
        Rotation = (FRotator)SerializationHelpers.DeserializeFRotator(reader);
    }
}