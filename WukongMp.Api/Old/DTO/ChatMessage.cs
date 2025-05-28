using System.Collections.Generic;
using LiteNetLib.Utils;

namespace WukongMp.Api.Old.DTO;

public class ChatMessage
{
    private ChatMessage(bool isServer, string nickname, string message, List<string> placeholders)
    {
        IsServer = isServer;
        Nickname = nickname;
        Message = message;
        Placeholders = placeholders;
    }

    public static ChatMessage CreateServerMessage(string message, List<string> placeholders)
    {
        return new ChatMessage(true, "", message, placeholders);
    }

    public static ChatMessage CreateClientMessage(string nickname, string message)
    {
        return new ChatMessage(false, nickname, message, []);
    }

    public bool IsServer { get; }
    public string? Nickname { get; }
    public string Message { get; }
    public List<string> Placeholders { get; }

    public static void Serialize(NetDataWriter writer, object customObject)
    {
        var chatMessage = (ChatMessage)customObject;
        writer.Put(chatMessage.IsServer);
        writer.Put(chatMessage.Message);

        if (!chatMessage.IsServer)
        {
            writer.Put(chatMessage.Nickname);
        }
        else
        {
            writer.Put(chatMessage.Placeholders.Count);
            foreach (var placeholder in chatMessage.Placeholders)
            {
                writer.Put(placeholder);
            }
        }
    }

    public static object Deserialize(NetDataReader reader)
    {
        var isServer = reader.GetBool();
        var message = reader.GetString();
        if (!isServer)
        {
            var nickname = reader.GetString();
            return CreateClientMessage(nickname, message);
        }
        else
        {
            var count = reader.GetInt();
            List<string> placeholders = [];
            for (int i = 0; i < count; i++)
            {
                placeholders.Add(reader.GetString());
            }
            return CreateServerMessage(message, placeholders);
        }
    }
}