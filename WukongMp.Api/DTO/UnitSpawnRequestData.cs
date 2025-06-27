using LiteNetLib.Utils;
using ReadyM.Api.Serialization;
using ReadyM.Relay.Client;

namespace WukongMp.Api.DTO;

[DeriveINetSerializable]
[DeriveJsonSerializable]
public partial struct UnitSpawnRequestData(string unitName, int count, int teamId) : INetSerializable
{
    public string UnitName = unitName;
    public int Count = count;
    public int TeamId = teamId;
}
