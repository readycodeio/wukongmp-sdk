using System;
using b1;
using Photon.Client;
using UnrealEngine.Runtime;

namespace WukongApi
{
    public static class SerializationHelpers
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

        public static short SerializeDamageNumParam(StreamBuffer outStream, object obj)
        {
            var dmg = (DamageNumParam)obj;
            outStream.Write(BitConverter.GetBytes(dmg.DamageNum), 0, 4);
            outStream.WriteByte((byte)dmg.DamageType);
            var s1 = SerializeFVector(outStream, dmg.RealHitLocation);
            outStream.Write(BitConverter.GetBytes(dmg.Amplitude), 0, 4);
            outStream.WriteByte((byte)dmg.AttackerTeamType);
            var s2 = SerializeFVector(outStream, dmg.RealHitDir);
            return (short)(4 + 1 + s1 + 4 + 1 + s2);
        }

        public static object DeserializeDamageNumParam(StreamBuffer inStream, short length)
        {
            var intBytes = new byte[4];
            inStream.Read(intBytes, 0, 4);
            var damageNum = BitConverter.ToInt32(intBytes, 0);
            var damageType = (EDamageNumberType)inStream.ReadByte();
            var realHitLocation = (FVector)DeserializeFVector(inStream, 12);
            inStream.Read(intBytes, 0, 4);
            var amplitude = BitConverter.ToSingle(intBytes, 0);
            var attackerTeamType = (EDmgNumUITeamType)inStream.ReadByte();
            var realHitDir = (FVector)DeserializeFVector(inStream, 12);
            return new DamageNumParam(damageType, damageNum, amplitude, realHitLocation, realHitDir, attackerTeamType);
        }
    }
}