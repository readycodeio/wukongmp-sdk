using Photon.Client;
using System;

namespace WukongApi.Timer
{
    public class TimerData
    {
        public TimerKind TimerKind{ get; }
        public long TimerEndTicks { get; }

        public TimerData(TimerKind timerKind, long timerEndTicks)
        {
            TimerKind = timerKind;
            TimerEndTicks = timerEndTicks;
        }

        public static short Serialize(StreamBuffer outStream, object customObject)
        {
            var data = (TimerData)customObject;
            outStream.WriteByte((byte)data.TimerKind);
            outStream.Write(BitConverter.GetBytes(data.TimerEndTicks), 0, 8);

            return 9;
        }

        public static object Deserialize(StreamBuffer inStream, short length)
        {
            var timerKind = (TimerKind)inStream.ReadByte();

            var longBytes = new byte[8];
            inStream.Read(longBytes, 0, 8);
            var timerEndTicks = BitConverter.ToInt32(longBytes, 0);

            return new TimerData(timerKind, timerEndTicks);
        }
    }
}
