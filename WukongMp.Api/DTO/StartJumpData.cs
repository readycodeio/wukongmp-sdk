using b1;
using LiteNetLib.Utils;
using ReadyM.Api.Multiplayer.ECS.Values;
using ReadyM.Api.Serialization;
using UnrealEngine.Runtime;
using WukongMp.Api.Serialization;

namespace WukongMp.Api.DTO;

[DeriveJsonSerializable]
internal partial struct StartJumpData(NetworkId netId, ESkillDirection startJumpDir, FVector2D inputVector) : INetSerializable
{
    public NetworkId NetId = netId;
    public ESkillDirection StartJumpDir = startJumpDir;
    public FVector2D InputVector = inputVector;

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(NetId);
        writer.Put((byte)StartJumpDir);
        SerializationHelpers.SerializeFVector2D(writer, InputVector);
    }

    public void Deserialize(NetDataReader reader)
    {
        NetId = reader.Get<NetworkId>();
        StartJumpDir = (ESkillDirection)reader.GetByte();
        InputVector = (FVector2D)SerializationHelpers.DeserializeFVector2D(reader);
    }
}