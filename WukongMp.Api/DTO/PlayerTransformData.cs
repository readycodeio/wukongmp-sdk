using LiteNetLib.Utils;
using UnrealEngine.Runtime;

namespace WukongMp.Api.DTO
{
    public struct PlayerTransformData(int playerId, FVector location, FRotator rotation) : INetSerializable
    {
        public int PlayerId = playerId;
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
            PlayerId = reader.GetInt();
            Location = (FVector)SerializationHelpers.DeserializeFVector(reader);
            Rotation = (FRotator)SerializationHelpers.DeserializeFRotator(reader);
        }
    }
}