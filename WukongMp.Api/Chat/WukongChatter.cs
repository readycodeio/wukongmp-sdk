using b1;
using Microsoft.Extensions.Logging;
using ReadyM.Api.DI;
using ReadyM.Api.Helpers;
using ReadyM.Api.Idents;
using ReadyM.Relay.Client;
using WukongMp.Api.Configuration;
using WukongMp.Api.DTO;
using WukongMp.Api.Resources;
using WukongMp.Api.State;
using WukongMp.Api.UI;

namespace WukongMp.Api.Chat;

internal class WukongChatter(
    WukongPlayerState playerState,
    WukongClientRpcCallbacks clientRpc,
    WukongWidgetManager widgetManager,
    ILogger logger
) : IHostedService
{
    private string NickName => playerState.LocalPlayerEntity?.GetState().Nickname ?? "";

    public void OnScopeStart()
    {
        clientRpc.OnGetChatMessage += OnGetMessage;
    }

    public void Dispose()
    {
        logger.LogDebug("Disposing WukongChatter");

        clientRpc.OnGetChatMessage -= OnGetMessage;
    }

    public void ProcessMessage(string message)
    {
        if (!string.IsNullOrWhiteSpace(message))
        {
            message = message.Trim();
            if (playerState.LocalPlayerId.HasValue)
            {
                if (message.StartsWith("/"))
                {
                    AddLocalServerMessage("HintCommandsUse");
                }

                SendChatMessage(playerState.LocalPlayerId.Value, NickName, message);
            }
            else
                Logging.LogError("Cannot send chat message because local player ID is not set");
        }
    }

    public void SendChatMessage(PlayerId playerId, string nickname, string message)
    {
        logger.LogDebug("Sending message {Message}", message);
        clientRpc.SendChatMessage(ChatMessage.CreateClientMessage(playerId, nickname, message));
    }

    public void SendServerMessage(string message)
    {
        logger.LogDebug("Sending server message {Message}", message);
        clientRpc.SendChatMessage(ChatMessage.CreateServerMessage(message));
    }

    public void SendLocalizedServerMessage(string message, params string[] args)
    {
        logger.LogDebug("Sending server message {Message}", message);
        clientRpc.SendChatMessage(ChatMessage.CreateLocalizedServerMessage(message, args));
    }

    private void OnGetMessage(ChatMessage message)
    {
        var isServer = message.PlayerId == PlayerId.Server;
        var messageColor = isServer ? Constants.ServerMessageColor : Constants.PlayerMessageColor;

        var sender = playerState.GetMainCharacterByPlayerId(message.PlayerId);
        if (sender.HasValue && playerState.LocalMainCharacter.HasValue)
        {
            var senderPawn = sender.Value.Pawn;
            var localPlayerPawn = playerState.LocalMainCharacter.Value.Pawn;
            var isEnemy = BGUFunctionLibraryCS.BGUIsEnemyTeam(localPlayerPawn, senderPawn);
            if (isEnemy)
            {
                messageColor = Constants.EnemyPlayerMessageColor;
            }
        }

        var translatedMessage = message.Message;
        if (message.Localized)
        {
            translatedMessage = string.Format(BuiltinTexts.ResourceManager.GetString(message.Message, BuiltinTexts.Culture)!, [.. message.Placeholders]);
        }

        if (isServer)
        {
            widgetManager.AddSystemChatMessage(translatedMessage, messageColor);
        }
        else
        {
            widgetManager.AddChatMessage(message.Nickname!, translatedMessage, messageColor);
        }

        logger.LogDebug("Message \"{Message}\" received from \"{Sender}\"", message.Message, isServer ? "Server" : message.Nickname!);
    }

    public void AddLocalServerMessage(string message, params string[] placeholders)
    {
        var translatedMessage = string.Format(BuiltinTexts.ResourceManager.GetString(message, BuiltinTexts.Culture)!, [.. placeholders]);
        widgetManager.AddSystemChatMessage(translatedMessage, Constants.ServerMessageColor);
    }
}