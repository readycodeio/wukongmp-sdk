using System;
using System.Text;
using b1;
using Photon.Client;

namespace WukongApi
{
    public class MontageCallbackData(string montagePath, float position)
    {
        public string MontagePath { get; } = montagePath;
        public float Position { get; } = position;

        public static short Serialize(StreamBuffer outStream, object customObject)
        {
            var data = (MontageCallbackData)customObject;
            outStream.Write(BitConverter.GetBytes(data.Position), 0, 4);

            var nameBytes = Encoding.UTF8.GetBytes(data.MontagePath);
            var nameLength = (short)nameBytes.Length;

            outStream.Write(BitConverter.GetBytes(nameLength), 0, 2);
            outStream.Write(nameBytes, 0, nameBytes.Length);

            return (short)(4 + nameLength);
        }

        public static object Deserialize(StreamBuffer inStream, short length)
        {
            var offsetBytes = new byte[4];
            inStream.Read(offsetBytes, 0, 4);
            var offset = BitConverter.ToSingle(offsetBytes, 0);

            var nameLengthBytes = new byte[2];
            inStream.Read(nameLengthBytes, 0, 2);
            var nameLength = BitConverter.ToInt16(nameLengthBytes, 0);

            var nameBytes = new byte[nameLength];
            inStream.Read(nameBytes, 0, nameLength);
            var name = Encoding.UTF8.GetString(nameBytes);

            return new MontageCallbackData(name, offset);
        }
    }
}