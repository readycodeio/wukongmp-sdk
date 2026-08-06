using ReadyM.Api.Command;
using WukongMp.Api.Chat;
using WukongMp.Api.State;

namespace WukongMp.Api;

internal class CheatCommandRegistration(
    WukongAreaState areaState,
    WukongChatter chatter,
    WukongServerRpcCallbacks serverRpc
) : IConsoleCommandRegistration
{
    public void RegisterCommands(ConsoleCommandRegistry registry)
    {
        registry.AddCommand("cheats", ConsoleCommand.Create(ToggleCheats, isDebugOnly: true));
    }

    private void ToggleCheats()
    {
        if (areaState is { IsMasterClient: true, CurrentArea: not null })
        {
            var roomComp = areaState.CurrentArea.Value.Room;
            chatter.SendLocalizedServerMessage(roomComp.CheatsAllowed ? "CheatsDisabled" : "CheatsEnabled");

            serverRpc.SendEnableCheats(areaState.CurrentArea.Value.Scope.AreaId, !roomComp.CheatsAllowed);
        }
    }
}