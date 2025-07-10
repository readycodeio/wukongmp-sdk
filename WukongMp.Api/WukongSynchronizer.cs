using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using b1;
using BtlShare;
using CSharpModBase;
using Microsoft.Extensions.Logging;
using ReadyM.Api;
using ReadyM.Api.Multiplayer;
using ReadyM.Relay.Client;
using ReadyM.Relay.Common;
using ReadyM.Relay.Common.Wukong;
using UnrealEngine.Runtime;
using WukongMp.Api.Configuration;
using WukongMp.Api.ECS.Systems;
using WukongMp.Api.Old;
using WukongMp.Api.Old.Api;
using WukongMp.Api.Old.State;
using WukongMp.Api.Patches;
using WukongMp.Api.UI;
using WukongMp.Api.WukongUtils;

namespace WukongMp.Api;

public class WukongSynchronizer : NetworkedStateSynchronizer, IDisposable
{
    private readonly RoomStateProxy _roomState;
    private readonly WukongPlayerRegistry _playerRegistry;
    private readonly WukongPlayerPropertyManager _playerProperty;
    private readonly WukongPlayerModeManager _modeManager;
    private readonly WukongPlayerPawnManager _playerPawnManager;
    private readonly WukongRpcCallbacks _rpc;
    private ArchetypeId _roomConfigArchetype;
    
    public WukongSynchronizer(
        Store world,
        RoomStateProxy roomState,
        WukongPlayerRegistry playerRegistry,
        WukongPlayerPropertyManager playerProperty,
        WukongPlayerModeManager modeManager,
        WukongPlayerPawnManager playerPawnManager,
        WukongRpcCallbacks rpc,
        NetworkedEntityManager netManager,
        INetworkedComponentRegistry netComponentRegistry,
        IRelayClient relayClient,
        SystemUpdateLoop updateLoop,
        ISystemRegistry systemRegistry,
        ILogger logger)
        : base(world, netManager, netComponentRegistry, relayClient, updateLoop, logger)
    {
        _roomState = roomState;
        _playerRegistry = playerRegistry;
        _playerProperty = playerProperty;
        _modeManager = modeManager;
        _playerPawnManager = playerPawnManager;
        _rpc = rpc;
        _roomConfigArchetype = systemRegistry.RegisterArchetype(WukongCoreApi.RegisterRoomConfigArchetype);

        systemRegistry.AddSystem<SyncTamersSystem>();
        systemRegistry.AddSystem<UpdateMarkersSystem>();
        systemRegistry.AddSystem<DestroyDeadMonstersMarkersSystem>();
        systemRegistry.AddSystem(new SyncMonstersSystem(RelayClient));
    }

    [Obsolete("Ideally we should just be able to get rid of this entirely")]
    public void Refresh()
    {
        if (!IsRunning)
            return;

        // NOTE: This shouldn't depend on _roomState.InMatchmaking
        if (!_roomState.InMatchmaking && _playerRegistry.LocalPlayerState.IsSpectator)
        {
            _modeManager.HandleBecameSpectator(_playerRegistry.LocalPlayerState); // TODO: Called twice?
        }

        _modeManager.UpdatePlayerTeamUi(_playerRegistry.LocalPlayerState);
    }

    protected override void RunOnGameThread(Action action)
    {
        GameLoopPatch.QueueOnGameThread(action);
    }
    
    private void SpawnPlayersAlreadyInRoom()
    {
        // when joining game, spawn all players already in room
        foreach (var d in RelayClient.OtherPlayers)
        {
            Logging.LogDebug("Other player: {PlayerId}", d.Key);
            GameLoopPatch.QueueOnGameThread(() => AddPlayer(d.Value.PlayerId), "AddPlayer");
        }
    }
    
