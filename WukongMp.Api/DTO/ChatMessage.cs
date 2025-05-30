using LiteNetLib.Utils;

namespace WukongMp.Api.DTO;

public struct ChatMessage : INetSerializable
{
    private ChatMessage(bool isServer, string nickname, string message, string[] placeholders)
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

        Placeholders = reader.GetStringArray();
    }
}