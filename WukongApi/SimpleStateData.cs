using System;
using b1;
using Photon.Client;

namespace WukongApi
{
    public class SimpleStateData(int characterId, EBGUSimpleState simpleState, bool isRemove)
    {
        public int CharacterId { get; } = characterId;
        public EBGUSimpleState SimpleState { get; } = simpleState;
        public bool IsRemove { get; } = isRemove;

        public static short Serialize(StreamBuffer outStream, object customObject)
        {
            var data = (SimpleStateData)customObject;
            outStream.Write(BitConverter.GetBytes(data.CharacterId), 0, 4);
            outStream.WriteByte((byte)data.SimpleState);
            outStream.Write(BitConverter.GetBytes(data.IsRemove), 0, 1);

            return 6;
        }

        public static object Deserialize(StreamBuffer inStream, short length)
        {
            var bytes = new byte[4];
            inStream.Read(bytes, 0, 4);
            var characterId = BitConverter.ToInt32(bytes, 0);

            var simpleState = (EBGUSimpleState)inStream.ReadByte();

            var booleanBytes = new byte[1];
            inStream.Read(booleanBytes, 0, 1);
            var isRemove = BitConverter.ToBoolean(booleanBytes, 0);

            return new SimpleStateData(characterId, simpleState, isRemove);
        }
    }
}
