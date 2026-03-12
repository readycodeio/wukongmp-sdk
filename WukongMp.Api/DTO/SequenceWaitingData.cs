using LiteNetLib.Utils;
using ReadyM.Api.Serialization;
using UnrealEngine.Runtime;
using WukongMp.Api.Serialization;

namespace WukongMp.Api.DTO;

[DeriveJsonSerializable]
internal partial struct SequenceWaitingData(int sequenceID, FVector sequenceLocation) : INetSerializable
{
    public int SequenceID = sequenceID;
    public FVector SequenceLocation = sequenceLocation;

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(SequenceID);
        SerializationHelpers.SerializeFVector(writer, SequenceLocation);
    }

    public void Deserialize(NetDataReader reader)
    {
        SequenceID = reader.GetInt();
        SequenceLocation = (FVector)SerializationHelpers.DeserializeFVector(reader);
    }
}
