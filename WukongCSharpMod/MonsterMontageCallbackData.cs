using System;
using System.Text;
using b1;
using Photon.Client;

namespace WukongCSharpMod
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

        public static short Serialize(StreamBuffer outstream, object customobject)
        {
            var data = (MonsterMontageCallbackData)customobject;
            
            var guidBytes = Encoding.UTF8.GetBytes(data.MonsterGuid);
            var guidLength = (short)guidBytes.Length;
            
            outstream.Write(BitConverter.GetBytes(guidLength), 0, 2);
            outstream.Write(guidBytes, 0, guidBytes.Length);
            
            outstream.WriteByte((byte)data.Reason);
            outstream.WriteByte((byte)data.State);

            var nameBytes = Encoding.UTF8.GetBytes(data.MontagePath);
            var nameLength = (short)nameBytes.Length;

            outstream.Write(BitConverter.GetBytes(nameLength), 0, 2);
            outstream.Write(nameBytes, 0, nameBytes.Length);

            return (short)(2 + guidLength + 2 + 2 + nameLength);
        }

        public static object Deserialize(StreamBuffer instream, short length)
        {
            var guidLengthBytes = new byte[2];
            instream.Read(guidLengthBytes, 0, 2);
            var guidLength = BitConverter.ToInt16(guidLengthBytes, 0);
            
            var guidBytes = new byte[guidLength];
            instream.Read(guidBytes, 0, guidLength);
            var guid = Encoding.UTF8.GetString(guidBytes);

            var reason = (EMontageBindReason)instream.ReadByte();
            var state = (EMontageCallbackState)instream.ReadByte();

            var nameLengthBytes = new byte[2];
            instream.Read(nameLengthBytes, 0, 2);
            var nameLength = BitConverter.ToInt16(nameLengthBytes, 0);

            var nameBytes = new byte[nameLength];
            instream.Read(nameBytes, 0, nameLength);
            var name = Encoding.UTF8.GetString(nameBytes);

            return new MonsterMontageCallbackData(guid, reason, name, state);
        }
    }
}