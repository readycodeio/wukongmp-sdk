using System;
using b1;
using Friflo.Engine.ECS;
using ReadyM.Api.Idents;
using ReadyM.Relay.Client.State;
using WukongMp.Api.ECS.Entities;
using WukongMp.Api.State;
using WukongMp.Sdk.Entities;

namespace WukongMp.Sdk.Api;

public sealed class WukongEventApi : IDisposable
{
    private readonly ClientState _clientState;
    private readonly WukongPlayerPawnState _pawnState;
    private readonly WukongPlayerState _playerState;

    internal WukongEventApi(ClientState clientState, WukongPlayerPawnState pawnState, WukongPlayerState playerState)
    {
        _clientState = clientState;
        _pawnState = pawnState;
        _playerState = playerState;

        _clientState.OnJoinedArea += InvokeJoinedArea;
        _clientState.OnLeftArea += InvokeLeftArea;
        _pawnState.OnPlayerPawnSpawned += InvokePlayerPawnSpawned;
        _playerState.OnMainCharacterEntityInitialized += InvokeMainCharacterEntityInitialized;
    }

    public void Dispose()
    {
        _clientState.OnJoinedArea -= InvokeJoinedArea;
        _clientState.OnLeftArea -= InvokeLeftArea;
        _pawnState.OnPlayerPawnSpawned -= InvokePlayerPawnSpawned;
        _playerState.OnMainCharacterEntityInitialized -= InvokeMainCharacterEntityInitialized;
    }

    public event Action<AreaId>? OnJoinedArea;

    public event Action<AreaId>? OnLeftArea;

    public event Action<ReadyMainCharacter>? OnPlayerPawnSpawned;
    
    public event Action<ReadyMainCharacter>? OnMainCharacterEntityInitialized;

    private void InvokeJoinedArea(AreaId areaId, Entity _)
    {
        OnJoinedArea?.Invoke(areaId);
    }

    private void InvokeLeftArea(AreaId areaId, Entity _)
    {
        OnLeftArea?.Invoke(areaId);
    }

    private void InvokePlayerPawnSpawned(MainCharacterEntity entity, BGUCharacterCS _)
    {
        OnPlayerPawnSpawned?.Invoke(new ReadyMainCharacter(WukongApi.Client, entity));
    }
    
    private void InvokeMainCharacterEntityInitialized(MainCharacterEntity mainCharacterEntity)
    {
        OnMainCharacterEntityInitialized?.Invoke(new ReadyMainCharacter(WukongApi.Client, mainCharacterEntity));
    }
}