using System;
using System.Text;
using b1;
using Photon.Client;

namespace WukongCSharpMod
{
    public class MonsterMontageCallbackData
    {
        public int MonsterId { get; }
        public EMontageBindReason Reason { get; }
        public string MontagePath { get; }
        public EMontageCallbackState State { get; }

        public MonsterMontageCallbackData(int monsterId, EMontageBindReason reason, string montagePath, EMontageCallbackState state)
        {
            MonsterId = monsterId;
            Reason = reason;
            MontagePath = montagePath;
            State = state;
        }

        public static short Serialize(StreamBuffer outstream, object customobject)
        {
            var data = (MonsterMontageCallbackData)customobject;
            outstream.Write(BitConverter.GetBytes(data.MonsterId), 0, 4);
            outstream.WriteByte((byte)data.Reason);
            outstream.WriteByte((byte)data.State);

            var nameBytes = Encoding.UTF8.GetBytes(data.MontagePath);
            var nameLength = (short)nameBytes.Length;

            outstream.Write(BitConverter.GetBytes(nameLength), 0, 2);
            outstream.Write(nameBytes, 0, nameBytes.Length);

            return (short)(4 + 2 + 2 + nameLength);
        }

        public static object Deserialize(StreamBuffer instream, short length)
        {
            var intBytes = new byte[4];
            instream.Read(intBytes, 0, 4);
            var monsterid = BitConverter.ToInt32(intBytes, 0);

            var reason = (EMontageBindReason)instream.ReadByte();
            var state = (EMontageCallbackState)instream.ReadByte();

            var nameLengthBytes = new byte[2];
            instream.Read(nameLengthBytes, 0, 2);
            var nameLength = BitConverter.ToInt16(nameLengthBytes, 0);

            var nameBytes = new byte[nameLength];
            instream.Read(nameBytes, 0, nameLength);
            var name = Encoding.UTF8.GetString(nameBytes);

            return new MonsterMontageCallbackData(monsterid, reason, name, state);
        }
    }
}