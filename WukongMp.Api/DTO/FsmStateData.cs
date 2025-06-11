using LiteNetLib.Utils;
using ReadyM.Api.Multiplayer;
using ReadyM.Relay.Common.ECS;

namespace WukongMp.Api.DTO;

[DeriveINetSerializable]
public partial struct FsmStateData(NetworkIdComponent netId, string fsmStateName) : INetSerializable
{
    public NetworkIdComponent NetId= netId;
    public string FsmStateName = fsmStateName;
}