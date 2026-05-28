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

    private int[]? _equipments;

    public EquipmentState()
    {
        _equipments = new int[(int)EquipPosition.EnumMax];
    }

    private EquipmentState(int[] eq)
    {
        _equipments = eq;
    }

    public EquipmentState(IEnumerable<(EquipPosition, int)> equipments)
    {
        _equipments = new int[(int)EquipPosition.EnumMax];

        foreach (var (position, id) in equipments)
        {
            _equipments[(int)position] = id;
        }
    }

    [Pure]
    public EquipmentState WithSetItem(EquipPosition position, int eqId)
    {
        _equipments ??= new int[(int)EquipPosition.EnumMax];

        var equipments = (int[])_equipments.Clone();
        var item = new EquipmentState(equipments);
        item._equipments![(int)position] = eqId;
        return item;
    }

    [Pure]
    public IEnumerable<(EquipPosition, int)> GetItems()
    {
        _equipments ??= new int[(int)EquipPosition.EnumMax];

        for (var i = 0; i < (int)EquipPosition.EnumMax; i++)
        {
            var id = _equipments[i];
            if (id != 0 || i == (int)EquipPosition.Head) // invisible head bug workaround: always include head even if it's 0
            {
                yield return ((EquipPosition)i, id);
            }
        }
    }

    public void Serialize(NetDataWriter writer)
    {
        _equipments ??= new int[(int)EquipPosition.EnumMax];

        for (var i = 0; i < (int)EquipPosition.EnumMax; i++)
        {
            var item = _equipments[i];
            writer.Put(item);
        }
    }

    public void Deserialize(NetDataReader reader)
    {
        _equipments ??= new int[(int)EquipPosition.EnumMax];

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
        if (_equipments is null || other._equipments is null)
            return false;

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
        if (_equipments is null || other._equipments is null)
            return false;

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
        _equipments ??= new int[(int)EquipPosition.EnumMax];

        unchecked
        {
            var hashCode = _equipments.Length;
            foreach (var equipment in _equipments)
            {
                hashCode = (hashCode * 397) ^ equipment;
            }

            return hashCode;
        }
    }

    public int GetItem(EquipPosition pos)
    {
        return _equipments == null ? 0 : _equipments[(int)pos];
    }
}