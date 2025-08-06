using System.Collections.Generic;
using LiteNetLib.Utils;

namespace WukongMp.Api.ECS.Components;

public struct AttributesState : INetSerializable
{
    public Dictionary<int, float> Data;

    public void Serialize(NetDataWriter writer)
    {
        writer.Put((byte)Data.Count);
        foreach (var kvp in Data)
        {
            writer.Put(kvp.Key);
            writer.Put(kvp.Value);
        }
    }

    public void Deserialize(NetDataReader reader)
    {
        var count = reader.GetByte();
        Data = new Dictionary<int, float>(count);
        for (var i = 0; i < count; i++)
        {
            var key = reader.GetInt();
            var value = reader.GetFloat();
            Data[key] = value;
        }
    }
}