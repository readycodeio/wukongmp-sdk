using b1;
using LiteNetLib.Utils;
using ReadyM.Api.Multiplayer;
using ReadyM.Relay.Common.ECS;

namespace WukongMp.Api.DTO;

[DeriveINetSerializable]
public partial struct MotionMatchingStateData(NetworkIdComponent netId, EState_MM state) : INetSerializable
{
    public NetworkIdComponent NetId = netId;
    public EState_MM State = state;
}