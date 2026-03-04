using System;
using System.Collections.Generic;
using LiteNetLib.Utils;
using ReadyM.Api.Helpers;
using ReadyM.Api.Serialization;

namespace ReadyM.Wukong.Common.ECS.Values;

public struct AttributesState() : INetSerializable, IDeltaEquatable<AttributesState>
{
    private Dictionary<byte, float> _data = new();
    
    public ReadOnlyDictionary<byte, float> Data
        => new(_data);

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

        _data ??= new Dictionary<byte, float>();
        
        _data.Clear();
        _data.EnsureCapacity(count);
        for (var i = 0; i < count; i++)
        {
            var key = reader.GetByte();
            var value = reader.GetFloat();
            _data?.Add(key, value);
        }
    }

    public bool DeltaEquals(AttributesState other, float delta)
    {
        foreach (var d in _data)
        {
            if (!other._data.TryGetValue(d.Key, out var otherValue) || Math.Abs(d.Value - otherValue) > delta)
                return false;
        }
        
        foreach (var d in other._data)
        {
            if (!_data.ContainsKey(d.Key))
                return false;
        }

        return true;
    }

    public float GetAttribute(byte attr)
        => _data[attr];
    
    public void SetAttribute(byte key, float value)
        => _data[key] = value;
    
    public Dictionary<byte, float>.Enumerator GetEnumerator()
        => _data.GetEnumerator();

    public bool TryGetAttribute(byte key, out float value)
        => _data.TryGetValue(key, out value);
}