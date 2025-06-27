using b1;
using LiteNetLib.Utils;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.Api.Serialization;
using ReadyM.Relay.Client;

namespace WukongMp.Api.DTO;

[DeriveINetSerializable]
[DeriveJsonSerializable]
public partial struct SimpleStateData(NetworkIdComponent netId, EBGUSimpleState simpleState, bool isRemove) : INetSerializable
{
    public NetworkIdComponent NetId = netId;
    public EBGUSimpleState SimpleState = simpleState;
    public bool IsRemove = isRemove;
}
