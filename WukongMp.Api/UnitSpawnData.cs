using LiteNetLib.Utils;
using ReadyM.Api.Multiplayer;
using ReadyM.Api.Multiplayer.Extensions;

namespace WukongMp.Api
{
    public readonly struct UnitSpawnData(NetworkIdComponent id, string guid, string name, int teamId, float x, float y, float z)
    {
        public readonly NetworkIdComponent Id = id;
        public readonly string Guid = guid;
        public readonly string Name = name;
        public readonly int TeamId = teamId;
        public readonly float X = x;
        public readonly float Y = y;
        public readonly float Z = z;

        public static void Serialize(NetDataWriter outStream, object unitSpawnData)
        {
            var spawnData = (UnitSpawnData)unitSpawnData;
            outStream.Put(spawnData.Id);
            outStream.Put(spawnData.Guid);
            outStream.Put(spawnData.Name);
            outStream.Put(spawnData.TeamId);
            outStream.Put(spawnData.X);
            outStream.Put(spawnData.Y);
            outStream.Put(spawnData.Z);
        }

        public static object Deserialize(NetDataReader inStream)
        {
            var id = inStream.GetNetworkId();
            var guid = inStream.GetString();
            var name = inStream.GetString();
            var teamId = inStream.GetInt();
            var x = inStream.GetFloat();
            var y = inStream.GetFloat();
            var z = inStream.GetFloat();

            return new UnitSpawnData(id, guid, name, teamId, x, y, z);
        }
    }
}