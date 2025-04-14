using LiteNetLib.Utils;
using UnrealEngine.Runtime;

namespace WukongApi
{
    public readonly struct PlayerTransformData(int playerId, FVector location, FRotator rotation)
    {
        public readonly int PlayerId = playerId;
        public readonly FVector Location = location;
        public readonly FRotator Rotation = rotation;

        public static void Serialize(NetDataWriter outStream, object customObject)
        {
            var data = (PlayerTransformData)customObject;
            outStream.Put(data.PlayerId);
            SerializationHelpers.SerializeFVector(outStream, data.Location);
            SerializationHelpers.SerializeFRotator(outStream, data.Rotation);
        }

        public static object Deserialize(NetDataReader inStream)
        {
            var playerId = inStream.GetInt();
            var location = (FVector)SerializationHelpers.DeserializeFVector(inStream);
            var rotation = (FRotator)SerializationHelpers.DeserializeFRotator(inStream);
            return new PlayerTransformData(playerId, location, rotation);
        }
    }
}