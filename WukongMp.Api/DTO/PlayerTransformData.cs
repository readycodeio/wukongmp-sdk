using LiteNetLib.Utils;
using ReadyM.Relay.Common;
using UnrealEngine.Runtime;

namespace WukongMp.Api.DTO;

public struct PlayerTransformData(PlayerId playerId, FVector location, FRotator rotation) : INetSerializable
{
    public PlayerId PlayerId = playerId;
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
        PlayerId = reader.Get<PlayerId>();
        Location = (FVector)SerializationHelpers.DeserializeFVector(reader);
        Rotation = (FRotator)SerializationHelpers.DeserializeFRotator(reader);
    }
}