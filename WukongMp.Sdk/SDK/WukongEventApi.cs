using System;
using System.Globalization;
using b1;
using Friflo.Engine.ECS;
using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer.Common;
using ReadyM.Api.Multiplayer.ECS.Managers;
using ReadyM.Api.Multiplayer.Protocol;
using ReadyM.Relay.Client.State;
using ReadyM.SDK.Client.Entities;
using ReadyM.SDK.Entities;
using WukongMp.Api;
using WukongMp.Api.ECS.Archetypes;
using WukongMp.Api.ECS.Entities;
using WukongMp.Api.State;
using WukongMp.Sdk.Common.Archetypes;

namespace WukongMp.Sdk.SDK;

internal sealed class GameEvents : IDisposable, IGameEvents
{
    private readonly IEntityApi _internalEntityApi;
    private readonly ClientState _clientState;
    private readonly WukongPlayerPawnState _pawnState;
    private readonly WukongPlayerState _playerState;
    private readonly WukongEventBus _eventBus;
    private readonly GameplayEventRouter _eventRouter;

    public GameEvents(
        IEntityApi internalEntityApi,
        ClientState clientState,
        WukongPlayerPawnState pawnState,
        WukongPlayerState playerState,
        WukongEventBus eventBus,
        GameplayEventRouter eventRouter
    )
    {
        _internalEntityApi = internalEntityApi;
        _clientState = clientState;
        _pawnState = pawnState;
        _playerState = playerState;
        _eventBus = eventBus;
        _eventRouter = eventRouter;

        _clientState.OnJoinedArea += InvokeJoinedArea;
        _clientState.OnLeftArea += InvokeLeftArea;
        _clientState.OnConnected += InvokeOnConnected;
        _clientState.OnDisconnected += InvokeOnDisconnected;
        _clientState.OnOtherPlayerInsideArea += InvokeOnOtherPlayerInsideArea;
        _clientState.OnOtherPlayerOutsideArea += InvokeOnOtherPlayerOutsideArea;
        _pawnState.OnPlayerPawnSpawned += InvokePlayerPawnSpawned;
        _playerState.OnMainCharacterEntityInitialized += InvokeMainCharacterEntityInitialized;
        _eventBus.OnExitLevel += InvokeOnExitLevel;
        _eventBus.OnLevelLoaded += InvokeOnLevelLoaded;
        _eventBus.OnLoadingScreenClose += InvokeOnLoadingScreenClose;
        _eventBus.OnBeginPlayGameplayLevel += InvokeOnBeginPlayGameplayLevel;
        _eventBus.OnEndPlayGameplayLevel += InvokeOnEndPlayGameplayLevel;
        _eventRouter.OnPlayerChangedTeam += InvokeOnPlayerChangedTeam;
        _eventRouter.OnLocalPlayerBeforeRebirth += InvokeOnLocalPlayerBeforeRebirth;
        _eventRouter.OnUnitDead += InvokeOnUnitDead;
        _eventRouter.OnLocalPlayerChangedSpectator += InvokeOnLocalPlayerChangedSpectator;
        _eventRouter.OnMonsterSpawned += InvokeOnMonsterSpawned;
        _eventRouter.OnLanguageChanged += InvokeOnLanguageChanged;
    }

    public void Dispose()
    {
        _clientState.OnJoinedArea -= InvokeJoinedArea;
        _clientState.OnLeftArea -= InvokeLeftArea;
        _clientState.OnConnected -= InvokeOnConnected;
        _clientState.OnDisconnected -= InvokeOnDisconnected;
        _clientState.OnOtherPlayerInsideArea -= InvokeOnOtherPlayerInsideArea;
        _clientState.OnOtherPlayerOutsideArea -= InvokeOnOtherPlayerOutsideArea;
        _pawnState.OnPlayerPawnSpawned -= InvokePlayerPawnSpawned;
        _playerState.OnMainCharacterEntityInitialized -= InvokeMainCharacterEntityInitialized;
        _eventBus.OnExitLevel -= InvokeOnExitLevel;
        _eventBus.OnLevelLoaded -= InvokeOnLevelLoaded;
        _eventBus.OnLoadingScreenClose -= InvokeOnLoadingScreenClose;
        _eventBus.OnBeginPlayGameplayLevel -= InvokeOnBeginPlayGameplayLevel;
        _eventBus.OnEndPlayGameplayLevel -= InvokeOnEndPlayGameplayLevel;
        _eventRouter.OnPlayerChangedTeam -= InvokeOnPlayerChangedTeam;
        _eventRouter.OnLocalPlayerBeforeRebirth -= InvokeOnLocalPlayerBeforeRebirth;
        _eventRouter.OnUnitDead -= InvokeOnUnitDead;
        _eventRouter.OnLocalPlayerChangedSpectator -= InvokeOnLocalPlayerChangedSpectator;
        _eventRouter.OnMonsterSpawned -= InvokeOnMonsterSpawned;
        _eventRouter.OnLanguageChanged -= InvokeOnLanguageChanged;
    }

