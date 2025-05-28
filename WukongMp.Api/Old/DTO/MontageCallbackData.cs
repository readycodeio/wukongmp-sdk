using LiteNetLib.Utils;
using ReadyM.Relay.Common.ECS;

namespace WukongMp.Api.Old.DTO
{
    public struct MontageCallbackData(NetworkIdComponent netId, bool compressed, string montagePath, float position, bool reset) : INetSerializable
    {
        public NetworkIdComponent NetId = netId;
        public bool Compressed = compressed;
        public string MontagePath = montagePath;
        public float Position = position;
        public bool Reset = reset;

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(NetId);
            writer.Put(Compressed);
            writer.Put(MontagePath);
            writer.Put(Position);
            writer.Put(Reset);
        }

        public void Deserialize(NetDataReader reader)
        {
            NetId = reader.GetNetworkId();
            Compressed = reader.GetBool();
            MontagePath = reader.GetString();
            Position = reader.GetFloat();
            Reset = reader.GetBool();
        }
    }
}