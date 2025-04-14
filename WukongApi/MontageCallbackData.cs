using System;
using System.Text;
using b1;
using Photon.Client;

namespace WukongApi
{
    public class MontageCallbackData(int id, bool compressed, string montagePath, float position, bool reset)
    {
        public int CharacterId { get; } = id;
        public bool Compressed { get; } = compressed;
        public string MontagePath { get; } = montagePath;
        public float Position { get; } = position;
        public bool Reset { get; } = reset;

        public static short Serialize(StreamBuffer outStream, object customObject)
        {
            var data = (MontageCallbackData)customObject;

            outStream.Write(BitConverter.GetBytes(data.CharacterId), 0, 4);

            outStream.WriteByte((byte)(data.Compressed ? 1 : 0));
            outStream.WriteByte((byte)(data.Reset ? 1 : 0));
            
            outStream.Write(BitConverter.GetBytes(data.Position), 0, 4);

            var nameBytes = Encoding.UTF8.GetBytes(data.MontagePath);
            var nameLength = (short)nameBytes.Length;

            outStream.Write(BitConverter.GetBytes(nameLength), 0, 2);
            outStream.Write(nameBytes, 0, nameBytes.Length);


            return (short)(4 + 1 + 1 + 4 + 2 + nameLength);
        }

        public static object Deserialize(StreamBuffer inStream, short length)
        {
            var fourBytesArray = new byte[4];
            inStream.Read(fourBytesArray, 0, 4);
            var id = BitConverter.ToInt32(fourBytesArray, 0);

            var compressed = inStream.ReadByte() == 1;
            var reset = inStream.ReadByte() == 1;
            
            inStream.Read(fourBytesArray, 0, 4);
            var offset = BitConverter.ToSingle(fourBytesArray, 0);

            var nameLengthBytes = new byte[2];
            inStream.Read(nameLengthBytes, 0, 2);
            var nameLength = BitConverter.ToInt16(nameLengthBytes, 0);

            var nameBytes = new byte[nameLength];
            inStream.Read(nameBytes, 0, nameLength);
            var name = Encoding.UTF8.GetString(nameBytes);

            return new MontageCallbackData(id, compressed, name, offset, reset);
        }
    }
}