using System;
using System.Text;
using Photon.Client;

namespace WukongCSharpMod
{
    public readonly struct UnitSpawnData
    {
        public readonly string Guid;
        public readonly string Name;
        public readonly float X;
        public readonly float Y;
        public readonly float Z;

        public UnitSpawnData(string guid, string name, float x, float y, float z)
        {
            Guid = guid;
            Name = name;
            X = x;
            Y = y;
            Z = z;
        }

        public static short Serialize(StreamBuffer outstream, object unitSpawnData)
        {
            var spawnData = (UnitSpawnData)unitSpawnData;
            
            var guidBytes = Encoding.UTF8.GetBytes(spawnData.Guid);
            var guidLength = (short)guidBytes.Length;

            var nameBytes = Encoding.UTF8.GetBytes(spawnData.Name);
            var nameLength = (short)nameBytes.Length;

            outstream.Write(BitConverter.GetBytes(guidLength), 0, 2);
            outstream.Write(guidBytes, 0, guidBytes.Length);
            outstream.Write(BitConverter.GetBytes(nameLength), 0, 2);
            outstream.Write(nameBytes, 0, nameBytes.Length);
            outstream.Write(BitConverter.GetBytes(spawnData.X), 0, 4);
            outstream.Write(BitConverter.GetBytes(spawnData.Y), 0, 4);
            outstream.Write(BitConverter.GetBytes(spawnData.Z), 0, 4);

            return (short)(2 + guidBytes.Length + 2 + nameBytes.Length + 12);
        }

        public static object Deserialize(StreamBuffer instream, short length)
        {
            var guidLengthBytes = new byte[2];
            instream.Read(guidLengthBytes, 0, 2);
            var guidLength = BitConverter.ToInt16(guidLengthBytes, 0);
            
            var guidBytes = new byte[guidLength];
            instream.Read(guidBytes, 0, guidLength);
            var guid = Encoding.UTF8.GetString(guidBytes);
            
            var nameLengthBytes = new byte[2];
            instream.Read(nameLengthBytes, 0, 2);
            var nameLength = BitConverter.ToInt16(nameLengthBytes, 0);

            var nameBytes = new byte[nameLength];
            instream.Read(nameBytes, 0, nameLength);
            var name = Encoding.UTF8.GetString(nameBytes);

            var floatBytes = new byte[4];
            instream.Read(floatBytes, 0, 4);
            var x = BitConverter.ToSingle(floatBytes, 0);
            instream.Read(floatBytes, 0, 4);
            var y = BitConverter.ToSingle(floatBytes, 0);
            instream.Read(floatBytes, 0, 4);
            var z = BitConverter.ToSingle(floatBytes, 0);

            return new UnitSpawnData(guid, name, x, y, z);
        }
    }
}