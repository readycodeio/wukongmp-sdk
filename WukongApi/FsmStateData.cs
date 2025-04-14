using LiteNetLib.Utils;

namespace WukongApi
{
    public class FsmStateData(int characterId, string fsmStateName)
    {
        public int CharacterId { get; } = characterId;
        public string FsmStateName { get; } = fsmStateName;

        public static void Serialize(NetDataWriter outStream, object unitSpawnData)
        {
            var spawnData = (FsmStateData)unitSpawnData;
            outStream.Put(spawnData.CharacterId);
            outStream.Put(spawnData.FsmStateName);
        }

        public static object Deserialize(NetDataReader inStream)
        {
            var id = inStream.GetInt();
            var name = inStream.GetString();
            return new FsmStateData(id, name);
        }
    }
}