using b1;
using LiteNetLib.Utils;
using ReadyM.Relay.Client;
using ReadyM.Relay.Common.ECS;
using ReadyM.Relay.Common.Serialization;

namespace WukongMp.Api.DTO;

[DeriveINetSerializable]
[DeriveJsonSerializable]
public partial struct SimpleStateData(NetworkIdComponent netId, EBGUSimpleState simpleState, bool isRemove) : INetSerializable
{
    public NetworkIdComponent NetId = netId;
    public EBGUSimpleState SimpleState = simpleState;
    public bool IsRemove = isRemove;
}
