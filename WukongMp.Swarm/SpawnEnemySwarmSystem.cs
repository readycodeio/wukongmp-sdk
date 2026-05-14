using System;
using System.Numerics;
using UnrealEngine.Runtime;
using WukongMp.Api.Configuration;
using WukongMp.Sdk;
using WukongMp.Sdk.Api;
using WukongMp.Sdk.Entities;

namespace WukongMp.Swarm;

// ReSharper disable once ClassNeverInstantiated.Global
public sealed class SpawnEnemySwarmSystem : ModSystemBase
{
    private bool _enabled;
    private float _timeSinceLastSpawn;
    private int _swarmSize = 3;
    public int SpawnedEnemies;
    private const int SwarmIncrement = 1;
    private const int SwarmMax = 7;
    private const float SpawnRadius = 1000.0f;
    private const float SpawnInterval = 10.0f;
    private const float InitialDelay = 3.0f;

    public void Enable()
    {
        _enabled = true;
    }

    public void Disable()
    {
        if (!_enabled)
            return;

        _enabled = false;

        // reset state for next time
        SpawnedEnemies = 0;
        _swarmSize = 3;
        _timeSinceLastSpawn = SpawnInterval - InitialDelay;
    }

    protected override void OnUpdate(UpdateTick tick)
    {
        if (!_enabled || !WukongApi.Sync.LocalMainCharacter.HasValue)
            return;

        _timeSinceLastSpawn += tick.deltaTime;
        if (_timeSinceLastSpawn > SpawnInterval)
        {
            _timeSinceLastSpawn = 0;
            WukongApi.Local.ShowInfoMessage($"Spawning {_swarmSize} enemies!", 1);

            // spawn a few enemies around the player
            for (var i = 0; i < _swarmSize; i++)
            {
                var position = GetNthPointOnCircle(WukongApi.Sync.LocalMainCharacter.Value.Location, i, _swarmSize);
                WukongApi.Sync.SpawnEnemy(TamerKinds.WolfSentinel, position);
            }

            // increase difficulty, up to a certain point
            _swarmSize = Math.Min(_swarmSize + SwarmIncrement, SwarmMax);

            // count total enemies spawned for end-of-mode summary
            SpawnedEnemies += _swarmSize;
        }
    }

    private static Vector3 GetNthPointOnCircle(Vector3 center, int i, int n)
    {
        var angle = (double)i / n * 2 * Math.PI;
        return new Vector3(
            center.X + (float)Math.Cos(angle) * SpawnRadius,
            center.Y + (float)Math.Sin(angle) * SpawnRadius,
            center.Z
        );
    }
}