using LiteNetLib.Utils;
using ReadyM.Api.Multiplayer.Generators;
using ReadyM.Api.Serialization;
using UnrealEngine.Runtime;
using WukongMp.Api.Serialization;

namespace WukongMp.Api.DTO;

[DeriveJsonSerializable]
public partial struct UnitSpawnData(string unitName, string guid, FVector location) : INetSerializable
{
    public string UnitName = unitName;
    public string Guid = guid;
    public FVector Location = location;

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(UnitName);
        writer.Put(Guid);
        SerializationHelpers.SerializeFVector(writer, Location);
    }

    public void Deserialize(NetDataReader reader)
    {
        UnitName = reader.GetString();
        Guid = reader.GetString();
        Location = (FVector)SerializationHelpers.DeserializeFVector(reader);
    }
}
