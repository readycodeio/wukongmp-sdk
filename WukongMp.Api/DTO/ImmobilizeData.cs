using LiteNetLib.Utils;
using ReadyM.Api.Multiplayer.ECS.Values;
using ReadyM.Api.Multiplayer.Generators;
using ReadyM.Api.Serialization;

namespace WukongMp.Api.DTO;

[DeriveINetSerializable]
[DeriveJsonSerializable]
internal partial struct TriggerImmobilizeData(
    NetworkId netId, 
    NetworkId targetNetId, 
    bool greatSageTalentActiveBuff) : INetSerializable
{
    public NetworkId NetId = netId;
    public NetworkId TargetNetId = targetNetId;
    public bool GreatSageTalentActiveBuff = greatSageTalentActiveBuff;
}
