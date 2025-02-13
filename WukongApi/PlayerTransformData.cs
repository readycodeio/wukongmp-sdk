using System;
using Photon.Client;
using UnrealEngine.Runtime;

namespace WukongApi
{
    public readonly struct PlayerTransformData
    {
        public readonly int PlayerId;
        public readonly FVector Location;
        public readonly FRotator Rotation;

        public PlayerTransformData(int playerId, FVector location, FRotator rotation)
        {
            PlayerId = playerId;
            Location = location;
            Rotation = rotation;
        }

        public static short Serialize(StreamBuffer outStream, object customObject)
        {
            var data = (PlayerTransformData)customObject;

            short messageLength = 4;
            outStream.Write(BitConverter.GetBytes(data.PlayerId), 0, 4);
            messageLength += SerializationHelpers.SerializeFVector(outStream, data.Location);
            messageLength += SerializationHelpers.SerializeFRotator(outStream, data.Rotation);
            return messageLength;
        }

        public static object Deserialize(StreamBuffer inStream, short length)
        {
            var intBytes = new byte[4];
            inStream.Read(intBytes, 0, 4);
            var playerId = BitConverter.ToInt32(intBytes, 0);

            var location = (FVector)SerializationHelpers.DeserializeFVector(inStream, 12);
            var rotation = (FRotator)SerializationHelpers.DeserializeFRotator(inStream, 12);

            return new PlayerTransformData(playerId, location, rotation);
        }
    }
}