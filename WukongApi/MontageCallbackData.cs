using LiteNetLib.Utils;

namespace WukongApi
{
    public class MontageCallbackData(int id, bool compressed, string montagePath, float position, bool reset)
    {
        public int CharacterId { get; } = id;
        public bool Compressed { get; } = compressed;
        public string MontagePath { get; } = montagePath;
        public float Position { get; } = position;
        public bool Reset { get; } = reset;

        public static void Serialize(NetDataWriter outStream, object customObject)
        {
            var data = (MontageCallbackData)customObject;
            outStream.Put(data.CharacterId);
            outStream.Put(data.Compressed);
            outStream.Put(data.MontagePath);
            outStream.Put(data.Position);
            outStream.Put(data.Reset);
        }

        public static object Deserialize(NetDataReader inStream)
        {
            var id = inStream.GetInt();
            var compressed = inStream.GetBool();
            var montagePath = inStream.GetString();
            var offset = inStream.GetFloat();
            var reset = inStream.GetBool();
            return new MontageCallbackData(id, compressed, montagePath, offset, reset);
        }
    }
}