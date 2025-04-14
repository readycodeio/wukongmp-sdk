using b1;
using LiteNetLib.Utils;

namespace WukongApi
{
    public class StateTriggerData(int characterId, EBUStateTrigger trigger, float time, bool needForceUpdate)
    {
        public int CharacterId { get; } = characterId;
        public EBUStateTrigger Trigger { get; } = trigger;
        public float Time { get; } = time;
        public bool NeedForceUpdate { get; } = needForceUpdate;

        public static void Serialize(NetDataWriter outStream, object customObject)
        {
            var data = (StateTriggerData)customObject;
            outStream.Put(data.CharacterId);
            outStream.Put((byte)data.Trigger);
            outStream.Put(data.Time);
            outStream.Put(data.NeedForceUpdate);
        }

        public static object Deserialize(NetDataReader inStream)
        {
            var characterId = inStream.GetInt();
            var trigger = (EBUStateTrigger)inStream.GetByte();
            var time = inStream.GetFloat();
            var needForceUpdate = inStream.GetBool();
            return new StateTriggerData(characterId, trigger, time, needForceUpdate);
        }
    }
}
