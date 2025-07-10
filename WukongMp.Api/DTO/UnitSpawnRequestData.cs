using LiteNetLib.Utils;
using ReadyM.Relay.Client;
using ReadyM.Relay.Common.Serialization;

namespace WukongMp.Api.DTO;

[DeriveINetSerializable]
[DeriveJsonSerializable]
public partial struct UnitSpawnRequestData(string unitName, int count, int teamId) : INetSerializable
{
    public string UnitName = unitName;
    public int Count = count;
    public int TeamId = teamId;
}
