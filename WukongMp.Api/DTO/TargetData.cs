using LiteNetLib.Utils;
using ReadyM.Api.Multiplayer;
using ReadyM.Relay.Common.ECS;

namespace WukongMp.Api.DTO;

[DeriveINetSerializable]
public partial struct TargetData(NetworkIdComponent character, NetworkIdComponent target, bool clearTarget) : INetSerializable
{
    public NetworkIdComponent Character = character;
    public NetworkIdComponent Target = target;
    public bool ClearTarget = clearTarget;
}