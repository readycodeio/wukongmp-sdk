using b1;
using LiteNetLib.Utils;
using ReadyM.Api.Multiplayer.ECS.Values;
using ReadyM.Api.Multiplayer.Generators;
using ReadyM.Api.Serialization;

namespace WukongMp.Api.DTO;

[DeriveINetSerializable]
[DeriveJsonSerializable]
public partial struct PlayBaneEffectData(NetworkId netId, EAbnormalStateType stateType, EAbnromalDispActionType actionType) : INetSerializable
{
    public NetworkId NetId = netId;
    public EAbnormalStateType StateType = stateType;
    public EAbnromalDispActionType ActionType = actionType;
}