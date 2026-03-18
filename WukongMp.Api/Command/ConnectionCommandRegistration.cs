using ReadyM.Api.Command;
using WukongMp.Api.Chat;
using WukongMp.Api.State;

namespace WukongMp.Api.Command;

internal class ConnectionCommandRegistration(
    WukongPlayerState playerState,
    WukongAreaState areaState,
    WukongConnectionManager connection,
    WukongChatter chatter
) : IConsoleCommandRegistration
{
    public void RegisterCommands(ConsoleCommandRegistry registry)
    {
        registry.AddCommand("reconnect", ConsoleCommand.Create(RequestReconnect, isDebugOnly: false));
        registry.AddCommand("disconnect", ConsoleCommand.Create(RequestDisconnect, isDebugOnly: true));
    }

    private void RequestDisconnect()
    {
        if (areaState.InRoom)
        {
            chatter.SendServerMessage("PlayerLeft", playerState.Nickname);
            connection.Disconnect();
        }
    }

    private void RequestReconnect()
    {
        connection.Reconnect();
    }
}