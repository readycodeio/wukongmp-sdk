using b1;
using LiteNetLib.Utils;
using ReadyM.Relay.Common.ECS;

namespace WukongMp.Api.Old.DTO
{
    public struct StateTriggerData(NetworkIdComponent netId, EBUStateTrigger trigger, float time, bool needForceUpdate)
    {
        public NetworkIdComponent NetId { get; } = netId;
        public EBUStateTrigger Trigger { get; } = trigger;
        public float Time { get; } = time;
        public bool NeedForceUpdate { get; } = needForceUpdate;

        public static void Serialize(NetDataWriter outStream, object customObject)
        {
            var data = (StateTriggerData)customObject;
            outStream.Put(data.NetId.Owner);
            outStream.Put(data.NetId.Id);
            outStream.Put((byte)data.Trigger);
            outStream.Put(data.Time);
            outStream.Put(data.NeedForceUpdate);
        }

        public static object Deserialize(NetDataReader inStream)
        {
            var owner = inStream.GetShort();
            var id = inStream.GetUInt();
            var trigger = (EBUStateTrigger)inStream.GetByte();
            var time = inStream.GetFloat();
            var needForceUpdate = inStream.GetBool();
            return new StateTriggerData(new NetworkIdComponent(owner, id), trigger, time, needForceUpdate);
        }
    }
}
