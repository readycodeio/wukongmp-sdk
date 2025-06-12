using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using b1;
using BtlB1;
using BtlShare;
using CSharpModBase;
using LiteNetLib;
using ReadyM.Relay.Client;
using ReadyM.Relay.Common;
using ReadyM.Relay.Common.Wukong.Components;
using UnrealEngine.Engine;
using UnrealEngine.Runtime;
using WukongMp.Api.Configuration;
using WukongMp.Api.Old.Enums;
using WukongMp.Api.Old.State;
using WukongMp.Api.UI;
using WukongMp.Api.WukongUtils;

namespace WukongMp.Api.Old;

public sealed class WukongClient
{
    public RelayClient RelayClient => WukongMpMod.Instance.RelayClient;
    private UserId PeerId => RelayClient.PeerId; // is -1 before joining room
    public bool IsMasterClient => WukongMpMod.Instance.IsMasterClient;
    public bool ConnectedAndInRoom => WukongMpMod.Instance.RelayClient.InRoom;

    private readonly Action _beforeJoinedRoomCallback;
    private readonly Action _afterJoinedRoomCallback;
    private readonly Action<UserId> _playerJoinedCallback;

    public WukongChatter WukongChat { get; }
    public LobbyManager LobbyManager { get; }

    private PlayerState? _localPlayerState;

    public PlayerState LocalPlayerState
    {
        get
        {
            if (_localPlayerState == null)
            {
                throw new InvalidOperationException("Local player state is null");
            }

            return _localPlayerState;
        }
        private set => _localPlayerState = value;
    }

    public RoomStateProxy RoomState { get; }

    public readonly Dictionary<UserId, PlayerState> ConnectedPlayers = new();

    public IEnumerable<PlayerState> AllConnectedPlayers
        => ConnectedPlayers.Values.Append(LocalPlayerState);

    public IEnumerable<PlayerState> SpectatingPlayers
        => ConnectedPlayers.Values.Where(p => p.IsSpectator).Concat(LocalPlayerState.IsSpectator ? [LocalPlayerState] : []);

    public IEnumerable<PlayerState> AllPvPPlayers
        => ConnectedPlayers.Values.Where(p => !p.IsSpectator).Concat(LocalPlayerState.IsSpectator ? [] : [LocalPlayerState]);

    public event Action<UserId, EquipmentState>? OnEquipmentChange;
    public event Action<string, bool, int>? OnReadinessChange;
    public event Action<PlayerState, int>? OnTeamChange;
    public event Action<PlayerState>? OnPlayerLeft;
    public event Action? OnBeforeJoinRoom;

    public WukongClient(Action onBeforeJoinedRoom, Action onAfterJoinedRoom, Action<UserId> playerJoinedCallback)
    {
        // TODO: Figure out ownership
        WukongChat = new WukongChatter(this, WukongMpMod.Instance);
        LobbyManager = new LobbyManager(this, WukongMpMod.Instance);
        RoomState = new RoomStateProxy(RelayClient);

        _beforeJoinedRoomCallback = onBeforeJoinedRoom;
        _afterJoinedRoomCallback = onAfterJoinedRoom;
        _playerJoinedCallback = playerJoinedCallback;

        ConfigureRelay();
    }

    ~WukongClient()
    {
        Logging.LogInformation("WukongClient finalizer called");
        StopRelayClient();

        RelayClient.OnPlayerPropertiesChanged -= OnPlayerPropertiesChanged;
        RelayClient.OnBeforeJoinedRoom -= OnBeforeJoinedRoomHandler;
        RelayClient.OnAfterJoinedRoom -= OnAfterJoinedRoomHandler;
        RelayClient.OnDisconnected -= OnDisconnectedHandler;
        RelayClient.OnOtherPlayerJoined -= OtherPlayerJoinedRoomHandler;
        RelayClient.OnOtherPlayerLeft -= OnPlayerLeftRoomHandler;
    }

    public void RegisterPlayer(PlayerState state)
    {
        Logging.LogDebug("Registering player {PlayerId}", state.PeerId);
        ConnectedPlayers.Add(state.PeerId, state);
    }

    public PlayerState? GetPlayerByActor(AActor? actor)
    {
        if (actor == null)
            return null;

        return actor == LocalPlayerState.Pawn
            ? LocalPlayerState
            : ConnectedPlayers.FirstOrDefault(x => x.Value!.Pawn == actor).Value;
    }

    [Obsolete]
    public PlayerState? GetPlayerById(UserId userId)
    {
        return userId == LocalPlayerState.PeerId
            ? LocalPlayerState
            : ConnectedPlayers.GetValueOrDefault(userId);
    }

