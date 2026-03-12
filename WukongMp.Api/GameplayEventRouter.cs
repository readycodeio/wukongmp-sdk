using System;
using System.Collections.Generic;
using System.Globalization;
using Friflo.Engine.ECS;
using UnrealEngine.Engine;
using WukongMp.Api.ECS.Entities;

namespace WukongMp.Api;

internal delegate void InternalObstacleCollisionDelegate(MainCharacterEntity mainEntity, AActor obstacle, out bool shouldBlock);
    
internal sealed class GameplayEventRouter // TODO: Export these in public API with object-like wrappers
{
    public event Action<CultureInfo>? OnLanguageChanged;
    public event Action<Entity, Entity>? OnUnitDead;
    public event Action<Entity>? OnMonsterSpawned;
    public event Action<PlayerEntity, MainCharacterEntity>? OnPlayerChangedTeam;
    public event Action<bool>? OnLocalPlayerChangedSpectator;
    public event Action? OnLocalPlayerBeforeRebirth;
        
    private readonly List<InternalObstacleCollisionDelegate> _obstacleCollisionHandlers = new();
        
    public event InternalObstacleCollisionDelegate OnObstacleCollision
    {
        add => _obstacleCollisionHandlers.Add(value);
        remove => _obstacleCollisionHandlers.Remove(value);
    }
        
    public event Action<AActor>? OnDisableObstacle;
        
    public void RaiseOnLanguageChanged(CultureInfo culture)
    {
        OnLanguageChanged?.Invoke(culture);
    }

    public void RaiseOnUnitDead(Entity victimEntity, Entity attackerEntity)
    {
        OnUnitDead?.Invoke(victimEntity, attackerEntity);
    }
        
    public void RaiseOnMonsterSpawned(Entity monsterEntity)
    {
        OnMonsterSpawned?.Invoke(monsterEntity);
    }

    public void RaiseOnPlayerChangedTeam(PlayerEntity playerEntity, MainCharacterEntity mainEntity)
    {
        OnPlayerChangedTeam?.Invoke(playerEntity, mainEntity);
    }

    public void RaiseOnLocalPlayerChangedSpectator(bool enabled)
    {
        OnLocalPlayerChangedSpectator?.Invoke(enabled);
    }

    public void RaiseOnLocalPlayerBeforeRebirth()
    {
        OnLocalPlayerBeforeRebirth?.Invoke();
    }

    public void NotifyObstacleCollision(MainCharacterEntity mainEntity, AActor obstacle, out bool shouldBlock)
    {
        shouldBlock = false;
            
        foreach (var handler in _obstacleCollisionHandlers)
        {
            handler.Invoke(mainEntity, obstacle, out var b);
                
            if (b)
            {
                shouldBlock = true;
            }
        }
    }

    public void NotifyDisableObstacle(AActor obstacle)
    {
        OnDisableObstacle?.Invoke(obstacle);
    }
}