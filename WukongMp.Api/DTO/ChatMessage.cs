using LiteNetLib.Utils;
using ReadyM.Api.Idents;

namespace WukongMp.Api.DTO;

internal struct ChatMessage : INetSerializable
{
    private ChatMessage(PlayerId playerId, bool localized, string? nickname, string message, string[] placeholders)
    {
        PlayerId = playerId;
        Localized = localized;
        Nickname = nickname;
        Message = message;
        Placeholders = placeholders;
    }

    public static ChatMessage CreateServerMessage(string message)
    {
        return new ChatMessage(PlayerId.Server, false, "", message, []);
    }

    public static ChatMessage CreateLocalizedServerMessage(string message, string[] placeholders)
    {
        return new ChatMessage(PlayerId.Server, true, "", message, placeholders);
    }

    public static ChatMessage CreateClientMessage(PlayerId playerId, string nickname, string message)
    {
        return new ChatMessage(playerId, false, nickname, message, []);
    }

    public PlayerId PlayerId;
    public bool Localized;
    public string? Nickname;
    public string Message;
    public string[] Placeholders;

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(PlayerId);
        writer.Put(Localized);
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
        Localized = reader.GetBool();
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
}