    public void SetMasterClient(string newMasterName)
    {
        if (IsMasterClient)
        {
            var newMasterPlayer = AllConnectedPlayers.FirstOrDefault(x => x.NickName == newMasterName);
            if (newMasterPlayer != null)
            {
                RoomState.MasterClientId = newMasterPlayer.PeerId;
                WukongChat.SendServerMessage("MasterClient", newMasterName);
            }
            else
            {
                Logging.LogError("Player {PlayerName} not found", newMasterName);
            }
        }
    }

    public void SetReadyState(bool isReady)
    {
        CachePlayerProperty(nameof(PlayerState.IsReadyForPvP), isReady);
    }

    public void SwitchReadyStateMulti()
    {
        if (ConnectedAndInRoom && RoomState is { InPvP: false, InMatchmaking: false } && ConnectedPlayers.Count > 0)
        {
            SwitchReadyState();
        }
    }

    public void SwitchReadyStateSingle()
    {
        if (ConnectedAndInRoom && RoomState is { InPvP: false, InMatchmaking: false } && ConnectedPlayers.Count == 0)
        {
            SwitchReadyState();
        }
    }

    private void SwitchReadyState()
    {
        var isReady = LocalPlayerState.IsReadyForPvP;
        SetReadyState(!isReady);
        WukongMP.Instance.SwitchReadyState(!isReady);
    }

    public void SwitchTeam(bool force = false)
    {
        if (force || (ConnectedAndInRoom && !LocalPlayerState.IsReadyForPvP && RoomState is { InPvP: false, InMatchmaking: false }))
        {
            var teamId = PvPUtils.GetOppositeTeam(LocalPlayerState.TeamId);
            CachePlayerProperty(nameof(PlayerState.TeamId), teamId);
        }
    }

    public void CheckRoundEndCondition()
    {
        if (!IsMasterClient || !RoomState.InPvP)
        {
            return;
        }

        // check if all players but one are dead
        var players = AllPvPPlayers.ToList();
        var aliveTeamIds = players.Where(p => !p.IsDead).Select(x => x.TeamId).ToList();

        var aliveMonsters = new List<int>();
        WukongMpMod.Instance.World.Query<HpComponent, TeamComponent>().ForEachEntity((ref hp, ref team, _) =>
        {
            if (hp.Hp <= 0)
                return;

            aliveMonsters.Add(team.TeamId);
        });

        var alivePlayersTeams = aliveTeamIds.Concat(aliveMonsters).ToList();

        var aliveTeamCount = alivePlayersTeams.Distinct().Count();

        var aliveTeamPlayers = alivePlayersTeams
            .GroupBy(teamId => teamId)
            .Select(group => new { TeamId = group.Key, Count = group.Count() })
            .OrderByDescending(item => item.Count).ToList();

        if (aliveTeamIds.Count == 0)
        {
            Logging.LogInformation("All players are dead, ending round");
            var aliveTeamId = aliveTeamPlayers.Count > 0 ? aliveTeamPlayers[0].TeamId : Constants.DrawTeamId;
            if (alivePlayersTeams.Count == 0)
            {
                Task.Run(async () => await LobbyManager.EndRoundAsync(PvPUtils.GetOppositeTeam(aliveTeamId)));
            }
            else
            {
                Task.Run(async () => await LobbyManager.EndRoundAsync(aliveTeamId));
            }

            return;
        }

        if (aliveTeamCount == 1)
        {
            Logging.LogInformation("One team with alive players, ending round");
            var winner = players.First(p => !p.IsDead);
            Task.Run(async () => await LobbyManager.EndRoundAsync(winner.TeamId));
        }
    }

    public void EnterPvP()
    {
        if (!IsMasterClient)
            return;

        if (!RelayClient.InRoom)
        {
            Logging.LogError("No room joined.");
            return;
        }

        RoomState.InPvP = true;
    }

    public void ExitPvP()
    {
        if (!IsMasterClient)
            return;

        if (!RelayClient.InRoom)
        {
            Logging.LogError("No room joined.");
            return;
        }

        RoomState.InPvP = false;
    }

    public void OnPlayerReadinessChanged(string playerNickname, bool isReady)
    {
        var playersReady = ConnectedPlayers.Values.Count(x => x.IsReadyForPvP) + (LocalPlayerState.IsReadyForPvP ? 1 : 0);
        OnReadinessChange?.Invoke(playerNickname, isReady, playersReady);
    }

