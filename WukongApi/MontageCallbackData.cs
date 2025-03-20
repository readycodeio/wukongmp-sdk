using System;
using System.Text;
using b1;
using Photon.Client;

namespace WukongApi
{
    public class MontageCallbackData(EMontageBindReason reason, string montagePath, EMontageCallbackState state)
    {
        public EMontageBindReason Reason { get; } = reason;
        public string MontagePath { get; } = montagePath;
        public EMontageCallbackState State { get; } = state;

        public static short Serialize(StreamBuffer outStream, object customObject)
        {
            var data = (MontageCallbackData)customObject;
            outStream.WriteByte((byte)data.Reason);
            outStream.WriteByte((byte)data.State);

            var nameBytes = Encoding.UTF8.GetBytes(data.MontagePath);
            var nameLength = (short)nameBytes.Length;

            outStream.Write(BitConverter.GetBytes(nameLength), 0, 2);
            outStream.Write(nameBytes, 0, nameBytes.Length);

            return (short)(2 + 2 + nameLength);
        }

        public static object Deserialize(StreamBuffer inStream, short length)
        {
            var reason = (EMontageBindReason)inStream.ReadByte();
            var state = (EMontageCallbackState)inStream.ReadByte();

            var nameLengthBytes = new byte[2];
            inStream.Read(nameLengthBytes, 0, 2);
            var nameLength = BitConverter.ToInt16(nameLengthBytes, 0);

            var nameBytes = new byte[nameLength];
            inStream.Read(nameBytes, 0, nameLength);
            var name = Encoding.UTF8.GetString(nameBytes);

            return new MontageCallbackData(reason, name, state);
        }
    }
}