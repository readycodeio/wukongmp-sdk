using LiteNetLib.Utils;
using ReadyM.Api.Multiplayer.Idents;
using ReadyM.Api.Serialization;
using UnrealEngine.Runtime;
using WukongMp.Api.Serialization;

namespace WukongMp.Api.DTO;

[DeriveJsonSerializable]
public partial struct PlayerTransformData(PlayerId playerId, FVector location, FRotator rotation) : INetSerializable
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