    public void Reconnect()
    {
        Logging.LogInformation("Attempting to reconnect...");
        _ = Task.Run(async () =>
        {
            StopRelayClient();
            await Task.Delay(Constants.ReconnectDelayMs);
            StartClient();
        });
    }

    private void ConfigureRelay()
    {
        RelayClient.RegisterType(typeof(DamageNumParam), SerializationHelpers.SerializeDamageNumParam, SerializationHelpers.DeserializeDamageNumParam);
        RelayClient.RegisterType(typeof(EquipmentState), EquipmentState.Serialize, EquipmentState.Deserialize);
        RelayClient.RegisterType(typeof(FRotator), SerializationHelpers.SerializeFRotator, SerializationHelpers.DeserializeFRotator);
        RelayClient.RegisterType(typeof(FVector), SerializationHelpers.SerializeFVector, SerializationHelpers.DeserializeFVector);

        RelayClient.OnAfterJoinedRoom += OnAfterJoinedRoomHandler;
        RelayClient.OnBeforeJoinedRoom += OnBeforeJoinedRoomHandler;
        RelayClient.OnDisconnected += OnDisconnectedHandler;
        RelayClient.OnOtherPlayerJoined += OtherPlayerJoinedRoomHandler;
        RelayClient.OnOtherPlayerLeft += OnPlayerLeftRoomHandler;
        RelayClient.OnPlayerPropertiesChanged += OnPlayerPropertiesChanged;
    }

    public void StartClient()
    {
        OnBeforeJoinRoom?.Invoke();
        WukongMpMod.Instance.Start();
        Logging.LogInformation("Client started");
    }

    public void StopRelayClient()
    {
        Logging.LogInformation("Stopping relay client...");

        if (GameUtils.IsWorldValid())
        {
            UnsubscribeFromPlayerEvents();
        }

        WukongMpMod.Instance.Stop();

        // clear the chat window
        ChatWidget.Instance.ClearMessages();

        // destroy all connected players
        foreach (var player in ConnectedPlayers.Values)
        {
            WukongMP.Instance.RemovePlayer(player);
        }

        // clear state
        ConnectedPlayers.Clear();
        Utils.TryRunOnGameThread(TamerUtils.ClearEcsMonsters);
        _localPlayerState = null;

        Logging.LogInformation("Stopped client.");
    }

    public IEnumerable<Player> GetOtherPlayersInRoom()
    {
        foreach (var (playerId, player) in RelayClient.OtherPlayers)
        {
            Logging.LogDebug("Other player: {PeerId}", playerId);
            yield return player;
        }
    }

    private void SetOrGetRoomProps()
    {
        Logging.LogInformation("Joining or creating private room");

        if (!IsMasterClient)
        {
            Logging.LogInformation("Not master client, skipping initialization");
            return;
        }

        // TODO: set from initial room properties (via server allocation request)
        RoomState.GameMode = GameMode.Private;
        RoomState.RoundWinners = [];
        RoomState.BotsEnabled = true; // TODO: Selector
        RoomState.MaxPlayers = 10;
    }

    public bool IsSkillEnabled(int skillId)
    {
        if (skillId == Constants.ImmobilizeSkillId && !RoomState.ImmobilizeAllowed)
        {
            return false;
        }

        // more skills here
        return true;
    }

    public void CacheEquipmentChange(EquipPosition position, int newEq)
    {
        LocalPlayerState.Equipment.SetEquipment(position, newEq);
        CachePlayerProperty(nameof(PlayerState.Equipment), LocalPlayerState.Equipment);
    }

    public void StartPvP()
    {
        if (!IsMasterClient)
        {
            return;
        }

        // clear previous round winners
        RoomState.RoundWinners = [];

        Task.Run(LobbyManager.StartRoundAsync);
    }

    private ConcurrentDictionary<string, object> _playerProperties = new();

    private ConcurrentDictionary<string, object> _playerPropertiesRo = new();

    private readonly object _playerPropertiesLock = new();

    [Obsolete]
    public void SetCachedPlayerProperties()
    {
        lock (_playerPropertiesLock)
        {
            (_playerProperties, _playerPropertiesRo) = (_playerPropertiesRo, _playerProperties);

            if (_playerPropertiesRo.Count == 0)
                return;

            var hashtable = new Dictionary<object, object?>();
            foreach (var (key, value) in _playerPropertiesRo)
            {
                hashtable[key] = value;
            }

            _playerPropertiesRo.Clear();
            RelayClient.OpSetCustomPropertiesOfActor(PeerId, hashtable);
        }
    }