    public event Action? OnBeginPlayGameplayLevel;
    public event Action? OnEndPlayGameplayLevel;
    public event Action? OnLoadingScreenClose;
    public event Action? OnLevelLoaded;
    public event Action? OnExitLevel;
    public event Action<AreaId>? OnJoinedArea;
    public event Action<AreaId>? OnLeftArea;
    public event Action<MainCharacter>? OnPlayerPawnSpawned;
    public event Action<MainCharacter>? OnMainCharacterEntityInitialized;
    public event Action<MainCharacter>? OnPlayerChangedTeam;
    public event Action? OnLocalPlayerBeforeRebirth;
    public event Action<PlayerId, AreaId>? OnOtherPlayerInsideArea;
    public event Action<PlayerId, AreaId>? OnOtherPlayerOutsideArea;
    public event Action<PlayerId>? OnConnected;
    public event Action<PlayerId, DisconnectedReason>? OnDisconnected;
    public event Action<MainCharacter, Character?>? OnPlayerDead;
    public event Action<Tamer, Character?>? OnMonsterDead;
    public event Action<Tamer>? OnMonsterSpawned;
    public event Action<bool>? OnLocalPlayerChangedSpectator;
    public event Action<CultureInfo>? OnLanguageChanged;

    private void InvokeJoinedArea(AreaId areaId, Entity _)
        => OnJoinedArea?.Invoke(areaId);

    private void InvokeLeftArea(AreaId areaId, Entity _)
        => OnLeftArea?.Invoke(areaId);

    private void InvokeOnConnected(PlayerId playerId, Entity _)
        => OnConnected?.Invoke(playerId);

    private void InvokeOnDisconnected(PlayerId playerId, Entity? _, DisconnectedReason reason)
        => OnDisconnected?.Invoke(playerId, reason);

    private void InvokeOnOtherPlayerInsideArea(PlayerId playerId, AreaId areaId, OtherPlayerInsideAreaReason _)
        => OnOtherPlayerInsideArea?.Invoke(playerId, areaId);

    private void InvokeOnOtherPlayerOutsideArea(PlayerId playerId, AreaId areaId, OtherPlayerOutsideAreaReason _)
        => OnOtherPlayerOutsideArea?.Invoke(playerId, areaId);

    private void InvokePlayerPawnSpawned(MainCharacterEntity entity, BGUCharacterCS _)
    {
        OnPlayerPawnSpawned?.Invoke(new MainCharacter(new EntityHandle(entity.Entity.RawEntity, _internalEntityApi)));
    }

    private void InvokeMainCharacterEntityInitialized(MainCharacterEntity mainCharacterEntity)
    {
        OnMainCharacterEntityInitialized?.Invoke(new MainCharacter(new EntityHandle(mainCharacterEntity.Entity.RawEntity, _internalEntityApi)));
    }

    private void InvokeOnBeginPlayGameplayLevel()
        => OnBeginPlayGameplayLevel?.Invoke();

    private void InvokeOnEndPlayGameplayLevel()
        => OnEndPlayGameplayLevel?.Invoke();

    private void InvokeOnLoadingScreenClose()
        => OnLoadingScreenClose?.Invoke();

    private void InvokeOnLevelLoaded()
        => OnLevelLoaded?.Invoke();

    private void InvokeOnExitLevel()
        => OnExitLevel?.Invoke();

    private void InvokeOnPlayerChangedTeam(PlayerEntity playerEntity, MainCharacterEntity mainCharacterEntity)
    {
        OnPlayerChangedTeam?.Invoke(new MainCharacter(new EntityHandle(mainCharacterEntity.Entity.RawEntity, _internalEntityApi)));
    }

    private void InvokeOnLocalPlayerBeforeRebirth()
        => OnLocalPlayerBeforeRebirth?.Invoke();

    private void InvokeOnUnitDead(Entity victim, Entity? attacker)
    {
        var handle = new EntityHandle(victim.RawEntity, _internalEntityApi);
        EntityHandle? attackerHandle = attacker.HasValue ? new EntityHandle(attacker.Value.RawEntity, _internalEntityApi) : null;
        Character? attackerCharacter = attackerHandle.HasValue ? new Character(attackerHandle.Value) : null;

        if (MainCharacterEntity.IsMainCharacter(victim))
        {
            OnPlayerDead?.Invoke(new MainCharacter(handle), attackerCharacter);
        }
        else if (TamerEntity.IsTamer(victim))
        {
            OnMonsterDead?.Invoke(new Tamer(handle), attackerCharacter);
        }
    }

    private void InvokeOnMonsterSpawned(Entity entity)
        => OnMonsterSpawned?.Invoke(new Tamer(new EntityHandle(entity.RawEntity, _internalEntityApi)));

    private void InvokeOnLocalPlayerChangedSpectator(bool isSpectator)
        => OnLocalPlayerChangedSpectator?.Invoke(isSpectator);

    private void InvokeOnLanguageChanged(CultureInfo cultureInfo)
        => OnLanguageChanged?.Invoke(cultureInfo);
}