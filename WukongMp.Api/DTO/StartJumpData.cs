using b1;
using LiteNetLib.Utils;
using ReadyM.Api.Multiplayer.Generators;
using ReadyM.Api.Serialization;
using UnrealEngine.Runtime;
using WukongMp.Api.Serialization;

namespace WukongMp.Api.DTO;

[DeriveJsonSerializable]
public partial struct StartJumpData(ESkillDirection startJumpDir, FVector2D inputVector) : INetSerializable
{
    public ESkillDirection StartJumpDir = startJumpDir;
    public FVector2D InputVector = inputVector;

    public void Serialize(NetDataWriter writer)
    {
        writer.Put((byte)StartJumpDir);
        SerializationHelpers.SerializeFVector2D(writer, InputVector);
    }

    public void Deserialize(NetDataReader reader)
    {
        StartJumpDir = (ESkillDirection)reader.GetByte();
        InputVector = (FVector2D)SerializationHelpers.DeserializeFVector2D(reader);
    }
}