using b1;
using ReadyM.Api.Multiplayer;
using ReadyM.Relay.Common.ECS;

namespace WukongMp.Api.DTO;

[DeriveINetSerializable]
public partial struct SimpleStateData(NetworkIdComponent netId, EBGUSimpleState simpleState, bool isRemove)
{
    public NetworkIdComponent NetId = netId;
    public EBGUSimpleState SimpleState = simpleState;
    public bool IsRemove = isRemove;
}