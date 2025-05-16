using LiteNetLib.Utils;

namespace WukongMp.Api.DTO
{
    public readonly struct UnitSpawnRequestData(string unitName, int count, int teamId)
    {
        public readonly string UnitName = unitName;
        public readonly int Count = count;
        public readonly int TeamId = teamId;

        public static void Serialize(NetDataWriter outStream, object unitSpawnData)
        {
            var spawnData = (UnitSpawnRequestData)unitSpawnData;
            outStream.Put(spawnData.UnitName);
            outStream.Put(spawnData.Count);
            outStream.Put(spawnData.TeamId);
        }

        public static object Deserialize(NetDataReader inStream)
        {
            var unitName = inStream.GetString();
            var count = inStream.GetInt();
            var teamId = inStream.GetInt();

            return new UnitSpawnRequestData(unitName, count, teamId);
        }
    }
}
