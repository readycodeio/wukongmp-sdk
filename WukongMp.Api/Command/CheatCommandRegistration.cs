using ReadyM.Api.Command;
using WukongMp.Api.Chat;
using WukongMp.Api.State;

namespace WukongMp.Api.Command;

internal class CheatCommandRegistration(
    WukongPlayerState playerState,
    WukongAreaState areaState,
    WukongChatter chatter,
    WukongServerRpcCallbacks serverRpc) : IConsoleCommandRegistration
{
    public void RegisterCommands(ConsoleCommandRegistry registry)
    {
        registry.AddCommand("cheats", ConsoleCommand.Create(ToggleCheats, isDebugOnly: true));
    }
    
    private void ToggleCheats()
    {
        // NOTE: Why do we make that check
        if (playerState.LocalMainCharacter is not { } mainEntity)
            return;

        if (areaState.IsMasterClient && areaState.CurrentArea.HasValue)
        {
            var roomComp = areaState.CurrentArea.Value.Room;
            // TODO: Move to server rpc response.
            chatter.SendServerMessage(roomComp.CheatsAllowed ? "CheatsDisabled" : "CheatsEnabled");
            
            serverRpc.SendEnableCheats(areaState.CurrentArea.Value.Scope.AreaId, !roomComp.CheatsAllowed);
        }
    }
}