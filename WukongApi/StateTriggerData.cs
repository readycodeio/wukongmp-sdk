using System;
using b1;
using Photon.Client;

namespace WukongApi
{
    public class StateTriggerData(int characterId, EBUStateTrigger trigger, float time, bool needForceUpdate)
    {
        public int CharacterId { get; } = characterId;
        public EBUStateTrigger Trigger { get; } = trigger;
        public float Time { get; } = time;
        public bool NeedForceUpdate { get; } = needForceUpdate;

        public static short Serialize(StreamBuffer outStream, object customObject)
        {
            var data = (StateTriggerData)customObject;
            outStream.Write(BitConverter.GetBytes(data.CharacterId), 0, 4);
            outStream.WriteByte((byte)data.Trigger);
            outStream.Write(BitConverter.GetBytes(data.Time), 0, 4);
            outStream.Write(BitConverter.GetBytes(data.NeedForceUpdate), 0, 1);

            return 10;
        }

        public static object Deserialize(StreamBuffer inStream, short length)
        {
            var bytes = new byte[4];

            inStream.Read(bytes, 0, 4);
            var characterId = BitConverter.ToInt32(bytes, 0);

            var trigger = (EBUStateTrigger)inStream.ReadByte();

            inStream.Read(bytes, 0, 4);
            var time = BitConverter.ToSingle(bytes, 0);

            var booleanBytes = new byte[1];
            inStream.Read(booleanBytes, 0, 1);
            var needForceUpdate = BitConverter.ToBoolean(booleanBytes, 0);

            return new StateTriggerData(characterId, trigger, time, needForceUpdate);
        }
    }
}
