using ReadyM.Api.Command;

namespace WukongMp.Swarm;

public class SwarmModeConsoleCommandRegistration(SpawnEnemySwarmSystem swarmSystem) : IConsoleCommandRegistration
{
    public void RegisterCommands(ConsoleCommandRegistry registry)
    {
        registry.AddCommand("swarm_mode", ConsoleCommand.Create(EnableSwarmMode));
    }

    private void EnableSwarmMode() => swarmSystem.Enable();
}