using b1;
using LiteNetLib.Utils;
using ReadyM.Api.Multiplayer.ECS.Values;
using ReadyM.Api.Multiplayer.Generators;
using ReadyM.Api.Serialization;

namespace WukongMp.Api.DTO;

[DeriveINetSerializable]
[DeriveJsonSerializable]
public partial struct MotionMatchingStateData(NetworkId netId, EState_MM state) : INetSerializable
{
    public NetworkId NetId = netId;
    public EState_MM State = state;
}
