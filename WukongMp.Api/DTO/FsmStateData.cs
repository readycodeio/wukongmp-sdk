using LiteNetLib.Utils;
using ReadyM.Api.Multiplayer.ECS.Values;
using ReadyM.Api.Multiplayer.Generators;
using ReadyM.Api.Serialization;

namespace WukongMp.Api.DTO;

[DeriveINetSerializable]
[DeriveJsonSerializable]
public partial struct FsmStateData(NetworkId netId, string fsmStateName) : INetSerializable
{
    public NetworkId NetId = netId;
    public string FsmStateName = fsmStateName;
}
