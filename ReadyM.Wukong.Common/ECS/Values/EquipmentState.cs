using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using LiteNetLib.Utils;
using ReadyM.Api.Serialization;

namespace ReadyM.Wukong.Common.ECS.Values;

/// <summary>
/// Player character's equipment.
/// </summary>
public unsafe struct EquipmentState : INetSerializable, IDeltaEquatable<EquipmentState>, IEquatable<EquipmentState>
{
    private const int Slots = 8;

    [RegisterJsonConverter]
    public class Converter : JsonConverter<EquipmentState>
    {
        public override EquipmentState Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => TextDeserialize(ref reader, options);

        public override void Write(Utf8JsonWriter writer, EquipmentState value, JsonSerializerOptions options)
            => TextSerialize(writer, value, options);
    }

    private fixed int _equipments[Slots];

    private EquipmentState(ReadOnlySpan<int> eq)
    {
        Debug.Assert(eq.Length == Slots, $"EquipmentState array length must be {Slots}");

        for (var i = 0; i < eq.Length; i++)
        {
            _equipments[i] = eq[i];
        }
    }

    public EquipmentState(IEnumerable<(EquipPosition, int)> equipments)
    {
        foreach (var (position, id) in equipments)
        {
            _equipments[(int)position] = id;
        }
    }

    public EquipmentState WithSetItem(EquipPosition position, int eqId)
    {
        fixed (int* p = _equipments)
        {
            var span = new Span<int>(p, Slots);
            var item = new EquipmentState(span);
            item._equipments![(int)position] = eqId;
            return item;
        }
    }

    public List<(EquipPosition, int)> GetItems()
    {
        var list = new List<(EquipPosition, int)>(Slots);

        for (var i = 0; i < Slots; i++)
        {
            var id = _equipments[i];
            if (id != 0 || i == (int)EquipPosition.Head) // invisible head bug workaround: always include head even if it's 0
            {
                list.Add(((EquipPosition)i, id));
            }
        }

        return list;
    }

    public void Serialize(NetDataWriter writer)
    {
        for (var i = 0; i < Slots; i++)
        {
            var item = _equipments[i];
            writer.Put(item);
        }
    }

    public void Deserialize(NetDataReader reader)
    {
        for (var i = 0; i < Slots; i++)
        {
            var item = reader.GetInt();
            _equipments[i] = item;
        }
    }

    public static void TextSerialize(Utf8JsonWriter writer, EquipmentState obj, JsonSerializerOptions options)
    {
        var span = new ReadOnlySpan<int>(obj._equipments, Slots).ToArray();
        JsonSerializer.Serialize(writer, span, options);
    }

    public static EquipmentState TextDeserialize(ref Utf8JsonReader reader, JsonSerializerOptions options)
    {
        var eq = JsonSerializer.Deserialize<int[]>(ref reader, options);
        if (eq == null)
            throw new JsonException("Failed to deserialize equipment state.");

        return eq.Length == Slots ? new EquipmentState(eq) : throw new JsonException($"Invalid equipment state length: {eq.Length}");
    }

    public bool DeltaEquals(EquipmentState other, float delta) => Equals(other);

    public bool Equals(EquipmentState other)
    {
        for (var i = 0; i < Slots; i++)
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
            var hashCode = Slots;
            for (var i = 0; i < Slots; i++)
            {
                hashCode = (hashCode * 397) ^ _equipments[i];
            }

            return hashCode;
        }
    }

    public int GetItem(EquipPosition pos)
    {
        return _equipments[(int)pos];
    }
}