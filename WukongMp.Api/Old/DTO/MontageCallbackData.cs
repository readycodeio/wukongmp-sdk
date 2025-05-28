using LiteNetLib.Utils;
using ReadyM.Relay.Common.ECS;

namespace WukongMp.Api.Old.DTO
{
    public class MontageCallbackData(NetworkIdComponent netId, bool compressed, string montagePath, float position, bool reset)
    {
        public NetworkIdComponent NetId { get; } = netId;
        public bool Compressed { get; } = compressed;
        public string MontagePath { get; } = montagePath;
        public float Position { get; } = position;
        public bool Reset { get; } = reset;

        public static void Serialize(NetDataWriter outStream, object customObject)
        {
            var data = (MontageCallbackData)customObject;
            outStream.Put(data.NetId.Owner);
            outStream.Put(data.NetId.Id);
            outStream.Put(data.Compressed);
            outStream.Put(data.MontagePath);
            outStream.Put(data.Position);
            outStream.Put(data.Reset);
        }

        public static object Deserialize(NetDataReader inStream)
        {
            var owner = inStream.GetShort();
            var id = inStream.GetUInt();
            var compressed = inStream.GetBool();
            var montagePath = inStream.GetString();
            var offset = inStream.GetFloat();
            var reset = inStream.GetBool();
            return new MontageCallbackData(new NetworkIdComponent(owner, id), compressed, montagePath, offset, reset);
        }
    }
}