using ReadyM.Api.Command;
using WukongMp.Api.Chat;
using WukongMp.Api.State;

namespace WukongMp.Api.Command;

public class ConnectionCommandRegistration(
    WukongPlayerState playerState,
    WukongConnectionManager connection,
    WukongChatter chatter) : IConsoleCommandRegistration
{
    public void RegisterCommands(ConsoleCommandRegistry registry)
    {
        registry.AddCommand("reconnect", ConsoleCommand.Create(RequestReconnect, isDebugOnly: false));
        registry.AddCommand("disconnect", ConsoleCommand.Create(RequestDisconnect, isDebugOnly: true));
    }
    
    private void RequestDisconnect()
    {
        if (connection.AreaState.InRoom)
        {
            chatter.SendServerMessage("PlayerLeft", playerState.NickName);
            connection.Disconnect();
        }
    }

    private void RequestReconnect()
    {
        connection.Reconnect();
    }
}