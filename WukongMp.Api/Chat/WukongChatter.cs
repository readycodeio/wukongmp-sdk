using Friflo.Engine.ECS;
using ReadyM.Api.Multiplayer.Idents;
using ReadyM.Relay.Client.State;
using System;
using WukongMp.Api.Configuration;
using WukongMp.Api.DTO;
using WukongMp.Api.Resources;
using WukongMp.Api.State;
using WukongMp.Api.UI;

namespace WukongMp.Api.Chat;

public class WukongChatter : IDisposable
{
    private readonly ClientState _state;
    private readonly WukongPlayerState _playerState;
    private readonly WukongRpcCallbacks _rpc;
    private readonly WukongWidgetManager _widgetManager;
    private string NickName => _playerState.LocalPlayerEntity?.GetState().NickName ?? "";

    public WukongChatter(
        ClientState state,
        WukongPlayerState playerState,
        WukongRpcCallbacks rpc,
        WukongWidgetManager widgetManager
    )
    {
        Logging.LogDebug("Initializing WukongChatter");

        _state = state;
        _playerState = playerState;
        _rpc = rpc;
        _widgetManager = widgetManager;

        _state.OnJoinedArea += OnJoinedAreaHandler;
        _state.OnOtherPlayerOutsideArea += OnOtherPlayerOutsideAreaHandler;

        _rpc.OnGetChatMessage += OnGetMessage;
    }

    public void Dispose()
    {
        Logging.LogDebug("Disposing WukongChatter");

        _state.OnJoinedArea -= OnJoinedAreaHandler;
        _state.OnOtherPlayerOutsideArea -= OnOtherPlayerOutsideAreaHandler;

        _rpc.OnGetChatMessage -= OnGetMessage;
    }

    public void ProcessMessage(string message)
    {
        if (!string.IsNullOrWhiteSpace(message))
        {
            message = message.Trim();
            SendChatMessage(NickName, message);
        }
    }

    private void SendChatMessage(string nickname, string message)
    {
        Logging.LogDebug("Sending message {Message}", message);
        _rpc.SendChatMessage(ChatMessage.CreateClientMessage(nickname, message));
    }

    public void SendServerMessage(string message, params string[] args)
    {
        Logging.LogDebug("Sending server message {Message}", message);
        _rpc.SendChatMessage(ChatMessage.CreateServerMessage(message, args));
    }

    private void OnGetMessage(ChatMessage message)
    {
        var senderNickname = message.IsServer ? "Server" : message.Nickname!;
        var messageColor = message.IsServer ? Constants.ServerMessageColor : Constants.PlayerMessageColor;
        var translatedMessage = message.Message;
        if (message.IsServer)
        {
            translatedMessage = string.Format(Texts.ResourceManager.GetString(message.Message, Texts.Culture)!, [.. message.Placeholders]);
        }

        Logging.LogDebug("Message \"{Message}\" received from \"{Sender}\"", message, senderNickname);
        _widgetManager.AddChatMessage(message.IsServer, senderNickname, translatedMessage, messageColor);
    }

    public void AddLocalServerMessage(string message, params string[] placeholders)
    {
        var translatedMessage = string.Format(Texts.ResourceManager.GetString(message, Texts.Culture)!, [.. placeholders]);
        _widgetManager.AddChatMessage(true, "Server", translatedMessage, Constants.ServerMessageColor);
    }

    private void OnJoinedAreaHandler(AreaId areaId, Entity entity)
    {
        var playerEntity = _playerState.LocalPlayerEntity;
        if (playerEntity == null)
            return;
        ref var player = ref playerEntity.Value.GetState();
        Logging.LogDebug("Player {PlayerName} joined the room", player.NickName);
        SendServerMessage("PlayerJoined", player.NickName);
    }

    private void OnOtherPlayerOutsideAreaHandler(PlayerId arg1, AreaId arg2, ReadyM.Api.Multiplayer.Common.OtherPlayerOutsideAreaReason arg3)
    {
        var playerEntity = _playerState.GetPlayerById(arg1);
        if (playerEntity == null)
            return;
        ref var player = ref playerEntity.Value.GetState();
        var nickname = player.NickName;
        AddLocalServerMessage("PlayerLeft", [nickname]);
    }
}