    private void AddPlayer(PlayerId playerId)
    {
        var playerState = _playerPawnManager.AddPlayerPawn(playerId);

        if (playerState != null)
        {
            var props = RelayClient.GetPlayerState(playerId)?.Properties;

            if (props == null)
            {
                Logging.LogError("Player properties are null");
                return;
            }

            // set IsSpectator if client should be (joining during fight)
            var isSpectator = playerState.IsSpectator;

            // set remote player property - IsSpectator
            if (RelayClient.IsMasterClient)
            {
                _playerProperty.SetRemotePlayerProperty(playerId, nameof(PlayerState.IsSpectator), isSpectator);
            }

            _modeManager.UpdatePlayerTeamUi(playerState);
        }
    }

    private void ChangeEquipment(PlayerId playerId, EquipmentState eq)
    {
        if (playerId == _playerRegistry.LocalPlayerState.PlayerId)
            return;

        if (!_playerRegistry.ConnectedPlayers.TryGetValue(playerId, out var player))
        {
            Logging.LogError("Player not found: {PlayerId}", playerId);
            return;
        }

        if (player.Pawn == null)
        {
            Logging.LogWarning("Failed to cast pawn to BGUCharacterCS");
            return;
        }

        EquipmentHelpers.SetRemoteActorEquipment(player.Pawn, eq);
    }

    private static readonly Dictionary<string, Action<PlayerState, object>> PlayerSetters = new();

    private static Action<T, object> CreateSetter<T>(string propertyName)
    {
        var property = typeof(T).GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
        if (property == null)
            throw new InvalidOperationException($"Property '{propertyName}' not found on {typeof(T).Name}.");

        // Create the lambda (T state, object value) => state.Property = (T)value;
        var stateParam = Expression.Parameter(typeof(T), "state");
        var valueParam = Expression.Parameter(typeof(object), "value");

        // Cast value to the correct type
        var convertedValue = Expression.Convert(valueParam, property.PropertyType);

        // Build the assignment: state.Property = (T)value;
        var body = Expression.Assign(Expression.Property(stateParam, property), convertedValue);

        // Compile the lambda expression
        return Expression.Lambda<Action<T, object>>(body, stateParam, valueParam).Compile();
    }
    
    private void SetPlayerProperties()
    {
        var player = GameUtils.GetControlledPawn();

        if (player == null)
        {
            Logging.LogError("Failed to get controlled pawn");
            return;
        }

        Logging.LogDebug("Setting initial player properties");

        _playerProperty.CachePlayerProperty(nameof(PlayerState.Location), player.GetActorLocation());
        _playerProperty.CachePlayerProperty(nameof(PlayerState.Rotation), player.GetActorRotation());

        // nickname
        var nickname = CmdLineParams.Instance.Nickname;
        _playerProperty.CachePlayerProperty(nameof(PlayerState.NickName), nickname);

        // equipment
        var eq = EquipmentHelpers.GetCurrentEquipmentStateForActor(player);
        _playerProperty.CachePlayerProperty(nameof(PlayerState.Equipment), eq);

        // attributes
        var attrs = BGU_DataUtil.GetReadOnlyData<IBUC_AttrContainer, BUC_AttrContainer>(player);
        foreach (var attr in Constants.SyncedAttributes)
        {
            var value = attrs.GetFloatValue(attr);
            _playerProperty.CachePlayerAttribute(attr, value);
        }

        // hp
        var hp = attrs.GetFloatValue(EBGUAttrFloat.Hp);
        _playerProperty.CachePlayerProperty(nameof(PlayerState.Hp), hp);

        _playerProperty.SetCachedPlayerProperties();
        Logging.LogDebug("Finished setting initial player properties");
    }
    
    public void UpdatePlayer(PlayerState playerState, float deltaTime)
    {
        playerState.UpdateMarkerPosition();

        if (playerState.TeleportFinishFrames >= 0)
        {
            if (playerState.TeleportFinishFrames == 0)
            {
                _rpc.SendTeleportFinish();
            }

            playerState.TeleportFinishFrames--;
        }
    }
    
    #region Event handlers
    
