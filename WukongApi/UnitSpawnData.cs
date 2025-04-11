using System;
using System.Text;
using Photon.Client;

namespace WukongApi
{
    public readonly struct UnitSpawnData(int id, string guid, string name, int teamId, float x, float y, float z)
    {
        public readonly int Id = id;
        public readonly string Guid = guid;
        public readonly string Name = name;
        public readonly int TeamId = teamId;
        public readonly float X = x;
        public readonly float Y = y;
        public readonly float Z = z;

        public static short Serialize(StreamBuffer outStream, object unitSpawnData)
        {
            var spawnData = (UnitSpawnData)unitSpawnData;

            var guidBytes = Encoding.UTF8.GetBytes(spawnData.Guid);
            var guidLength = (short)guidBytes.Length;

            var nameBytes = Encoding.UTF8.GetBytes(spawnData.Name);
            var nameLength = (short)nameBytes.Length;

            outStream.Write(BitConverter.GetBytes(spawnData.Id), 0, 4);
            outStream.Write(BitConverter.GetBytes(guidLength), 0, 2);
            outStream.Write(guidBytes, 0, guidBytes.Length);
            outStream.Write(BitConverter.GetBytes(nameLength), 0, 2);
            outStream.Write(nameBytes, 0, nameBytes.Length);
            outStream.Write(BitConverter.GetBytes(spawnData.TeamId), 0, 4);
            outStream.Write(BitConverter.GetBytes(spawnData.X), 0, 4);
            outStream.Write(BitConverter.GetBytes(spawnData.Y), 0, 4);
            outStream.Write(BitConverter.GetBytes(spawnData.Z), 0, 4);

            return (short)(4 + 2 + guidBytes.Length + 2 + nameBytes.Length + 12);
        }

        public static object Deserialize(StreamBuffer inStream, short length)
        {
            var intBytes = new byte[4];
            inStream.Read(intBytes, 0, 4);
            var id = BitConverter.ToInt32(intBytes, 0);

            var guidLengthBytes = new byte[2];
            inStream.Read(guidLengthBytes, 0, 2);
            var guidLength = BitConverter.ToInt16(guidLengthBytes, 0);

            var guidBytes = new byte[guidLength];
            inStream.Read(guidBytes, 0, guidLength);
            var guid = Encoding.UTF8.GetString(guidBytes);

            var nameLengthBytes = new byte[2];
            inStream.Read(nameLengthBytes, 0, 2);
            var nameLength = BitConverter.ToInt16(nameLengthBytes, 0);

            var nameBytes = new byte[nameLength];
            inStream.Read(nameBytes, 0, nameLength);
            var name = Encoding.UTF8.GetString(nameBytes);

            inStream.Read(intBytes, 0, 4);
            var teamId = BitConverter.ToInt32(intBytes, 0);

            var floatBytes = new byte[4];
            inStream.Read(floatBytes, 0, 4);
            var x = BitConverter.ToSingle(floatBytes, 0);
            inStream.Read(floatBytes, 0, 4);
            var y = BitConverter.ToSingle(floatBytes, 0);
            inStream.Read(floatBytes, 0, 4);
            var z = BitConverter.ToSingle(floatBytes, 0);

            return new UnitSpawnData(id, guid, name, teamId, x, y, z);
        }
    }
}