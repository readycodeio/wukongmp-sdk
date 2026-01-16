using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using LiteNetLib.Utils;
using ReadyM.Api.Multiplayer.Idents;
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
    
    private ChatMessage(PlayerId playerId, string? nickname, string message, string[] placeholders)
    {
        PlayerId = playerId;
        Nickname = nickname;
        Message = message;
        Placeholders = placeholders;
    }

    public static ChatMessage CreateServerMessage(string message, string[] placeholders)
    {
        return new ChatMessage(PlayerId.Server, "", message, placeholders);
    }

    public static ChatMessage CreateClientMessage(PlayerId playerId, string nickname, string message)
    {
        return new ChatMessage(playerId, nickname, message, []);
    }

    public PlayerId PlayerId;
    public string? Nickname;
    public string Message;
    public string[] Placeholders;

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(PlayerId);
        writer.Put(Message);

        if (PlayerId != PlayerId.Server)
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
        PlayerId = reader.Get<PlayerId>();
        Message = reader.GetString();
        if (PlayerId != PlayerId.Server)
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
        writer.WriteNumber("playerId", (uint)obj.PlayerId.RawValue);
        writer.WriteString("message", obj.Message);
        writer.WriteString("nickname", obj.Nickname);
        writer.WriteStartArray("placeholders");
        writer.WriteEndObject();
    }

    public static ChatMessage TextDeserialize(ref Utf8JsonReader reader, JsonSerializerOptions options)
    {
        DebugJson.Assert(reader.TokenType == JsonTokenType.StartObject);
        
        PlayerId playerId = PlayerId.Invalid;
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
                case "playerId":
                    playerId = new PlayerId((ushort)reader.GetUInt32());
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
        
        return new ChatMessage(playerId, nickname, message, placeholders ?? []);
    }
}
