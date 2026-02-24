using LiteNetLib.Utils;
using ReadyM.Api.Serialization;
using UnrealEngine.Runtime;
using WukongMp.Api.Serialization;

namespace WukongMp.Api.DTO;

[DeriveJsonSerializable]
public partial struct RequestSpawnUnitsData(
    string unitName, 
    int count,
    int teamId,
    FVector location) : INetSerializable
{
    public string UnitName = unitName;
    public int Count = count;
    public int TeamId = teamId;
    public FVector Location = location;

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(UnitName);
        writer.Put(Count);
        writer.Put(TeamId);
        SerializationHelpers.SerializeFVector(writer, Location);
    }

    public void Deserialize(NetDataReader reader)
    {
        UnitName = reader.GetString();
        Count = reader.GetInt();
        TeamId = reader.GetInt();
        Location = (FVector)SerializationHelpers.DeserializeFVector(reader);
    }
}
