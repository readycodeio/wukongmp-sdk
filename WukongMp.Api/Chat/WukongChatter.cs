using b1;
using ReadyM.Api.Multiplayer.Idents;
using System;
using WukongMp.Api.Configuration;
using WukongMp.Api.DTO;
using WukongMp.Api.Resources;
using WukongMp.Api.State;
using WukongMp.Api.UI;

namespace WukongMp.Api.Chat;

public class WukongChatter : IDisposable
{
    private readonly WukongPlayerState _playerState;
    private readonly WukongRpcCallbacks _rpc;
    private readonly WukongWidgetManager _widgetManager;
    private string NickName => _playerState.LocalPlayerEntity?.GetState().NickName ?? "";

    public WukongChatter(
        WukongPlayerState playerState,
        WukongRpcCallbacks rpc,
        WukongWidgetManager widgetManager
    )
    {
        Logging.LogDebug("Initializing WukongChatter");

        _playerState = playerState;
        _rpc = rpc;
        _widgetManager = widgetManager;

        _rpc.OnGetChatMessage += OnGetMessage;
    }

    public void Dispose()
    {
        Logging.LogDebug("Disposing WukongChatter");

        _rpc.OnGetChatMessage -= OnGetMessage;
    }

    public void ProcessMessage(string message)
    {
        if (!string.IsNullOrWhiteSpace(message))
        {
            message = message.Trim();
            if (_playerState.LocalPlayerId.HasValue)
                SendChatMessage(_playerState.LocalPlayerId.Value, NickName, message);
            else
                Logging.LogError("Cannot send chat message because local player ID is not set");
        }
    }

    private void SendChatMessage(PlayerId playerId, string nickname, string message)
    {
        Logging.LogDebug("Sending message {Message}", message);
        _rpc.SendChatMessage(ChatMessage.CreateClientMessage(playerId, nickname, message));
    }

    public void SendServerMessage(string message, params string[] args)
    {
        Logging.LogDebug("Sending server message {Message}", message);
        _rpc.SendChatMessage(ChatMessage.CreateServerMessage(message, args));
    }

    private void OnGetMessage(ChatMessage message)
    {
        var isServer = message.PlayerId == PlayerId.Server;
        var messageColor = isServer ? Constants.ServerMessageColor : Constants.PlayerMessageColor;

        var sender = _playerState.GetMainCharacterById(message.PlayerId);
        if (sender.HasValue && _playerState.LocalMainCharacter.HasValue)
        {
            var senderPawn = sender.Value.GetLocalState().Pawn;
            var localPlayerPawn = _playerState.LocalMainCharacter.Value.GetLocalState().Pawn;
            var isEnemy = BGUFunctionLibraryCS.BGUIsEnemyTeam(localPlayerPawn, senderPawn);
            if (isEnemy)
            {
                messageColor = Constants.EnemyPlayerMessageColor;
            }
        }
        var senderNickname = isServer ? "Server" : message.Nickname!;
        var translatedMessage = message.Message;
        if (isServer)
        {
            translatedMessage = string.Format(Texts.ResourceManager.GetString(message.Message, Texts.Culture)!, [.. message.Placeholders]);
        }

        Logging.LogDebug("Message \"{Message}\" received from \"{Sender}\"", message.Message, senderNickname);
        _widgetManager.AddChatMessage(isServer, senderNickname, translatedMessage, messageColor);
    }

    public void AddLocalServerMessage(string message, params string[] placeholders)
    {
        var translatedMessage = string.Format(Texts.ResourceManager.GetString(message, Texts.Culture)!, [.. placeholders]);
        _widgetManager.AddChatMessage(true, "Server", translatedMessage, Constants.ServerMessageColor);
    }
}
