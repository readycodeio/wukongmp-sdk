using LiteNetLib.Utils;
using ReadyM.Relay.Common.ECS;
using WukongMp.Api.Old.Enums;

namespace WukongMp.Api.DTO;

public struct ImmobilizeData(NetworkIdComponent playerId, NetworkIdComponent otherPlayerId, ImmobilizeActionType immobilizeActionType, bool greatSageTalentActiveBuff) : INetSerializable
{
    public NetworkIdComponent PlayerId = playerId;
    public NetworkIdComponent OtherPlayerId = otherPlayerId;
    public ImmobilizeActionType ImmobilizeActionType = immobilizeActionType;
    public bool GreatSageTalentActiveBuff = greatSageTalentActiveBuff;

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(PlayerId);
        writer.Put(OtherPlayerId);
        writer.Put((byte)ImmobilizeActionType);
        writer.Put(GreatSageTalentActiveBuff);
    }

    public void Deserialize(NetDataReader reader)
    {
        PlayerId = reader.GetNetworkId();
        OtherPlayerId = reader.GetNetworkId();
        ImmobilizeActionType = (ImmobilizeActionType)reader.GetByte();
        GreatSageTalentActiveBuff = reader.GetBool();
    }
}