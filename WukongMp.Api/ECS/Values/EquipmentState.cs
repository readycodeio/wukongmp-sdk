using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using BtlB1;
using LiteNetLib.Utils;
using ReadyM.Api.Multiplayer.Generators;
using ReadyM.Api.Serialization;

namespace WukongMp.Api.ECS.Values;

public struct EquipmentState : INetSerializable, INetDirtyFlag
{
    [RegisterJsonConverter]
    public class Converter : JsonConverter<EquipmentState>
    {
        public override EquipmentState Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => TextDeserialize(ref reader, options);

        public override void Write(Utf8JsonWriter writer, EquipmentState value, JsonSerializerOptions options)
            => TextSerialize(writer, value, options);
    }

    private bool _dirty;
    private int[] _equipments = [0, 0, 0, 0, 0, 0, 0, 0];

    private EquipmentState(int[] eq)
    {
        _equipments = eq;
    }

    public EquipmentState(IEnumerable<(EquipPosition, int)> equipments)
    {
        _dirty = true;
        foreach (var (position, id) in equipments)
        {
            _equipments[(int)position] = id;
        }
    }

    public void SetEquipment(EquipPosition position, int eqId)
    {
        _dirty = true;
        _equipments[(int)position] = eqId;
    }

    public IEnumerable<(EquipPosition, int)> GetEquipments()
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

    public bool IsDirty
        => _dirty;
    
    public void ClearDirty()
    {
        _dirty = false;
    }

    public void Serialize(NetDataWriter writer)
    {
        writer.PutArray(_equipments);
    }

    public void Deserialize(NetDataReader reader)
    {
        _dirty = true;
        _equipments = reader.GetIntArray();
        if (_equipments.Length != (int)EquipPosition.EnumMax)
        {
            throw new ArgumentException($"Invalid equipment state length: {_equipments.Length}");
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
}
