using b1;
using LiteNetLib.Utils;
using ReadyM.Api.Multiplayer;
using ReadyM.Relay.Common.ECS;

namespace WukongMp.Api.DTO;

[DeriveINetSerializable]
public partial struct SimpleStateData(NetworkIdComponent netId, EBGUSimpleState simpleState, bool isRemove) : INetSerializable
{
    public NetworkIdComponent NetId = netId;
    public EBGUSimpleState SimpleState = simpleState;
    public bool IsRemove = isRemove;
}