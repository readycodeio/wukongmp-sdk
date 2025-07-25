using b1;
using LiteNetLib.Utils;
using ReadyM.Relay.Client;
using ReadyM.Relay.Common.ECS;
using ReadyM.Relay.Common.Serialization;

namespace WukongMp.Api.DTO;

[DeriveINetSerializable]
[DeriveJsonSerializable]
public partial struct MotionMatchingStateData(NetworkIdComponent netId, EState_MM state) : INetSerializable
{
    public NetworkIdComponent NetId = netId;
    public EState_MM State = state;
}
