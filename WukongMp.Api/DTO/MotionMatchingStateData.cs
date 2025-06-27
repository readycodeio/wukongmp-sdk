using b1;
using LiteNetLib.Utils;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.Api.Serialization;
using ReadyM.Relay.Client;

namespace WukongMp.Api.DTO;

[DeriveINetSerializable]
[DeriveJsonSerializable]
public partial struct MotionMatchingStateData(NetworkIdComponent netId, EState_MM state) : INetSerializable
{
    public NetworkIdComponent NetId = netId;
    public EState_MM State = state;
}
