using System;
using b1;
using Microsoft.Extensions.Logging;
using ReadyM.Api.Idents;
using WukongMp.Api.Configuration;
using WukongMp.Api.DTO;
using WukongMp.Api.Resources;
using WukongMp.Api.State;
using WukongMp.Api.UI;

namespace WukongMp.Api.Chat;

internal class WukongChatter : IDisposable
{
    private readonly WukongPlayerState _playerState;
    private readonly WukongClientRpcCallbacks _clientRpc;
    private readonly WukongWidgetManager _widgetManager;
    private readonly ILogger _logger;

    private string NickName => _playerState.LocalPlayerEntity?.GetState().Nickname ?? "";

    public WukongChatter(
        WukongPlayerState playerState,
        WukongClientRpcCallbacks clientRpc,
        WukongWidgetManager widgetManager,
        ILogger logger)
    {
        _playerState = playerState;
        _clientRpc = clientRpc;
        _widgetManager = widgetManager;
        _logger = logger;
        _logger.LogDebug("Initializing WukongChatter");

        _clientRpc.OnGetChatMessage += OnGetMessage;
    }

    public void Dispose()
    {
        _logger.LogDebug("Disposing WukongChatter");

        _clientRpc.OnGetChatMessage -= OnGetMessage;
    }

    public void ProcessMessage(string message)
    {
        if (!string.IsNullOrWhiteSpace(message))
        {
            message = message.Trim();
            if (_playerState.LocalPlayerId.HasValue)
            {
                if (message.StartsWith("/"))
                {
                    AddLocalServerMessage("HintCommandsUse");
                }

                SendChatMessage(_playerState.LocalPlayerId.Value, NickName, message);
            }
            else
                Logging.LogError("Cannot send chat message because local player ID is not set");
        }
    }

    private void SendChatMessage(PlayerId playerId, string nickname, string message)
    {
        _logger.LogDebug("Sending message {Message}", message);
        _clientRpc.SendChatMessage(ChatMessage.CreateClientMessage(playerId, nickname, message));
    }

    public void SendServerMessage(string message, params string[] args)
    {
        _logger.LogDebug("Sending server message {Message}", message);
        _clientRpc.SendChatMessage(ChatMessage.CreateServerMessage(message, args));
    }

    private void OnGetMessage(ChatMessage message)
    {
        var isServer = message.PlayerId == PlayerId.Server;
        var messageColor = isServer ? Constants.ServerMessageColor : Constants.PlayerMessageColor;

        var sender = _playerState.GetMainCharacterByPlayerId(message.PlayerId);
        if (sender.HasValue && _playerState.LocalMainCharacter.HasValue)
        {
            var senderPawn = sender.Value.Pawn;
            var localPlayerPawn = _playerState.LocalMainCharacter.Value.Pawn;
            var isEnemy = BGUFunctionLibraryCS.BGUIsEnemyTeam(localPlayerPawn, senderPawn);
            if (isEnemy)
            {
                messageColor = Constants.EnemyPlayerMessageColor;
            }
        }

        var translatedMessage = message.Message;
        if (isServer)
        {
            translatedMessage = string.Format(BuiltinTexts.ResourceManager.GetString(message.Message, BuiltinTexts.Culture)!, [.. message.Placeholders]);
            _widgetManager.AddSystemChatMessage(translatedMessage, messageColor);
        }
        else
        {
            _widgetManager.AddChatMessage(message.Nickname!, translatedMessage, messageColor);
        }

        _logger.LogDebug("Message \"{Message}\" received from \"{Sender}\"", message.Message, isServer ? "Server" : message.Nickname!);
    }

    public void AddLocalServerMessage(string message, params string[] placeholders)
    {
        var translatedMessage = string.Format(BuiltinTexts.ResourceManager.GetString(message, BuiltinTexts.Culture)!, [.. placeholders]);
        _widgetManager.AddSystemChatMessage(translatedMessage, Constants.ServerMessageColor);
    }
}