    protected override void OnPlayerPropertiesChangedHandler(PlayerId playerId, Dictionary<object, object?> changes)
    {
        base.OnPlayerPropertiesChangedHandler(playerId, changes);
        
        PlayerState playerState;

        if (playerId == RelayClient.LocalPlayer.PlayerId) // local player
        {
            if (!_playerRegistry.HasLocalPlayerState)
            {
                Logging.LogWarning("Local player state is null.");
                return;
            }

            playerState = _playerRegistry.LocalPlayerState;
        }
        else if (!_playerRegistry.ConnectedPlayers.TryGetValue(playerId, out playerState))
        {
            Logging.LogDebug("Player {Id} not found.", playerId); // TODO: Investigate why this is spammed
            return;
        }

        foreach (var kvp in changes)
        {
            if (kvp.Value == null)
                continue; // we don't really handle property removal

            if (kvp.Key is not string propertyName)
            {
                // ignore system properties
                continue;
            }

            // attributes have special treatment
            if (propertyName.StartsWith(Constants.AttributePrefix))
            {
                Logging.LogTrace("Assigning {Property} = {Value} for player {PlayerId}", propertyName, kvp.Value, playerId);

                var key = propertyName[Constants.AttributePrefix.Length..];

                if (!Enum.TryParse<EBGUAttrFloat>(key, out var attr))
                    throw new InvalidOperationException($"Failed to parse attribute key: {key}");

                playerState.Attributes[attr] = (float)kvp.Value;
                continue;
            }

            if (!PlayerSetters.TryGetValue(propertyName, out var setter))
            {
                setter = CreateSetter<PlayerState>(propertyName);
                PlayerSetters[propertyName] = setter;
            }

            if (kvp.Value is not (FVector or FRotator or float))
            {
                Logging.LogTrace("Assigning {Property} = {Value} for player {PlayerId}", propertyName, kvp.Value, playerId);
            }

            setter(playerState, kvp.Value);

            // special handlers for some properties
            switch (propertyName)
            {
                case nameof(PlayerState.Equipment):
                {
                    var eq = (EquipmentState)kvp.Value;
                    GameLoopPatch.QueueOnGameThread(() => ChangeEquipment(playerId, eq), "ChangeEquipment");
                    break;
                }
                case nameof(PlayerState.TeamId):
                {
                    var teamId = (int)kvp.Value;
                    GameLoopPatch.QueueOnGameThread(() => _modeManager.UpdatePlayerTeam(playerState, teamId));
                    break;
                }
                case nameof(PlayerState.IsSpectator):
                {
                    var isSpectator = (bool)kvp.Value;
                    Logging.LogDebug("Player {Id} spectator status changed: {Spectator}", playerId, isSpectator);

                    Utils.TryRunOnGameThread(() =>
                    {
                        if (isSpectator)
                        {
                            _modeManager.HandleBecameSpectator(playerState);
                        }
                        else
                        {
                            _modeManager.HandleStoppedBeingSpectator(playerState);
                        }
                    });

                    break;
                }
            }
        }
    }
    
