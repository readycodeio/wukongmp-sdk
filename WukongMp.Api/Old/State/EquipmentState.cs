using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using BtlB1;
using LiteNetLib.Utils;
using ReadyM.Api.Serialization;

namespace WukongMp.Api.Old.State
{
    public struct EquipmentState
    {
        [RegisterJsonConverter]
        public class Converter : JsonConverter<EquipmentState>
        {
            public override EquipmentState Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
                => TextDeserialize(ref reader, options);

            public override void Write(Utf8JsonWriter writer, EquipmentState value, JsonSerializerOptions options)
                => TextSerialize(writer, value, options);
        }
        
        private readonly int[] equipments = [0, 0, 0, 0, 0, 0, 0, 0];

        private EquipmentState(int[] eq)
        {
            equipments = eq;
        }

        public EquipmentState(IEnumerable<(EquipPosition, int)> equipments)
        {
            foreach (var (position, id) in equipments)
            {
                this.equipments[(int)position] = id;
            }
        }

        public void SetEquipment(EquipPosition position, int eqId)
        {
            equipments[(int)position] = eqId;
        }

        public IEnumerable<(EquipPosition, int)> GetEquipments()
        {
            for (var i = 0; i < (int)EquipPosition.EnumMax; i++)
            {
                var id = equipments[i];
                if (id != 0)
                {
                    yield return ((EquipPosition)i, id);
                }
            }
        }

        public static void Serialize(NetDataWriter outStream, object customObject)
        {
            outStream.PutArray(((EquipmentState)customObject).equipments);
        }

        public static object Deserialize(NetDataReader inStream)
        {
            var eq = inStream.GetIntArray();
            if (eq.Length != (int)EquipPosition.EnumMax)
            {
                throw new ArgumentException($"Invalid equipment state length: {eq.Length}");
            }

            return new EquipmentState(eq);
        }
        
        public static void TextSerialize(Utf8JsonWriter writer, EquipmentState obj, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, obj.equipments, options);
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
}