using ReadyM.Api.Command;
using ReadyM.Api.Mapping.Events;
using ReadyM.Api.Mapping.Tags;
using WukongMp.Api.Chat;
using WukongMp.Api.ECS.GameEvents;
using WukongMp.Api.Resources;
using WukongMp.Api.State;

namespace WukongMp.Api.Command;

internal class RebirthCommandRegistration(
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
        ), default(EmptyContext));
        chatter.SendLocalizedServerMessage(nameof(BuiltinTexts.PlayerRequestedRebirth), playerState.Nickname);
    }

    private void RequestPointRebirth()
    {
        if (playerState.LocalMainCharacter is not { } mainEntity)
            return;

        mappedEvent.InvokeInGameAndNotifyEcs(new RebirthPlayerEvent(
            entity: mainEntity.Entity,
            teleport: true
        ), default(EmptyContext));

        chatter.SendLocalizedServerMessage(nameof(BuiltinTexts.PlayerRequestedRebirth), playerState.Nickname);
    }
}