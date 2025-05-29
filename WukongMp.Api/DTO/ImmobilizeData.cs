using ReadyM.Api.Multiplayer;
using ReadyM.Relay.Common.ECS;
using WukongMp.Api.Old.Enums;

namespace WukongMp.Api.DTO;

[DeriveINetSerializable]
public partial struct ImmobilizeData(NetworkIdComponent playerId, NetworkIdComponent otherPlayerId, ImmobilizeActionType immobilizeActionType, bool greatSageTalentActiveBuff)
{
    public NetworkIdComponent PlayerId = playerId;
    public NetworkIdComponent OtherPlayerId = otherPlayerId;
    public ImmobilizeActionType ImmobilizeActionType = immobilizeActionType;
    public bool GreatSageTalentActiveBuff = greatSageTalentActiveBuff;
}