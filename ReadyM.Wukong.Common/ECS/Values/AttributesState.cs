using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using LiteNetLib.Utils;
using ReadyM.Api.Serialization;

namespace ReadyM.Wukong.Common.ECS.Values;

/// <summary>
/// Entity attributes. Keys correspond to the EBGUAttrFloat enum in the game.
/// </summary>
public struct AttributesState() : INetSerializable, IDeltaEquatable<AttributesState>
{
    private ReadOnlyDictionary<byte, float>? _data = null;

    public AttributesState(Dictionary<byte, float>? data) : this()
    {
        _data = new ReadOnlyDictionary<byte, float>(data ?? []);
    }

    public void Serialize(NetDataWriter writer)
    {
        if (_data is null)
        {
            writer.Put((byte)0);
            return;
        }

        writer.Put((byte)_data.Count);
        foreach (var kvp in _data)
        {
            writer.Put(kvp.Key);
            writer.Put(kvp.Value);
        }
    }

    public void Deserialize(NetDataReader reader)
    {
        var count = reader.GetByte();
        if (count == 0)
        {
            _data = null;
            return;
        }

        var data = new Dictionary<byte, float>(count);

        for (var i = 0; i < count; i++)
        {
            var key = reader.GetByte();
            var value = reader.GetFloat();
            data.Add(key, value);
        }

        _data = new ReadOnlyDictionary<byte, float>(data);
    }

    public bool DeltaEquals(AttributesState other, float delta)
    {
        if (_data is null && other._data is null)
            return true;

        if (_data is null || other._data is null)
            return false;

        if (_data.Count != other._data.Count)
            return false;

        foreach (var d in _data)
        {
            if (!other._data.TryGetValue(d.Key, out var otherValue) || Math.Abs(d.Value - otherValue) > delta)
                return false;
        }

        return true;
    }

    public float GetAttribute(byte attr)
    {
        if (_data is null)
            return 0;

        return _data[attr];
    }

    public AttributesState WithSetAttribute(byte key, float value)
    {
        var newData = _data != null ? new Dictionary<byte, float>(_data) : [];
        newData[key] = value;
        return new AttributesState(newData);
    }

    public Dictionary<byte, float> ToDictionary()
    {
        return _data != null ? new Dictionary<byte, float>(_data) : [];
    }

    public IEnumerator<KeyValuePair<byte, float>> GetEnumerator()
    {
        if (_data is null)
            yield break;

        foreach (var kvp in _data)
            yield return kvp;
    }

    public bool TryGetAttribute(byte key, out float value)
    {
        if (_data is null)
        {
            value = 0;
            return false;
        }

        return _data.TryGetValue(key, out value);
    }
}