using b1;
using LiteNetLib.Utils;

namespace WukongApi
{
    public class StateTriggerData(int entityId, EBUStateTrigger trigger, float time, bool needForceUpdate)
    {
        public int EntityId { get; } = entityId;
        public EBUStateTrigger Trigger { get; } = trigger;
        public float Time { get; } = time;
        public bool NeedForceUpdate { get; } = needForceUpdate;

        public static void Serialize(NetDataWriter outStream, object customObject)
        {
            var data = (StateTriggerData)customObject;
            outStream.Put(data.EntityId);
            outStream.Put((byte)data.Trigger);
            outStream.Put(data.Time);
            outStream.Put(data.NeedForceUpdate);
        }

        public static object Deserialize(NetDataReader inStream)
        {
            var entityId = inStream.GetInt();
            var trigger = (EBUStateTrigger)inStream.GetByte();
            var time = inStream.GetFloat();
            var needForceUpdate = inStream.GetBool();
            return new StateTriggerData(entityId, trigger, time, needForceUpdate);
        }
    }
}
