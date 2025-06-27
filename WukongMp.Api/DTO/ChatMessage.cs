using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using LiteNetLib.Utils;
using ReadyM.Api.Serialization;

namespace WukongMp.Api.DTO;

public struct ChatMessage : INetSerializable
{
    [RegisterJsonConverter]
    public class Converter : JsonConverter<ChatMessage>
    {
        public override ChatMessage Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => TextDeserialize(ref reader, options);

        public override void Write(Utf8JsonWriter writer, ChatMessage value, JsonSerializerOptions options)
            => TextSerialize(writer, value, options);
    }
    
    private ChatMessage(bool isServer, string? nickname, string message, string[] placeholders)
    {
        IsServer = isServer;
        Nickname = nickname;
        Message = message;
        Placeholders = placeholders;
    }

    public static ChatMessage CreateServerMessage(string message, string[] placeholders)
    {
        return new ChatMessage(true, "", message, placeholders);
    }

    public static ChatMessage CreateClientMessage(string nickname, string message)
    {
        return new ChatMessage(false, nickname, message, []);
    }

    public bool IsServer;
    public string? Nickname;
    public string Message;
    public string[] Placeholders;

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(IsServer);
        writer.Put(Message);

        if (!IsServer)
        {
            writer.Put(Nickname);
        }
        else
        {
            writer.PutArray(Placeholders);
        }
    }

    public void Deserialize(NetDataReader reader)
    {
        IsServer = reader.GetBool();
        Message = reader.GetString();
        if (!IsServer)
        {
            Nickname = reader.GetString();
        }
        else
        {
            Placeholders = reader.GetStringArray();
        }
    }

    public static void TextSerialize(Utf8JsonWriter writer, ChatMessage obj, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteBoolean("isServer", obj.IsServer);
        writer.WriteString("message", obj.Message);
        writer.WriteString("nickname", obj.Nickname);
        writer.WriteStartArray("placeholders");
        writer.WriteEndObject();
    }

    public static ChatMessage TextDeserialize(ref Utf8JsonReader reader, JsonSerializerOptions options)
    {
        DebugJson.Assert(reader.TokenType == JsonTokenType.StartObject);
        
        bool isServer = false;
        string? nickname = null;
        string message = "";
        string[]? placeholders = null;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                break;

            DebugJson.Assert(reader.TokenType == JsonTokenType.PropertyName);
            var propertyName = reader.GetString()!;
            reader.Read();

            switch (propertyName)
            {
                case "isServer":
                    isServer = reader.GetBoolean();
                    break;
                case "nickname":
                    nickname = reader.GetString();
                    break;
                case "message":
                    message = reader.GetString()!;
                    break;
                case "placeholders":
                    placeholders = JsonSerializer.Deserialize<string[]>(ref reader, options);
                    break;
                default:
                    throw new JsonException($"Unexpected property: {propertyName}");
            }
        }
        
        return new ChatMessage(isServer, nickname, message, placeholders ?? []);
    }
}
