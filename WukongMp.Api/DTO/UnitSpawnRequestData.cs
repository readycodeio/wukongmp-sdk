using LiteNetLib.Utils;
using ReadyM.Api.Multiplayer.Generators;
using ReadyM.Api.Serialization;
using UnrealEngine.Runtime;
using WukongMp.Api.Serialization;

namespace WukongMp.Api.DTO;

[DeriveJsonSerializable]
public partial struct UnitSpawnRequestData(string unitName, string guid, int teamId, FVector location) : INetSerializable
{
    public string UnitName = unitName;
    public string Guid = guid;
    public int TeamId = teamId;
    public FVector Location = location;

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(UnitName);
        writer.Put(Guid);
        writer.Put(TeamId);
        SerializationHelpers.SerializeFVector(writer, Location);
    }

    public void Deserialize(NetDataReader reader)
    {
        UnitName = reader.GetString();
        Guid = reader.GetString();
        TeamId = reader.GetInt();
        Location = (FVector)SerializationHelpers.DeserializeFVector(reader);
    }
}
