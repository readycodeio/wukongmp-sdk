using b1;
using Microsoft.Extensions.Logging;
using ReadyM.Api.DI;
using ReadyM.Api.Helpers;
using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer.Client;
using ReadyM.Api.Multiplayer.Generators;
using ReadyM.Api.Multiplayer.Protocol.Enums;
using ReadyM.Api.Multiplayer.RPC;
using ReadyM.Api.Multiplayer.Serialization;
using ReadyM.Relay.Client;
using WukongMp.Api.Configuration;
using WukongMp.Api.DTO;
using WukongMp.Api.Resources;
using WukongMp.Api.State;
using WukongMp.Api.UI;

namespace WukongMp.Api.Chat;

internal partial class WukongChatter(
    WukongPlayerState playerState,
    WukongWidgetManager widgetManager,
    IClientEcsUpdateLoop ecsLoop,
    ILogger logger,
    IRpcClient rpcClient,
    IRelaySerializer serializer
) : RpcClassBase(rpcClient, serializer)
{
    private string NickName => playerState.LocalPlayerEntity?.GetState().Nickname ?? "";

    [RpcEvent(RelayMode.AreaOfInterestAll)]
    private void OnChatMessage(ChatMessage message)
    {
        ecsLoop.Scheduler.Schedule(static (_, self, message0) => { self.OnGetMessage(message0); }, this, message);
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
        SendChatMessage(ChatMessage.CreateClientMessage(playerId, nickname, message));
    }

    public void SendServerMessage(string message)
    {
        logger.LogDebug("Sending server message {Message}", message);
        SendChatMessage(ChatMessage.CreateServerMessage(message));
    }

    public void SendLocalizedServerMessage(string message, params string[] args)
    {
        logger.LogDebug("Sending server message {Message}", message);
        SendChatMessage(ChatMessage.CreateLocalizedServerMessage(message, args));
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