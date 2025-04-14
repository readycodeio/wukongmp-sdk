using LiteNetLib.Utils;

namespace WukongApi;

public class ChatMessage
{
    private ChatMessage(bool isServer, string nickname, string message)
    {
        IsServer = isServer;
        Nickname = nickname;
        Message = message;
    }

    public static ChatMessage CreateServerMessage(string message)
    {
        return new ChatMessage(true, "", message);
    }

    public static ChatMessage CreateClientMessage(string nickname, string message)
    {
        return new ChatMessage(false, nickname, message);
    }

    public bool IsServer { get; }
    public string? Nickname { get; }
    public string Message { get; }

    public static void Serialize(NetDataWriter writer, object customObject)
    {
        var chatMessage = (ChatMessage)customObject;
        writer.Put(chatMessage.IsServer);
        writer.Put(chatMessage.Message);

        if (!chatMessage.IsServer)
        {
            writer.Put(chatMessage.Nickname);
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

        return CreateServerMessage(message);
    }
}