    public void CachePlayerProperty(string key, object value)
    {
        _playerProperties[key] = value;
        if (!(value is FVector || value is FRotator || key == nameof(PlayerState.TurnInplaceRemainAngle)))
        {
            Logging.LogTrace("Set player property: {Property} = {Value}", key, value);
        }
    }

    public void CachePlayerAttribute(EBGUAttrFloat attr, float value)
    {
        // if HpMax changed, update Hp too
        if (IsMasterClient && _localPlayerState is not null && attr == EBGUAttrFloat.HpMaxBase)
        {
            var data = BGU_DataUtil.GetReadOnlyData<IBUC_AttrContainer, BUC_AttrContainer>(LocalPlayerState.Pawn);
            var currentHp = data.GetFloatValue(EBGUAttrFloat.Hp);

            LocalPlayerState.Hp = currentHp;
            CachePlayerProperty(nameof(PlayerState.Hp), currentHp);
        }

        CachePlayerProperty($"{Constants.AttributePrefix}{attr}", value);
    }

    public void SetRemotePlayerProperty(UserId peerId, string key, object value)
    {
        if (!IsMasterClient)
        {
            Logging.LogError("Only room owner can send remote player properties.");
            return;
        }

        var hashtable = new Dictionary<object, object?>
        {
            [key] = value
        };

        Logging.LogDebug("Sending remote player property: {Property} = {Value}", key, value);

        RelayClient.OpSetCustomPropertiesOfActor(peerId, hashtable);
    }

    private void SubscribeToPlayerEvents()
    {
        var events = BUS_EventCollectionCS.Get(LocalPlayerState.Pawn);
        events.Evt_BuffAdd += WukongMpMod.Instance.SendAddBuffHandler;
        events.Evt_BuffRemove += WukongMpMod.Instance.SendRemoveBuffHandler;
        events.Evt_BuffRemoveImmediately += WukongMpMod.Instance.HandleBuffRemoveImmediately;
        events.Evt_BuffAllRemove += WukongMpMod.Instance.SendRemoveAllBuffsHandler;
    }

    private void UnsubscribeFromPlayerEvents()
    {
        var myPawn = GameUtils.GetControlledPawn();

        if (myPawn == null)
            return;

        var events = BUS_EventCollectionCS.Get(myPawn);

        if (events != null)
        {
            events.Evt_BuffAdd -= WukongMpMod.Instance.SendAddBuffHandler;
            events.Evt_BuffRemove -= WukongMpMod.Instance.SendRemoveBuffHandler;
            events.Evt_BuffRemoveImmediately -= WukongMpMod.Instance.HandleBuffRemoveImmediately;
            events.Evt_BuffAllRemove -= WukongMpMod.Instance.SendRemoveAllBuffsHandler;
        }
    }

    private int GetSmallerTeamId()
    {
        Dictionary<int, int> teamsCount = [];
        var team1Id = Constants.AvailableTeamIds[0];
        var team2Id = Constants.AvailableTeamIds[1];
        teamsCount[team1Id] = 0;
        teamsCount[team2Id] = 0;

        foreach (var player in GetOtherPlayersInRoom())
        {
            if (player.Properties.TryGetValue(nameof(PlayerState.TeamId), out var assignedTeamId))
            {
                teamsCount[(int)assignedTeamId]++;
            }
        }

        return teamsCount[team1Id] > teamsCount[team2Id] ? team2Id : team1Id;
    }

    private void OnBeforeJoinedRoomHandler()
    {
        SetOrGetRoomProps();

        Logging.LogInformation("Joined room");

        var teamId = (int)RelayClient.LocalPlayer.Properties.GetValueOrDefault(nameof(PlayerState.TeamId), GetSmallerTeamId());

        var controlledPawn = GameUtils.GetControlledPawn();

        if (controlledPawn.IsNullOrDestroyed())
        {
            Logging.LogError("Controlled pawn is null or destroyed.");
            return;
        }

        var data = BGU_DataUtil.GetReadOnlyData<IBUC_AttrContainer, BUC_AttrContainer>(controlledPawn);
        var initialHp = data.GetFloatValue(EBGUAttrFloat.Hp);
        var initialHpMaxBase = data.GetFloatValue(EBGUAttrFloat.HpMaxBase);

        LocalPlayerState = new PlayerState(PeerId, controlledPawn, teamId, initialHp, initialHpMaxBase);
        CachePlayerProperty(nameof(PlayerState.TeamId), teamId);

        // get nickname from Relay
        var playerNickname = (string)RelayClient.LocalPlayer.Properties.GetValueOrDefault(nameof(PlayerState.NickName), CmdLineParams.Instance.Nickname);
        LocalPlayerState.NickName = playerNickname;

        // same for IsReadyForPvP and IsSpectator
        LocalPlayerState.IsReadyForPvP = (bool)RelayClient.LocalPlayer.Properties.GetValueOrDefault(nameof(PlayerState.IsReadyForPvP), false);
        LocalPlayerState.IsSpectator = (bool)RelayClient.LocalPlayer.Properties.GetValueOrDefault(nameof(PlayerState.IsSpectator), false);

        SubscribeToPlayerEvents();
        _beforeJoinedRoomCallback.Invoke();

        WukongChat.SendServerMessage("PlayerJoined", LocalPlayerState.NickName);
    }

