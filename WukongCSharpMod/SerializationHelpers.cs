using System;
using Photon.Client;
using UnrealEngine.Runtime;

namespace WukongCSharpMod
{
    public class SerializationHelpers
    {
        public static short SerializeFVector(StreamBuffer outStream, object obj)
        {
            var vec = (FVector)obj;
            outStream.Write(BitConverter.GetBytes(vec.X), 0, 4);
            outStream.Write(BitConverter.GetBytes(vec.Y), 0, 4);
            outStream.Write(BitConverter.GetBytes(vec.Z), 0, 4);
            return 12;
        }

        public static object DeserializeFVector(StreamBuffer inStream, short length)
        {
            var floatBytes = new byte[4];
            inStream.Read(floatBytes, 0, 4);
            var x = BitConverter.ToSingle(floatBytes, 0);
            inStream.Read(floatBytes, 0, 4);
            var y = BitConverter.ToSingle(floatBytes, 0);
            inStream.Read(floatBytes, 0, 4);
            var z = BitConverter.ToSingle(floatBytes, 0);
            return new FVector(x, y, z);
        }

        public static short SerializeFRotator(StreamBuffer outStream, object obj)
        {
            var vec = (FRotator)obj;
            outStream.Write(BitConverter.GetBytes(vec.Pitch), 0, 4);
            outStream.Write(BitConverter.GetBytes(vec.Yaw), 0, 4);
            outStream.Write(BitConverter.GetBytes(vec.Roll), 0, 4);
            return 12;
        }

        public static object DeserializeFRotator(StreamBuffer inStream, short length)
        {
            var floatBytes = new byte[4];
            inStream.Read(floatBytes, 0, 4);
            var pitch = BitConverter.ToSingle(floatBytes, 0);
            inStream.Read(floatBytes, 0, 4);
            var yaw = BitConverter.ToSingle(floatBytes, 0);
            inStream.Read(floatBytes, 0, 4);
            var roll = BitConverter.ToSingle(floatBytes, 0);
            return new FRotator(pitch, yaw, roll);
        }
    }
}