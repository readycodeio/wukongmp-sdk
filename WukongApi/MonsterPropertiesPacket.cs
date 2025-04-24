using System.Collections.Generic;
using LiteNetLib.Utils;

namespace WukongApi;

public readonly struct MonsterPropertiesPacket(int id, Dictionary<object, object> data)
{
    public int Id { get; } = id;
    public Dictionary<object, object> Data { get; } = data;
    
    public static void Serialize(NetDataWriter outStream, object customObject)
    {
        var data = (MonsterPropertiesPacket)customObject;
        outStream.Put(data.Id);
        WukongMP.Instance.Client.RelayClient.SerializeObject(outStream, data.Data);
    }

    public static object Deserialize(NetDataReader inStream)
    {
        var id = inStream.GetInt();
        var data = WukongMP.Instance.Client.RelayClient.DeserializeObject<Dictionary<object, object>>(inStream);
        return new MonsterPropertiesPacket(id, data);
    }
}