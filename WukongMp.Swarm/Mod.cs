using System.Linq;
using Microsoft.Extensions.Logging;
using ReadyM.Api.Command;
using ReadyM.Api.DI;
using UnrealEngine.Runtime;
using WukongMp.Sdk;
using WukongMp.Sdk.Api;
using WukongMp.Sdk.Entities;

namespace WukongMp.Swarm;

public class Mod : ModBase
{
    public override string Name => "SwarmMode";
    public override string Version => "1.0.0";

    protected override void Initialize(IDependencyContainer services)
    {
        Logger.LogWarning("Swarm mode Initialized!");
        services.RegisterSingleton<Rpc>();

        var rpc = services.Resolve<Rpc>();

        var spawnSystem = services.Resolve<SpawnEnemySwarmSystem>();

        WukongApi.Console.AddCommand("swarm_mode", ConsoleCommand.Create(() =>
        {
            spawnSystem.Enable();
            rpc.SendSwarmStarted();
        }));

        // if all players are dead, reset the swarm mode
        WukongApi.Events.OnPlayerDead += (victim, attacker) =>
        {
            var alivePlayers = WukongApi.Sync.AllMainCharacters.Count(x => !x.IsDead);

            if (alivePlayers > 0)
            {
                rpc.SendRemainingPlayers(alivePlayers);
            }
            else
            {
                rpc.SendSwarmEnded(spawnSystem.SpawnedEnemies);
                spawnSystem.Disable();
            }
        };
    }
}