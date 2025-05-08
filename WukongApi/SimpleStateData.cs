using b1;
using LiteNetLib.Utils;
using ReadyM.Relay.Common.ECS;
using ReadyM.Relay.Common.Protocol;

namespace WukongApi
{
    public class SimpleStateData(int characterId, EBGUSimpleState simpleState, bool isRemove)
    {
        public int CharacterId { get; } = characterId;
        public EBGUSimpleState SimpleState { get; } = simpleState;
        public bool IsRemove { get; } = isRemove;

        public static void Serialize(NetDataWriter outStream, object customObject)
        {
            var data = (SimpleStateData)customObject;
            outStream.Put(data.CharacterId);
            outStream.Put((byte)data.SimpleState);
            outStream.Put(data.IsRemove);
        }

        public static object Deserialize(NetDataReader inStream)
        {
            var characterId = inStream.GetInt();
            var simpleState = (EBGUSimpleState)inStream.GetByte();
            var isRemove = inStream.GetBool();
            return new SimpleStateData(characterId, simpleState, isRemove);
        }
    }
}