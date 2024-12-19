using Photon.Client;
using System;

namespace WukongMp.Common
{
    public readonly struct UnitSpawnData
    {
        public readonly byte Id;
        public readonly string Name;
        public readonly float X;
        public readonly float Y;
        public readonly float Z;

        public UnitSpawnData(byte id, string name, float x, float y, float z)
        {
            Id = id;
            Name = name;
            X = x;
            Y = y;
            Z = z;
        }

        public static short Serialize(StreamBuffer outstream, object unitSpawnData)
        {
            var spawnData = (UnitSpawnData)unitSpawnData;

            var nameBytes = System.Text.Encoding.UTF8.GetBytes(spawnData.Name);
            var nameLength = (short)nameBytes.Length;

            outstream.Write(BitConverter.GetBytes(spawnData.Id), 0, 1);
            outstream.Write(BitConverter.GetBytes(nameLength), 0, 2);
            outstream.Write(nameBytes, 0, nameBytes.Length);
            outstream.Write(BitConverter.GetBytes(spawnData.X), 0, 4);
            outstream.Write(BitConverter.GetBytes(spawnData.Y), 0, 4);
            outstream.Write(BitConverter.GetBytes(spawnData.Z), 0, 4);

            return (short)(1 + 2 + nameBytes.Length + 12);
        }

        public static object Deserialize(StreamBuffer instream, short length)
        {
            var id = instream.ReadByte();
            
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

            return new UnitSpawnData(id, name, x, y, z);
        }
    }
}