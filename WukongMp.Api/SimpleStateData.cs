using b1;
using LiteNetLib.Utils;
using ReadyM.Relay.Common.ECS;

namespace WukongMp.Api
{
    public class SimpleStateData(NetworkIdComponent netId, EBGUSimpleState simpleState, bool isRemove)
    {
        public NetworkIdComponent NetId { get; } = netId;
        public EBGUSimpleState SimpleState { get; } = simpleState;
        public bool IsRemove { get; } = isRemove;

        public static void Serialize(NetDataWriter outStream, object customObject)
        {
            var data = (SimpleStateData)customObject;
            outStream.Put(data.NetId.Owner);
            outStream.Put(data.NetId.Id);
            outStream.Put((byte)data.SimpleState);
            outStream.Put(data.IsRemove);
        }

        public static object Deserialize(NetDataReader inStream)
        {
            var owner = inStream.GetShort();
            var id = inStream.GetUInt();
            var simpleState = (EBGUSimpleState)inStream.GetByte();
            var isRemove = inStream.GetBool();
            return new SimpleStateData(new NetworkIdComponent(owner, id), simpleState, isRemove);
        }
    }
}