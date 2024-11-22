using System;
using Photon.Client;

namespace WukongMp.Common
{
    public readonly struct KeyPress
    {
        public readonly ConsoleKey Key;
        public readonly KeyState State;

        public KeyPress(ConsoleKey key, KeyState state)
        {
            Key = key;
            State = state;
        }

        public static object Deserialize(byte[] data)
        {
            return new KeyPress((ConsoleKey)data[0], (KeyState)data[1]);
        }

        public static byte[] Serialize(object keyPress)
        {
            var c = (KeyPress)keyPress;
            return new[] { (byte)c.Key, (byte)c.State };
        }

        public static short Serialize(StreamBuffer outstream, object customobject)
        {
            var c = (KeyPress)customobject;
            outstream.WriteByte((byte)c.Key);
            outstream.WriteByte((byte)c.State);
            return 2;
        }

        public static object Deserialize(StreamBuffer instream, short length)
        {
            var key = (ConsoleKey)instream.ReadByte();
            var state = (KeyState)instream.ReadByte();
            return new KeyPress(key, state);
        }
    }
}