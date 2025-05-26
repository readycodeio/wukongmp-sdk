using LiteNetLib.Utils;
using ReadyM.Relay.Common.ECS;

namespace WukongMp.Api
{
    public class FsmStateData(NetworkIdComponent netId, string fsmStateName)
    {
        public NetworkIdComponent NetId { get; } = netId;
        public string FsmStateName { get; } = fsmStateName;

        public static void Serialize(NetDataWriter outStream, object fsmStateData)
        {
            var spawnData = (FsmStateData)fsmStateData;
            outStream.Put(spawnData.NetId.Owner);
            outStream.Put(spawnData.NetId.Id);
            outStream.Put(spawnData.FsmStateName);
        }

        public static object Deserialize(NetDataReader inStream)
        {
            var owner = inStream.GetShort();
            var id = inStream.GetUInt();
            var name = inStream.GetString();
            return new FsmStateData(new NetworkIdComponent(owner, id), name);
        }
    }
}