    private void OnAfterJoinedRoomHandler()
    {
        _afterJoinedRoomCallback.Invoke();
    }

    public void OnDisconnectedHandler(DisconnectReason reason)
    {
        Logging.LogInformation("Disconnected");
        if (reason == DisconnectReason.DisconnectPeerCalled)
        {
            Logging.LogInformation("Disconnected: {Cause}", reason);
        }
        else
        {
            Logging.LogWarning("Disconnected: {Cause}", reason);
        }

        if (reason is DisconnectReason.Timeout or DisconnectReason.RemoteConnectionClose)
        {
            Reconnect();
        }
    }

    private void OtherPlayerJoinedRoomHandler(UserId playerId)
    {
        Logging.LogInformation("Player {PlayerId} entered the room", playerId);
        _playerJoinedCallback.Invoke(playerId);
    }

    private void OnPlayerLeftRoomHandler(UserId playerId)
    {
        var player = RelayClient.GetPlayerState(playerId)!;
        var nickname = (string)player.Properties.GetValueOrDefault(nameof(PlayerState.NickName), "Player");

        Logging.LogInformation("Player {Nickname} ({PlayerId}) left the room", nickname, playerId);

        if (ConnectedPlayers.Remove(playerId, out var playerState))
        {
            OnPlayerLeft?.Invoke(playerState);
        }
        else
        {
            Logging.LogWarning("Player {Id} not in ConnectedPlayers.", playerId);
        }

        if (IsMasterClient)
        {
            WukongChat.SendServerMessage("PlayerLeft", nickname);

            _ = Task.Run(async () =>
            {
                await Task.Delay(Constants.PlayerTtlMs);
                CheckRoundEndCondition();
            });
        }
    }

    private void OnPlayerPropertiesChanged(UserId peerId, Dictionary<object, object?> changes)
    {
        PlayerState playerState;

        if (peerId == RelayClient.LocalPlayer.PeerId) // local player
        {
            if (_localPlayerState == null)
            {
                Logging.LogWarning("Local player state is null.");
                return;
            }

            playerState = LocalPlayerState;
        }
        else if (!ConnectedPlayers.TryGetValue(peerId, out playerState))
        {
            Logging.LogDebug("Player {Id} not found.", peerId); // TODO: Investigate why this is spammed
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
                Logging.LogTrace("Assigning {Property} = {Value} for player {PlayerId}", propertyName, kvp.Value, peerId);

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
                Logging.LogTrace("Assigning {Property} = {Value} for player {PlayerId}", propertyName, kvp.Value, peerId);
            }

            setter(playerState, kvp.Value);

            // special handlers for some properties
            switch (propertyName)
            {
                case nameof(PlayerState.Equipment):
                    OnEquipmentChange?.Invoke(peerId, (EquipmentState)kvp.Value);
                    break;
                case nameof(PlayerState.IsReadyForPvP):
                    var state = RelayClient.GetPlayerState(peerId);

                    if (state == null)
                    {
                        Logging.LogError("Player {Id} not found.", peerId);
                        continue;
                    }

                    var targetPlayerNickname = (string)state.Properties[nameof(PlayerState.NickName)];
                    OnPlayerReadinessChanged(targetPlayerNickname, (bool)kvp.Value);
                    continue;
                case nameof(PlayerState.TeamId):
                    OnTeamChange?.Invoke(playerState, (int)kvp.Value);
                    continue;
                case nameof(PlayerState.IsSpectator):
                {
                    var isSpectator = (bool)kvp.Value;
                    Logging.LogDebug("Player {Id} spectator status changed: {Spectator}", peerId, isSpectator);

                    Utils.TryRunOnGameThread(() =>
                    {
                        if (isSpectator)
                        {
                            WukongMP.Instance.HandleBecameSpectator(playerState);
                        }
                        else
                        {
                            WukongMP.Instance.HandleStoppedBeingSpectator(playerState);
                        }
                    });

                    break;
                }
            }
        }
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
}