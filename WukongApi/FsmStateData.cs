using System;
using System.Text;
using Photon.Client;

namespace WukongApi
{
    public class FsmStateData(int characterId, string fsmStateName)
    {
        public int CharacterId { get; } = characterId;
        public string FsmStateName { get; } = fsmStateName;

        public static short Serialize(StreamBuffer outStream, object unitSpawnData)
        {
            var spawnData = (FsmStateData)unitSpawnData;

            var nameBytes = Encoding.UTF8.GetBytes(spawnData.FsmStateName);
            var nameLength = (short)nameBytes.Length;

            outStream.Write(BitConverter.GetBytes(spawnData.CharacterId), 0, 4);
            outStream.Write(BitConverter.GetBytes(nameLength), 0, 2);
            outStream.Write(nameBytes, 0, nameBytes.Length);

            return (short)(4 + 2 + nameBytes.Length);
        }

        public static object Deserialize(StreamBuffer inStream, short length)
        {
            var intBytes = new byte[4];
            inStream.Read(intBytes, 0, 4);
            var id = BitConverter.ToInt32(intBytes, 0);

            var nameLengthBytes = new byte[2];
            inStream.Read(nameLengthBytes, 0, 2);
            var nameLength = BitConverter.ToInt16(nameLengthBytes, 0);

            var nameBytes = new byte[nameLength];
            inStream.Read(nameBytes, 0, nameLength);
            var name = Encoding.UTF8.GetString(nameBytes);

            return new FsmStateData(id, name);
        }
    }
}