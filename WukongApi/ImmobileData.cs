using System;
using Photon.Client;

namespace WukongApi
{
    public class ImmobilizeData(int playerId, int otherPlayerId, ImmobilizeActionType immobilizeActionType, bool greatSageTalentActiveBuff)
    {
        public int PlayerId { get; } = playerId;
        public int OtherPlayerId { get; } = otherPlayerId;
        public ImmobilizeActionType ImmobilizeActionType { get; } = immobilizeActionType;
        public bool GreatSageTalentActiveBuff { get; } = greatSageTalentActiveBuff;

        public static short Serialize(StreamBuffer outStream, object customObject)
        {
            var data = (ImmobilizeData)customObject;
            outStream.Write(BitConverter.GetBytes(data.PlayerId), 0, 4);
            outStream.Write(BitConverter.GetBytes(data.OtherPlayerId), 0, 4);
            outStream.WriteByte((byte)data.ImmobilizeActionType);
            outStream.Write(BitConverter.GetBytes(data.GreatSageTalentActiveBuff), 0, 1);

            return 10;
        }

        public static object Deserialize(StreamBuffer inStream, short length)
        {
            var intBytes = new byte[4];

            inStream.Read(intBytes, 0, 4);
            var playerId = BitConverter.ToInt32(intBytes, 0);

            inStream.Read(intBytes, 0, 4);
            var otherPlayerId = BitConverter.ToInt32(intBytes, 0);

            var inputActionType = (ImmobilizeActionType)inStream.ReadByte();

            var booleanBytes = new byte[1];
            inStream.Read(booleanBytes, 0, 1);
            var greatSageTalentActiveBuff = BitConverter.ToBoolean(booleanBytes, 0);

            return new ImmobilizeData(playerId, otherPlayerId, inputActionType, greatSageTalentActiveBuff);
        }
    }
}