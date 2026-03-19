using System.Linq;
using UnrealEngine.Runtime;
using WukongMp.Api;
using WukongMp.Sdk;
using WukongMp.Sdk.Api;
using WukongMp.Sdk.Entities;

namespace WukongMp.Swarm;

public class Mod : ModBase
{
    public override string Name => "Swarm";
    public override string Version => "1.0.0";

    protected override void Initialize(IDependencyContainer services)
    {
        base.Initialize(services);

        var spawnSystem = services.Resolve<SpawnEnemySwarmSystem>();

        WukongApi.Console.AddCommands(new SwarmModeConsoleCommandRegistration(spawnSystem));

        // if all players are dead, reset the swarm mode
        WukongApi.Events.OnPlayerDead += (victim, attacker) =>
        {
            var alivePlayers = WukongApi.Sync.AllMainCharacters.Count(x => !x.IsDead);

            if (alivePlayers > 0)
            {
                WukongApi.Local.AddChatMessage($"Remaining players: {alivePlayers}", FLinearColor.Yellow);
            }
            else
            {
                spawnSystem.Disable();
            }
        };
    }
}