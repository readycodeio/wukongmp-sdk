using LiteNetLib.Utils;

namespace WukongApi
{
    public readonly struct UnitSummonData(int summonerId, int id, string guid, string name, int teamId)
    {
        public readonly int SummonerId = summonerId;
        public readonly int Id = id;
        public readonly string Guid = guid;
        public readonly string Name = name;
        public readonly int TeamId = teamId;

        public static void Serialize(NetDataWriter outStream, object unitSpawnData)
        {
            var spawnData = (UnitSummonData)unitSpawnData;
            outStream.Put(spawnData.SummonerId);
            outStream.Put(spawnData.Id);
            outStream.Put(spawnData.Guid);
            outStream.Put(spawnData.Name);
            outStream.Put(spawnData.TeamId);
        }

        public static object Deserialize(NetDataReader inStream)
        {
            var summonerId = inStream.GetInt();
            var id = inStream.GetInt();
            var guid = inStream.GetString();
            var name = inStream.GetString();
            var teamId = inStream.GetInt();

            return new UnitSummonData(summonerId, id, guid, name, teamId);
        }
    }
}
