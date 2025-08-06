using b1;
using LiteNetLib.Utils;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.Api.Multiplayer.Generators;
using ReadyM.Api.Serialization;

namespace WukongMp.Api.DTO;

[DeriveINetSerializable]
[DeriveJsonSerializable]
public partial struct SimpleStateData(NetworkId netId, EBGUSimpleState simpleState, bool isRemove) : INetSerializable
{
    public NetworkId NetId = netId;
    public EBGUSimpleState SimpleState = simpleState;
    public bool IsRemove = isRemove;
}
