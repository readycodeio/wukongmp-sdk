using System;
using System.Text;
using b1;
using Photon.Client;

namespace WukongApi
{
    public class MonsterMontageCallbackData
    {
        public string MonsterGuid { get; }
        public EMontageBindReason Reason { get; }
        public string MontagePath { get; }
        public EMontageCallbackState State { get; }

        public MonsterMontageCallbackData(string monsterGuid, EMontageBindReason reason, string montagePath, EMontageCallbackState state)
        {
            MonsterGuid = monsterGuid;
            Reason = reason;
            MontagePath = montagePath;
            State = state;
        }

        public static short Serialize(StreamBuffer outStream, object customObject)
        {
            var data = (MonsterMontageCallbackData)customObject;

            var guidBytes = Encoding.UTF8.GetBytes(data.MonsterGuid);
            var guidLength = (short)guidBytes.Length;

            outStream.Write(BitConverter.GetBytes(guidLength), 0, 2);
            outStream.Write(guidBytes, 0, guidBytes.Length);

            outStream.WriteByte((byte)data.Reason);
            outStream.WriteByte((byte)data.State);

            var nameBytes = Encoding.UTF8.GetBytes(data.MontagePath);
            var nameLength = (short)nameBytes.Length;

            outStream.Write(BitConverter.GetBytes(nameLength), 0, 2);
            outStream.Write(nameBytes, 0, nameBytes.Length);

            return (short)(2 + guidLength + 2 + 2 + nameLength);
        }

        public static object Deserialize(StreamBuffer inStream, short length)
        {
            var guidLengthBytes = new byte[2];
            inStream.Read(guidLengthBytes, 0, 2);
            var guidLength = BitConverter.ToInt16(guidLengthBytes, 0);

            var guidBytes = new byte[guidLength];
            inStream.Read(guidBytes, 0, guidLength);
            var guid = Encoding.UTF8.GetString(guidBytes);

            var reason = (EMontageBindReason)inStream.ReadByte();
            var state = (EMontageCallbackState)inStream.ReadByte();

            var nameLengthBytes = new byte[2];
            inStream.Read(nameLengthBytes, 0, 2);
            var nameLength = BitConverter.ToInt16(nameLengthBytes, 0);

            var nameBytes = new byte[nameLength];
            inStream.Read(nameBytes, 0, nameLength);
            var name = Encoding.UTF8.GetString(nameBytes);

            return new MonsterMontageCallbackData(guid, reason, name, state);
        }
    }
}