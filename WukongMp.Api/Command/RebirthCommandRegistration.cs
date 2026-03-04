using ReadyM.Api.Command;
using ReadyM.Api.Multiplayer.Mapping.Events;
using WukongMp.Api.Chat;
using WukongMp.Api.ECS.GameEvents;
using WukongMp.Api.State;

namespace WukongMp.Api.Command;

public class RebirthCommandRegistration(
    WukongPlayerState playerState,
    IMappedEventManager mappedEvent,
    WukongChatter chatter
) : IConsoleCommandRegistration
{
    public void RegisterCommands(ConsoleCommandRegistry registry)
    {
        registry.AddCommand("rebirth", ConsoleCommand.Create(RequestRebirth, isDebugOnly: false));
        registry.AddCommand("rebirth_shrine", ConsoleCommand.Create(RequestPointRebirth, isDebugOnly: false));
    }

    private void RequestRebirth()
    {
        if (playerState.LocalMainCharacter is not { } mainEntity)
            return;

        mappedEvent.InvokeInGameAndNotifyEcs(new RebirthPlayerEvent(
            entity: mainEntity.Entity,
            teleport: false
        ));
        chatter.SendServerMessage("PlayerRequestedRebirth", playerState.NickName);
    }

    private void RequestPointRebirth()
    {
        if (playerState.LocalMainCharacter is not { } mainEntity)
            return;

        mappedEvent.InvokeInGameAndNotifyEcs(new RebirthPlayerEvent(
            entity: mainEntity.Entity,
            teleport: true
        ));

        chatter.SendServerMessage("PlayerRequestedRebirth", playerState.NickName);
    }
}