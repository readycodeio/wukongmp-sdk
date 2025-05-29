using LiteNetLib.Utils;
using ReadyM.Api.Multiplayer;
using ReadyM.Relay.Common.ECS;

namespace WukongMp.Api.DTO;

[DeriveINetSerializable]
public partial struct TriggerImmobilizeData(NetworkIdComponent playerId, NetworkIdComponent target, bool greatSageTalentActiveBuff) : INetSerializable
{
    public NetworkIdComponent PlayerId = playerId;
    public NetworkIdComponent Target = target;
    public bool GreatSageTalentActiveBuff = greatSageTalentActiveBuff;
}