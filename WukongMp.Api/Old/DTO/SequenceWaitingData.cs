using LiteNetLib.Utils;
using UnrealEngine.Runtime;

namespace WukongMp.Api.Old.DTO;

public readonly struct SequenceWaitingData(int sequenceID, FVector sequenceLocation)
{
    public readonly int SequenceID = sequenceID;
    public readonly FVector SequenceLocation = sequenceLocation;

    public static void Serialize(NetDataWriter outStream, object customObject)
    {
        var data = (SequenceWaitingData)customObject;
        outStream.Put(data.SequenceID);
        SerializationHelpers.SerializeFVector(outStream, data.SequenceLocation);
    }

    public static object Deserialize(NetDataReader inStream)
    {
        var sequenceID = inStream.GetInt();
        var location = (FVector)SerializationHelpers.DeserializeFVector(inStream);
        return new SequenceWaitingData(sequenceID, location);
    }
}
