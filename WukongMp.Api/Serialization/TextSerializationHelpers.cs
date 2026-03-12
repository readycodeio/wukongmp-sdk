using System.Text.Json;
using b1;
using ReadyM.Api.Serialization;
using UnrealEngine.Runtime;

namespace WukongMp.Api.Serialization;

internal static class TextSerializationHelpers
{
    public static void TextSerializeFVector(Utf8JsonWriter writer, FVector vec, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        writer.WriteNumberValue(vec.X);
        writer.WriteNumberValue(vec.Y);
        writer.WriteNumberValue(vec.Z);
        writer.WriteEndArray();
    }

    public static FVector TextDeserializeFVector(ref Utf8JsonReader reader, JsonSerializerOptions options)
    {
        DebugJson.Assert(reader.TokenType == JsonTokenType.StartArray);
        
        if (!reader.Read() || reader.TokenType != JsonTokenType.Number)
            throw new JsonException("Expected number for X component of FVector");
        var x = reader.GetSingle();
        if (!reader.Read() || reader.TokenType != JsonTokenType.Number)
            throw new JsonException("Expected number for Y component of FVector");
        var y = reader.GetSingle();
        if (!reader.Read() || reader.TokenType != JsonTokenType.Number)
            throw new JsonException("Expected number for Z component of FVector");
        var z = reader.GetSingle();
        
        if (!reader.Read() || reader.TokenType != JsonTokenType.EndArray)
            throw new JsonException("Expected end of array for FVector");
        
        return new FVector(x, y, z);
    }

    public static void TextSerializeFRotator(Utf8JsonWriter writer, FRotator vec, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        writer.WriteNumberValue(vec.Pitch);
        writer.WriteNumberValue(vec.Yaw);
        writer.WriteNumberValue(vec.Roll);
        writer.WriteEndArray();
    }

    public static FRotator TextDeserializeFRotator(ref Utf8JsonReader reader, JsonSerializerOptions options)
    {
        DebugJson.Assert(reader.TokenType == JsonTokenType.StartArray);
        
        if (!reader.Read() || reader.TokenType != JsonTokenType.Number)
            throw new JsonException("Expected number for Pitch component of FRotator");
        var pitch = reader.GetSingle();
        if (!reader.Read() || reader.TokenType != JsonTokenType.Number)
            throw new JsonException("Expected number for Yaw component of FRotator");
        var yaw = reader.GetSingle();
        if (!reader.Read() || reader.TokenType != JsonTokenType.Number)
            throw new JsonException("Expected number for Roll component of FRotator");
        var roll = reader.GetSingle();
        
        if (!reader.Read() || reader.TokenType != JsonTokenType.EndArray)
            throw new JsonException("Expected end of array for FRotator");
        
        return new FRotator(pitch, yaw, roll);
    }

    public static void TextSerializeDamageNumParam(Utf8JsonWriter writer, DamageNumParam dmg, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteNumber("damageNum", dmg.DamageNum);
        writer.WriteNumber("damageType", (byte)dmg.DamageType);
        writer.WritePropertyName("realHitLocation");
        TextSerializeFVector(writer, dmg.RealHitLocation, options);
        writer.WriteNumber("amplitude", dmg.Amplitude);
        writer.WriteNumber("attackerTeamType", (byte)dmg.AttackerTeamType);
        writer.WritePropertyName("realHitDir");
        TextSerializeFVector(writer, dmg.RealHitDir, options);
        writer.WriteEndObject();
    }

    public static DamageNumParam TextDeserializeDamageNumParam(ref Utf8JsonReader reader, JsonSerializerOptions options)
    {
        var damageNum = 0;
        EDamageNumberType damageType = default;
        FVector realHitLocation = default;
        float amplitude = 0f;
        EDmgNumUITeamType attackerTeamType = default;
        FVector realHitDir = default;
        
        DebugJson.Assert(reader.TokenType == JsonTokenType.StartArray);
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                break;

            DebugJson.Assert(reader.TokenType == JsonTokenType.PropertyName);
            var propertyName = reader.GetString();
            reader.Read();

            switch (propertyName)
            {
                case "damageNum":
                    damageNum = reader.GetInt32();
                    break;
                case "damageType":
                    damageType = (EDamageNumberType)reader.GetByte();
                    break;
                case "realHitLocation":
                    realHitLocation = (FVector)TextDeserializeFVector(ref reader, options);
                    break;
                case "amplitude":
                    amplitude = reader.GetSingle();
                    break;
                case "attackerTeamType":
                    attackerTeamType = (EDmgNumUITeamType)reader.GetByte();
                    break;
                case "realHitDir":
                    realHitDir = (FVector)TextDeserializeFVector(ref reader, options);
                    break;
                default:
                    reader.Skip(); // skip unknown properties
                    break;
            }
        }

        return new DamageNumParam(damageType, damageNum, amplitude, realHitLocation, realHitDir, attackerTeamType);
    }
}
