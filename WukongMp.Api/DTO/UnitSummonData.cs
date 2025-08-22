using ReadyM.Api.Multiplayer.ECS.Values;
using ReadyM.Api.Multiplayer.Generators;
using ReadyM.Api.Serialization;

namespace WukongMp.Api.DTO;

[DeriveINetSerializable]
[DeriveJsonSerializable]
public partial struct UnitSummonData(NetworkId summonerId, NetworkId summonId, string guid, string name, int teamId)
{
    public NetworkId SummonerId = summonerId;
    public NetworkId SummonId = summonId;
    public string Guid = guid;
    public string Name = name;
    public int TeamId = teamId;
}
