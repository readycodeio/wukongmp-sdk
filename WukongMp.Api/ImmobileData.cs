using LiteNetLib.Utils;
using ReadyM.Api.Multiplayer;
using ReadyM.Api.Multiplayer.Extensions;

namespace WukongMp.Api
{
    public class ImmobilizeData(NetworkIdComponent playerId, NetworkIdComponent otherPlayerId, ImmobilizeActionType immobilizeActionType, bool greatSageTalentActiveBuff)
    {
        public NetworkIdComponent PlayerId { get; } = playerId;
        public NetworkIdComponent OtherPlayerId { get; } = otherPlayerId;
        public ImmobilizeActionType ImmobilizeActionType { get; } = immobilizeActionType;
        public bool GreatSageTalentActiveBuff { get; } = greatSageTalentActiveBuff;

        public static void Serialize(NetDataWriter outStream, object customObject)
        {
            var data = (ImmobilizeData)customObject;
            outStream.Put(data.PlayerId);
            outStream.Put(data.OtherPlayerId);
            outStream.Put((byte)data.ImmobilizeActionType);
            outStream.Put(data.GreatSageTalentActiveBuff);
        }

        public static object Deserialize(NetDataReader inStream)
        {
            var playerId = inStream.GetNetworkId();
            var otherPlayerId = inStream.GetNetworkId();
            var inputActionType = (ImmobilizeActionType)inStream.GetByte();
            var greatSageTalentActiveBuff = inStream.GetBool();

            return new ImmobilizeData(playerId, otherPlayerId, inputActionType, greatSageTalentActiveBuff);
        }
    }
}