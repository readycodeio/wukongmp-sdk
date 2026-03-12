using b1;
using LiteNetLib.Utils;
using UnrealEngine.Runtime;

namespace WukongMp.Api.Serialization;

internal static class SerializationHelpers
{
    public static void SerializeFVector(NetDataWriter outStream, object obj)
    {
        var vec = (FVector)obj;
        outStream.Put(vec.X);
        outStream.Put(vec.Y);
        outStream.Put(vec.Z);
    }

    public static object DeserializeFVector(NetDataReader inStream)
    {
        var x = inStream.GetFloat();
        var y = inStream.GetFloat();
        var z = inStream.GetFloat();
        return new FVector(x, y, z);
    }

    public static void SerializeFVector2D(NetDataWriter outStream, object obj)
    {
        var vec = (FVector2D)obj;
        outStream.Put(vec.X);
        outStream.Put(vec.Y);
    }

    public static object DeserializeFVector2D(NetDataReader inStream)
    {
        var x = inStream.GetFloat();
        var y = inStream.GetFloat();
        return new FVector2D(x, y);
    }

    public static void SerializeFRotator(NetDataWriter outStream, object obj)
    {
        var vec = (FRotator)obj;
        outStream.Put(vec.Pitch);
        outStream.Put(vec.Yaw);
        outStream.Put(vec.Roll);
    }

    public static object DeserializeFRotator(NetDataReader inStream)
    {
        var pitch = inStream.GetFloat();
        var yaw = inStream.GetFloat();
        var roll = inStream.GetFloat();
        return new FRotator(pitch, yaw, roll);
    }

    public static void SerializeDamageNumParam(NetDataWriter outStream, object obj)
    {
        var dmg = (DamageNumParam)obj;

        outStream.Put(dmg.DamageNum);
        outStream.Put((byte)dmg.DamageType);
        SerializeFVector(outStream, dmg.RealHitLocation);
        outStream.Put(dmg.Amplitude);
        outStream.Put((byte)dmg.AttackerTeamType);
        SerializeFVector(outStream, dmg.RealHitDir);
    }

    public static object DeserializeDamageNumParam(NetDataReader inStream)
    {
        var damageNum = inStream.GetInt();
        var damageType = (EDamageNumberType)inStream.GetByte();
        var realHitLocation = (FVector)DeserializeFVector(inStream);
        var amplitude = inStream.GetFloat();
        var attackerTeamType = (EDmgNumUITeamType)inStream.GetByte();
        var realHitDir = (FVector)DeserializeFVector(inStream);
        return new DamageNumParam(damageType, damageNum, amplitude, realHitLocation, realHitDir, attackerTeamType);
    }
}