    protected override void OnBeforeJoinedRoomHandler()
    {
        Logging.LogInformation("Synchronizer before joined room");
        
        int? teamId = null;
        if (RelayClient.LocalPlayer.Properties.TryGetValue(nameof(PlayerState.TeamId), out var teamIdUntyped))
            teamId = (int)teamIdUntyped;
        
        var controlledPawn = GameUtils.GetControlledPawn();

        if (controlledPawn.IsNullOrDestroyed())
        {
            Logging.LogError("Controlled pawn is null or destroyed.");
            return;
        }

        var data = BGU_DataUtil.GetReadOnlyData<IBUC_AttrContainer, BUC_AttrContainer>(controlledPawn);
        var initialHp = data.GetFloatValue(EBGUAttrFloat.Hp);
        var initialHpMaxBase = data.GetFloatValue(EBGUAttrFloat.HpMaxBase);

        _playerRegistry.LocalPlayerState = new PlayerState(RelayClient.PlayerId, controlledPawn, teamId, initialHp, initialHpMaxBase);

        // get nickname from Relay
        var playerNickname = (string)RelayClient.LocalPlayer.Properties.GetValueOrDefault(nameof(PlayerState.NickName), CmdLineParams.Instance.Nickname);
        _playerRegistry.LocalPlayerState.NickName = playerNickname;

        _playerRegistry.LocalPlayerState.IsSpectator = (bool)RelayClient.LocalPlayer.Properties.GetValueOrDefault(nameof(PlayerState.IsSpectator), false);

        SpawnPlayersAlreadyInRoom();
        _playerPawnManager.UpdateConnectedCount();
        
        // FIXME: Move to PVP
        if (!Constants.IsCoop)
        {
            var player = GameUtils.GetControlledPawn();
            SkillsUtils.DisableVigorSkill(player);
            SkillsUtils.DisableFaBaoSkill(player);
        }
#if TESTING
        BUC_SpeedCtrlData? speedCtrlData = BGU_DataUtil.GetUnPersistentReadOnlyData<IBUC_SpeedCtrlData, BUC_SpeedCtrlData>(GameUtils.GetControlledPawn()) as BUC_SpeedCtrlData;
        speedCtrlData?.SetSpeedInfo(10000, 10000, 10000);
#endif
        
        // FIXME: Move to PVP
        LobbyStatusWidget.Instance.SetMaxConnectedCount(_roomState.MaxPlayers);
        
        // FIXME: Move to Coop
        CoopStatusWidget.Instance.SetMaxConnectedCount(_roomState.MaxPlayers);
        
        base.OnBeforeJoinedRoomHandler();
    }

    protected override void OnAfterJoinedRoomHandler(Dictionary<object, object> initialState)
    {
        Logging.LogInformation("Synchronizer after joined room");
        base.OnAfterJoinedRoomHandler(initialState);
    }

    protected override void OnOtherPlayerJoinedHandler(PlayerId playerId, Dictionary<object, object> initialState)
    {
        base.OnOtherPlayerJoinedHandler(playerId, initialState);
        
        Logging.LogInformation("Player {PlayerId} entered the room", playerId);
        
        GameLoopPatch.QueueOnGameThread(() => AddPlayer(playerId), "AddPlayer");
    }
    
    protected override void OnOtherPlayerLeftHandler(PlayerId playerId)
    {
        base.OnOtherPlayerLeftHandler(playerId);
        
        var player = RelayClient.GetPlayerState(playerId)!;
        var nickname = (string)player.Properties.GetValueOrDefault(nameof(PlayerState.NickName), "Player");

        Logging.LogInformation("Player {Nickname} ({PlayerId}) left the room", nickname, playerId);

        if (_playerRegistry.ConnectedPlayers.Remove(playerId, out var playerState))
        {
            GameLoopPatch.QueueOnGameThread(() => _playerPawnManager.RemovePlayerPawn(playerState));
        }
        else
        {
            Logging.LogWarning("Player {Id} not in ConnectedPlayers.", playerId);
        }
    }
    
    protected override void OnEnterRoomRequest()
    {
        base.OnEnterRoomRequest();
        
        SetPlayerProperties();
    }
    
    protected override void OnExitRoomRequest()
    {
        base.OnExitRoomRequest();
        
        Logging.LogInformation("Exit room callback...");

        // clear the chat window
        ChatWidget.Instance.ClearMessages();

        // destroy all connected players
        foreach (var player in _playerRegistry.ConnectedPlayers.Values)
        {
            _playerPawnManager.RemovePlayerPawn(player);
        }

        // clear state
        _playerRegistry.ConnectedPlayers.Clear();
        Utils.TryRunOnGameThread(TamerUtils.ClearEcsMonsters);
        _playerRegistry.ResetLocalPlayer();

        Logging.LogInformation("Exited.");
    }
    
    #endregion
}
