using LiteNetLib.Utils;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.Api.Multiplayer.Generators;
using ReadyM.Api.Serialization;

namespace WukongMp.Api.DTO;

[DeriveINetSerializable]
[DeriveJsonSerializable]
public partial struct TriggerImmobilizeData(NetworkId playerId, NetworkId target, bool greatSageTalentActiveBuff) : INetSerializable
{
    public NetworkId PlayerId = playerId;
    public NetworkId Target = target;
    public bool GreatSageTalentActiveBuff = greatSageTalentActiveBuff;
}
