using LiteNetLib.Utils;
using ReadyM.Api.Multiplayer;
using ReadyM.Relay.Common.ECS;

namespace WukongMp.Api.DTO;

[DeriveINetSerializable]
public partial struct UnitSummonData(NetworkIdComponent summonerId, NetworkIdComponent summonId, string guid, string name, int teamId) : INetSerializable
{
    public NetworkIdComponent SummonerId = summonerId;
    public NetworkIdComponent SummonId = summonId;
    public string Guid = guid;
    public string Name = name;
    public int TeamId = teamId;
}