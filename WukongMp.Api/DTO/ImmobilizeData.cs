using LiteNetLib.Utils;
using ReadyM.Relay.Client;
using ReadyM.Relay.Common.ECS;
using ReadyM.Relay.Common.Serialization;

namespace WukongMp.Api.DTO;

[DeriveINetSerializable]
[DeriveJsonSerializable]
public partial struct TriggerImmobilizeData(NetworkIdComponent playerId, NetworkIdComponent target, bool greatSageTalentActiveBuff) : INetSerializable
{
    public NetworkIdComponent PlayerId = playerId;
    public NetworkIdComponent Target = target;
    public bool GreatSageTalentActiveBuff = greatSageTalentActiveBuff;
}
