using LiteNetLib.Utils;
using ReadyM.Api.Multiplayer.ECS.Values;
using ReadyM.Api.Serialization;
using UnrealEngine.Runtime;
using WukongMp.Api.Serialization;

namespace WukongMp.Api.DTO;

[DeriveJsonSerializable]
internal partial struct BroadcastUnitSpawnData(
    NetworkId netId,
    string? unitName, 
    string guid,
    FVector location) : INetSerializable
{
    public NetworkId NetId = netId;
    public string? UnitName = unitName;
    public string Guid = guid;
    public FVector Location = location;

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(NetId);
        writer.Put(UnitName);
        writer.Put(Guid);
        SerializationHelpers.SerializeFVector(writer, Location);
    }

    public void Deserialize(NetDataReader reader)
    {
        NetId = reader.Get<NetworkId>();
        UnitName = reader.GetString();
        Guid = reader.GetString();
        Location = (FVector)SerializationHelpers.DeserializeFVector(reader);
    }
}
