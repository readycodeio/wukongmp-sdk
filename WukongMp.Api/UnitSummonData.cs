using LiteNetLib.Utils;
using ReadyM.Api.Multiplayer;
using ReadyM.Api.Multiplayer.Extensions;

namespace WukongMp.Api
{
    public readonly struct UnitSummonData(NetworkIdComponent summonerId, NetworkIdComponent summonId, string guid, string name, int teamId)
    {
        public readonly NetworkIdComponent SummonerId = summonerId;
        public readonly NetworkIdComponent SummonId = summonId;
        public readonly string Guid = guid;
        public readonly string Name = name;
        public readonly int TeamId = teamId;

        public static void Serialize(NetDataWriter outStream, object unitSpawnData)
        {
            var spawnData = (UnitSummonData)unitSpawnData;
            outStream.Put(spawnData.SummonerId);
            outStream.Put(spawnData.SummonId);
            outStream.Put(spawnData.Guid);
            outStream.Put(spawnData.Name);
            outStream.Put(spawnData.TeamId);
        }

        public static object Deserialize(NetDataReader inStream)
        {
            var summonerId = inStream.GetNetworkId();
            var id = inStream.GetNetworkId();
            var guid = inStream.GetString();
            var name = inStream.GetString();
            var teamId = inStream.GetInt();

            return new UnitSummonData(summonerId, id, guid, name, teamId);
        }
    }
}
