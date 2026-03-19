using System;
using System.Globalization;
using Friflo.Engine.ECS;
using WukongMp.Api.ECS.Entities;

namespace WukongMp.Api;

internal sealed class GameplayEventRouter // TODO: Export these in public API with object-like wrappers
{
    public event Action<CultureInfo>? OnLanguageChanged;
    public event Action<Entity, Entity?>? OnUnitDead;
    public event Action<Entity>? OnMonsterSpawned;
    public event Action<bool>? OnLocalPlayerChangedSpectator;
    public event Action<PlayerEntity, MainCharacterEntity>? OnPlayerChangedTeam;
    public event Action? OnLocalPlayerBeforeRebirth;

    public void RaiseOnLanguageChanged(CultureInfo culture)
    {
        OnLanguageChanged?.Invoke(culture);
    }

    public void RaiseOnUnitDead(Entity victimEntity, Entity? attackerEntity)
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
}