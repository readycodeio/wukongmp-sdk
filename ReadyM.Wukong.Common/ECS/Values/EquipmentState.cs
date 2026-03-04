using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Text.Json;
using System.Text.Json.Serialization;
using LiteNetLib.Utils;
using ReadyM.Api.Serialization;

namespace ReadyM.Wukong.Common.ECS.Values;

public struct EquipmentState : INetSerializable, IDeltaEquatable<EquipmentState>, IEquatable<EquipmentState>
{
    [RegisterJsonConverter]
    public class Converter : JsonConverter<EquipmentState>
    {
        public override EquipmentState Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => TextDeserialize(ref reader, options);

        public override void Write(Utf8JsonWriter writer, EquipmentState value, JsonSerializerOptions options)
            => TextSerialize(writer, value, options);
    }

    private bool _locallyDirty;
    private int[] _equipments;

    public EquipmentState()
    {
        _equipments = [0, 0, 0, 0, 0, 0, 0, 0];
    }

    private EquipmentState(int[] eq)
    {
        _locallyDirty = true;
        _equipments = eq;
    }

    public EquipmentState(IEnumerable<(EquipPosition, int)> equipments)
    {
        _locallyDirty = true;
        _equipments = [0, 0, 0, 0, 0, 0, 0, 0];
        foreach (var (position, id) in equipments)
        {
            _equipments[(int)position] = id;
        }
    }

    [Pure]
    public EquipmentState WithSetItem(EquipPosition position, int eqId) => new((int[])_equipments.Clone())
    {
        _equipments =
        {
            [(int)position] = eqId
        }
    };

    public IEnumerable<(EquipPosition, int)> GetItems()
    {
        for (var i = 0; i < (int)EquipPosition.EnumMax; i++)
        {
            var id = _equipments[i];
            if (id != 0)
            {
                yield return ((EquipPosition)i, id);
            }
        }
    }

    public bool IsLocallyDirty
        => _locallyDirty;

    public void ClearLocallyDirty()
    {
        _locallyDirty = false;
    }

    public void Serialize(NetDataWriter writer)
    {
        for (var i = 0; i < (int)EquipPosition.EnumMax; i++)
        {
            var item = _equipments[i];
            writer.Put(item);
        }
    }

    public void Deserialize(NetDataReader reader)
    {
        if (_equipments == null)
            _equipments = new int[(int)EquipPosition.EnumMax];

        _locallyDirty = true;

        for (var i = 0; i < (int)EquipPosition.EnumMax; i++)
        {
            var item = reader.GetInt();
            _equipments[i] = item;
        }
    }

    public static void SerializeUntyped(NetDataWriter writer, object customObject)
    {
        writer.PutArray(((EquipmentState)customObject)._equipments);
    }

    public static object DeserializeUntyped(NetDataReader reader)
    {
        var eq = reader.GetIntArray();
        if (eq.Length != (int)EquipPosition.EnumMax)
        {
            throw new ArgumentException($"Invalid equipment state length: {eq.Length}");
        }

        return new EquipmentState(eq);
    }

    public static void TextSerialize(Utf8JsonWriter writer, EquipmentState obj, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, obj._equipments, options);
    }

    public static EquipmentState TextDeserialize(ref Utf8JsonReader reader, JsonSerializerOptions options)
    {
        var eq = JsonSerializer.Deserialize<int[]>(ref reader, options);
        if (eq == null)
            throw new JsonException("Failed to deserialize equipment state.");

        if (eq.Length != (int)EquipPosition.EnumMax)
            throw new JsonException($"Invalid equipment state length: {eq.Length}");

        return new EquipmentState(eq);
    }

    public bool DeltaEquals(EquipmentState other, float delta)
    {
        if (_equipments.Length != other._equipments.Length)
            return false;

        for (var i = 0; i < _equipments.Length; i++)
        {
            if (_equipments[i] != other._equipments[i])
                return false;
        }

        return true;
    }

    public bool Equals(EquipmentState other)
    {
        if (_equipments.Length != other._equipments.Length)
            return false;
        for (var i = 0; i < _equipments.Length; i++)
        {
            if (_equipments[i] != other._equipments[i])
                return false;
        }
        return true;
    }

    public override bool Equals(object? obj)
    {
        return obj is EquipmentState other && Equals(other);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = _equipments.Length;
            for (var i = 0; i < _equipments.Length; i++)
            {
                hashCode = (hashCode * 397) ^ _equipments[i];
            }
            return hashCode;
        }
    }
}