using LiteNetLib.Utils;

namespace WukongApi
{
    public class ImmobilizeData(int playerId, int otherPlayerId, ImmobilizeActionType immobilizeActionType, bool greatSageTalentActiveBuff)
    {
        public int PlayerId { get; } = playerId;
        public int OtherPlayerId { get; } = otherPlayerId;
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
            var playerId = inStream.GetInt();
            var otherPlayerId = inStream.GetInt();
            var inputActionType = (ImmobilizeActionType)inStream.GetByte();
            var greatSageTalentActiveBuff = inStream.GetBool();

            return new ImmobilizeData(playerId, otherPlayerId, inputActionType, greatSageTalentActiveBuff);
        }
    }
}