using Photon.Client;
using System;

namespace WukongMp.Common
{
    public readonly struct UnitSpawnData
    {
        public readonly string Name;
        public readonly float X;
        public readonly float Y;
        public readonly float Z;

        public UnitSpawnData(string name, float x, float y, float z)
        {
            Name = name;
            X = x;
            Y = y;
            Z = z;
        }

        public static byte[] Serialize(object unitSpawnData)
        {
            var spawnData = (UnitSpawnData)unitSpawnData;

            var nameBytes = System.Text.Encoding.UTF8.GetBytes(spawnData.Name);
            var nameLength = (short)nameBytes.Length;

            var buffer = new byte[2 + nameLength + 12]; // 2 bytes for name length, name bytes, and 12 bytes for floats

            BitConverter.GetBytes(nameLength).CopyTo(buffer, 0);
            nameBytes.CopyTo(buffer, 2);
            BitConverter.GetBytes(spawnData.X).CopyTo(buffer, 2 + nameLength);
            BitConverter.GetBytes(spawnData.Y).CopyTo(buffer, 6 + nameLength);
            BitConverter.GetBytes(spawnData.Z).CopyTo(buffer, 10 + nameLength);

            return buffer;
        }

        public static object Deserialize(byte[] data)
        {
            var nameLength = BitConverter.ToInt16(data, 0);
            var name = System.Text.Encoding.UTF8.GetString(data, 2, nameLength);

            var x = BitConverter.ToSingle(data, 2 + nameLength);
            var y = BitConverter.ToSingle(data, 6 + nameLength);
            var z = BitConverter.ToSingle(data, 10 + nameLength);

            return new UnitSpawnData(name, x, y, z);
        }

        public static short Serialize(StreamBuffer outstream, object unitSpawnData)
        {
            var spawnData = (UnitSpawnData)unitSpawnData;

            var nameBytes = System.Text.Encoding.UTF8.GetBytes(spawnData.Name);
            var nameLength = (short)nameBytes.Length;

            outstream.Write(BitConverter.GetBytes(nameLength), 0, 2);
            outstream.Write(nameBytes, 0, nameBytes.Length);
            outstream.Write(BitConverter.GetBytes(spawnData.X), 0, 4);
            outstream.Write(BitConverter.GetBytes(spawnData.Y), 0, 4);
            outstream.Write(BitConverter.GetBytes(spawnData.Z), 0, 4);

            return (short)(2 + nameBytes.Length + 12);
        }

        public static object Deserialize(StreamBuffer instream, short length)
        {
            var nameLengthBytes = new byte[2];
            instream.Read(nameLengthBytes, 0, 2);
            var nameLength = BitConverter.ToInt16(nameLengthBytes, 0);

            var nameBytes = new byte[nameLength];
            instream.Read(nameBytes, 0, nameLength);
            var name = System.Text.Encoding.UTF8.GetString(nameBytes);

            var floatBytes = new byte[4];
            instream.Read(floatBytes, 0, 4);
            var x = BitConverter.ToSingle(floatBytes, 0);
            instream.Read(floatBytes, 0, 4);
            var y = BitConverter.ToSingle(floatBytes, 0);
            instream.Read(floatBytes, 0, 4);
            var z = BitConverter.ToSingle(floatBytes, 0);

            return new UnitSpawnData(name, x, y, z);
        }
    }

}
