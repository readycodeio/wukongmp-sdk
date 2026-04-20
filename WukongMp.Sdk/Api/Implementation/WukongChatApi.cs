using UnrealEngine.Runtime;
using WukongMp.Api;
using WukongMp.Api.Chat;
using WukongMp.Api.State;
using WukongMp.Api.UI;

namespace WukongMp.Sdk.Api.Implementation;

internal sealed class WukongChatApi(
    WukongPlayerState playerState,
    WukongWidgetManager widgetManager,
    WukongChatter chatter
) : IWukongChatApi
{
    public void ShowLocalMessage(string message, FLinearColor color)
    {
        widgetManager.AddSystemChatMessage(message, color);
    }

    public void SendPlayerMessage(string message)
    {
        var player = playerState.LocalPlayerEntity;
        if (!player.HasValue)
        {
            Logging.LogWarning("Trying to send player chat message while local player is null");
            return;
        }

        var playerId = playerState.LocalPlayerId!;
        var nickname = player.Value.GetState().Nickname;

        chatter.SendChatMessage(playerId.Value, nickname, message);
    }

    public void SendServerMessage(string message)
    {
        chatter.SendServerMessage(message);
    }
}