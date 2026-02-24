using ReadyM.Api.Command;
using ReadyM.Api.ECS.Worlds;
using ReadyM.Api.Mapping.Events;
using WukongMp.Api.State;
using WukongMp.Api.WukongUtils;

namespace WukongMp.Api.Command;

public class WorkaroundCommandRegistration(
    Store world,
    IMappedEventManager mappedEvent,
    WukongPlayerState playerState) : IConsoleCommandRegistration
{
    public void RegisterCommands(ConsoleCommandRegistry registry)
    {
        registry.AddCommand("softlock", ConsoleCommand.Create(ResolveSoftlock, isDebugOnly: true));
    }

    private void ResolveSoftlock()
    {
        if (playerState.LocalMainCharacter is not { } mainEntity)
            return;

        PlayerUtils.RespawnSoftlockedParty(world, mappedEvent, mainEntity);
    }

}