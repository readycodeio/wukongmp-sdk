using b1;
using LiteNetLib.Utils;

namespace WukongApi
{
    public class MonsterMontageCallbackData(string monsterGuid, EMontageBindReason reason, string montagePath, EMontageCallbackState state)
    {
        public string MonsterGuid { get; } = monsterGuid;
        public EMontageBindReason Reason { get; } = reason;
        public string MontagePath { get; } = montagePath;
        public EMontageCallbackState State { get; } = state;

        public static void Serialize(NetDataWriter outStream, object customObject)
        {
            var data = (MonsterMontageCallbackData)customObject;
            outStream.Put((byte)data.Reason);
            outStream.Put((byte)data.State);
            outStream.Put(data.MonsterGuid);
            outStream.Put(data.MontagePath);
        }

        public static object Deserialize(NetDataReader inStream)
        {
            var reason = (EMontageBindReason)inStream.GetByte();
            var state = (EMontageCallbackState)inStream.GetByte();
            var guid = inStream.GetString();
            var name = inStream.GetString();
            return new MonsterMontageCallbackData(guid, reason, name, state);
        }
    }
}