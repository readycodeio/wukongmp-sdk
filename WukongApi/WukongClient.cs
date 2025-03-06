using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using b1;
using BtlB1;
using BtlShare;
using CSharpModBase;
using Photon.Client;
using Photon.Realtime;
using UnrealEngine.Engine;
using UnrealEngine.Runtime;
using WukongApi.State;
using PlayerState = WukongApi.State.PlayerState;

namespace WukongApi
{
    public class WukongClient : IConnectionCallbacks, IOnEventCallback, IMatchmakingCallbacks, IInRoomCallbacks
    {
        internal readonly RealtimeClient PhotonClient = new RealtimeClient();
        private readonly AuthenticationValues _authValues;
        private readonly TypedLobby _lobby = new TypedLobby("pvpLobby", LobbyType.Default);

        private const char MonsterHashtableKeySeparator = ';';

        private GameMode _gameMode;
        private string _roomName;
        private int _playersPerTeam;

        private bool _isExit;
        private bool _inPvP;

        public bool ShouldEnableMultiplayer => _authValues != null;
        protected int PhotonId => PhotonClient.LocalPlayer.ActorNumber;
        public bool IsMasterClient => PhotonClient.CurrentRoom?.MasterClientId == PhotonId;
        public bool Ready => PhotonClient.IsConnectedAndReady;

        private readonly Action _joinedRoomCallback;
        private readonly Action<Player> _playerJoinedCallback;
        public event Action<int, MontageCallbackData> OnMontageCallback;
        public event Action<int, MonsterMontageCallbackData> OnMonsterMontageCallback;
        public event Action<int, string, string, int, float, float, float> OnUnitSpawn;
        public event Action<string> OnMonsterWakeUp;
        public event Action<int, EquipmentState> OnEquipmentChange;
        public event Action<string, bool, int> OnReadinessChange;
        public event Action<PlayerState, int> OnTeamChange;
        public event Action<PlayerState> OnPlayerLeft;
        public event Action<int> OnPlayerRebirth;
        public event Action<int> OnKillPlayer;
        public event Action<FVector, FRotator> OnSetPlayerTransform;
        public event Action OnBeforeJoinRoom;
        public event Action<DamageNumParam> OnDamageNum;
        public event Action<int, ESkillDirection> OnPhantomRush;
        public event Action<int> OnExitPhantomRush;
        public event Action<int, int, ImmobilizeActionType, bool> OnHandleImmobilize;
        public event Action<int, int> OnTargetSet;

        public WukongChatter WukongChat { get; private set; }
        public LobbyManager LobbyManager { get; private set; }

        public PlayerState LocalPlayerState { get; protected set; }
        public RoomState CurrentRoomState { get; }

        public readonly Dictionary<int, PlayerState> ConnectedPlayers = new Dictionary<int, PlayerState>();
        public readonly Dictionary<string, MonsterState> SyncedMonsters = new Dictionary<string, MonsterState>();

        public IEnumerable<PlayerState> AllConnectedPlayers
            => ConnectedPlayers.Values.Append(LocalPlayerState);

        private readonly List<WukongClientClone> _photonClones = new List<WukongClientClone>();

        public void RegisterPlayer(PlayerState state)
        {
            Logging.LogDebug($"Registering player {state.PhotonId}");
            ConnectedPlayers.Add(state.PhotonId, state);
        }

        public PlayerState GetByActor(AActor actor)
        {
            if (actor == LocalPlayerState.Pawn)
                return LocalPlayerState;
            var kvp = ConnectedPlayers.FirstOrDefault(x => x.Value.Pawn == actor);
            return kvp.Value;
        }

        public PlayerState GetById(int playerId)
        {
            if (playerId == LocalPlayerState.PhotonId)
                return LocalPlayerState;
            return ConnectedPlayers.GetValueOrDefault(playerId);
        }

        public MonsterState GetByTamerActor(BUTamerActor owner)
        {
            var kvp = SyncedMonsters.FirstOrDefault(x => x.Value.Pawn == owner);
            return kvp.Value;
        }

        public void SetReadyState(bool isReady)
        {
            CachePlayerProperty(nameof(PlayerState.IsReadyForPvP), isReady);
        }

        public void SwitchReadyState()
        {
            if (PhotonClient.InRoom && !_inPvP)
            {
                var isReady = LocalPlayerState.IsReadyForPvP;
                SetReadyState(!isReady);
                WukongMP.Instance.SwitchReadyState(!isReady);
            }
        }

        public void SwitchTeam()
        {
            if (PhotonClient.InRoom && !LocalPlayerState.IsReadyForPvP && !_inPvP)
            {
                var teamId = (LocalPlayerState.TeamId == Constants.AvailableTeamIds[0]) ? Constants.AvailableTeamIds[1] : Constants.AvailableTeamIds[0];
                CachePlayerProperty(nameof(PlayerState.TeamId), teamId);
            }
        }

        public MonsterState GetMonsterByCharacter(BGUCharacterCS owner)
        {
            var kvp = SyncedMonsters.FirstOrDefault(x => x.Value.Pawn.GetMonster() == owner);
            return kvp.Value;
        }

        public void RemoveMonster(string monsterGuid)
        {
            SyncedMonsters.Remove(monsterGuid);
        }

        public WukongClient(Action onJoinedRoom, Action<Player> playerJoinedCallback)
        {
            _authValues = ParseCmdLineArgs();
            if (!ShouldEnableMultiplayer)
                return;

            WukongChat = new WukongChatter(this);
            CurrentRoomState = new RoomState(this);
            _joinedRoomCallback = onJoinedRoom;
            _playerJoinedCallback = playerJoinedCallback;
        }

        ~WukongClient()
        {
            PhotonClient.Disconnect();
            PhotonClient.RemoveCallbackTarget(this);
        }

        private AuthenticationValues ParseCmdLineArgs()
        {
            var cmd = USystemLibrary.GetCommandLine();

            Logging.LogDebug($"Command line: {cmd}");

            var tokenMatch = Regex.Match(cmd, $@"-access_token ""?({Constants.JsonCompactSerializationRegex})""?");

            string accessToken;
            if (tokenMatch.Success)
            {
                accessToken = tokenMatch.Groups[1].Value;
            }
            else
            {
                Logging.LogError("Access token not provided. Launch the game from the ReadyM Launcher.");
                return null;
            }

            // this can be either a private match (-room_name "name") or a quick match (-quick_match 1/3/5)

            var roomNameMatch = Regex.Match(cmd, @"-room_name ""([a-zA-Z0-9_\- ]+)""|-room_name ([a-zA-Z0-9_\-]+)");
            if (roomNameMatch.Success)
            {
                // private match
                _roomName = roomNameMatch.Groups[1].Success ? roomNameMatch.Groups[1].Value : roomNameMatch.Groups[2].Value;
                _gameMode = GameMode.Private;
            }
            else
            {
                var quickMatchMatch = Regex.Match(cmd, @"-quick_match (\d)");
                if (quickMatchMatch.Success)
                {
                    // quick match
                    var rounds = int.Parse(quickMatchMatch.Groups[1].Value);
                    _gameMode = GameMode.XvX;
                    _playersPerTeam = rounds;
                }
                else
                {
                    Logging.LogError("Room name not provided. Launch the game from the ReadyM Launcher.");
                    return null;
                }
            }

            var authValues = new AuthenticationValues
            {
                AuthType = CustomAuthenticationType.Custom
            };
            authValues.AddAuthParameter("access_token", accessToken);
            return authValues;
        }

        public void OnEvent(EventData photonEvent)
        {
            switch (photonEvent.Code)
            {
                case 1:
                    // unit spawn
                    var unitData = (UnitSpawnData)photonEvent.CustomData;
                    OnUnitSpawn?.Invoke(photonEvent.Sender, unitData.Guid, unitData.Name, unitData.TeamId, unitData.X, unitData.Y, unitData.Z);
                    break;
                case 2:
                    // montage callback
                    var montData = (MontageCallbackData)photonEvent.CustomData;
                    OnMontageCallback?.Invoke(photonEvent.Sender, montData);
                    break;
                case 3:
                    // monster properties
                    ApplyMonsterMove(photonEvent.CustomData as PhotonHashtable);
                    break;
                case 4:
                    // montage callback
                    var monsterMontageData = (MonsterMontageCallbackData)photonEvent.CustomData;
                    OnMonsterMontageCallback?.Invoke(photonEvent.Sender, monsterMontageData);
                    break;
                case 5:
                    // monster wake up
                    var guid = (string)photonEvent.CustomData;
                    OnMonsterWakeUp?.Invoke(guid);
                    break;
                case 6:
                    // damage num
                    var damageNumParam = (DamageNumParam)photonEvent.CustomData;
                    OnDamageNum?.Invoke(damageNumParam);
                    break;
                case 7:
                {
                    // player rebirth
                    var playerId = (int)photonEvent.CustomData;
                    OnPlayerRebirth?.Invoke(playerId);
                    break;
                }
                case 8:
                    // PvP event
                    var ev = (int[])photonEvent.CustomData;
                    HandlePvPEvent((PvPEvent)ev[0], ev[1]);
                    break;
                case 9:
                    // kill player
                    var id = (int)photonEvent.CustomData;
                    OnKillPlayer?.Invoke(id);
                    break;
                case 10:
                    // player transform
                    var playerData = (PlayerTransformData)photonEvent.CustomData;
                    if (playerData.PlayerId == LocalPlayerState.PhotonId)
                        OnSetPlayerTransform?.Invoke(playerData.Location, playerData.Rotation);
                    break;
                case 11:
                    // start phantom rush
                    var direction = (ESkillDirection)photonEvent.CustomData;
                    OnPhantomRush?.Invoke(photonEvent.Sender, direction);
                    break;
                case 12:
                    // immobilize
                    var immobilizeData = (ImmobilizeData)photonEvent.CustomData;
                    OnHandleImmobilize?.Invoke(immobilizeData.PlayerId, immobilizeData.OtherPlayerId, immobilizeData.ImmobilizeActionType, immobilizeData.GreatSageTalentActiveBuff);
                    break;
                case 13:
                    // target
                    var targetId = (int)photonEvent.CustomData;
                    OnTargetSet?.Invoke(photonEvent.Sender, targetId);
                    break;
                case 14:
                    // exit phantom rush
                    var phantomRushPlayerId = (int)photonEvent.CustomData;
                    OnExitPhantomRush?.Invoke(phantomRushPlayerId);
                    break;
            }
        }

        private void HandlePvPEvent(PvPEvent ev, int winnerTeamId)
        {
            Logging.LogDebug($"Received PvP event: {ev}");

            switch (ev)
            {
                case PvPEvent.RoundStart:
                    Task.Run(GameUtils.ShowPvPCountDown);
                    WukongMP.Instance.StartRound();
                    WukongMP.Instance.EnablePvP();
                    EnterPvP();
                    break;
                case PvPEvent.RoundEnd:
                    WukongMP.Instance.DisablePvP();
                    WukongMP.Instance.EndRound();

                    if (winnerTeamId == Constants.DrawTeamId)
                    {
                        GameUtils.ShowTip($"Round ended: Draw");
                    }
                    else
                    {
                        GameUtils.ShowTip($"Round ended. Team {GameUtils.GetTeamName(winnerTeamId)} won");
                    }

                    if (winnerTeamId == Constants.DrawTeamId)
                        return;

                    var winner = AllConnectedPlayers.FirstOrDefault(x => x.TeamId == winnerTeamId);
                    if (winner is null)
                    {
                        Logging.LogError("No winner found.");
                        return;
                    }

                    if (winner.TeamId == LocalPlayerState.TeamId)
                    {
                        GameUtils.PlayBossDefeatedSound();
                    }

                    break;
                case PvPEvent.TournamentEnd:
                {
                    if (winnerTeamId == Constants.DrawTeamId)
                    {
                        GameUtils.ShowTip("Draw");
                    }
                    else
                    {
                        GameUtils.ShowTip($"Winner: Team {GameUtils.GetTeamName(winnerTeamId)}");
                    }

                    Task.Run(async () =>
                    {
                        await Task.Delay(2000);
                        WukongMP.Instance.EndTurnament(winnerTeamId);
                        ExitPvP();
                        SetReadyState(false);
                    });

                    break;
                }
                case PvPEvent.ResetStats:
                    if (!LocalPlayerState.IsDead)
                    {
                        Utils.TryRunOnGameThread(() =>
                        {
                            var events = BUS_EventCollectionCS.Get(LocalPlayerState.Pawn);
                            events.Evt_TriggerTeleportResetPlayer.Invoke();
                        });
                    }

                    if (IsMasterClient)
                    {
                        // reset other players' Hp to HpMax if they are not dead
                        foreach (var (key, state) in ConnectedPlayers)
                        {
                            if (!state.IsDead)
                            {
                                var attrContainer = (BUC_AttrContainer)BGU_DataUtil.GetReadOnlyData<IBUC_AttrContainer, BUC_AttrContainer>(state.Pawn);
                                if (attrContainer != null)
                                {
                                    var hpMax = attrContainer.GetFloatValue(EBGUAttrFloat.HpMax);
                                    attrContainer.SetFloatValue(EBGUAttrFloat.Hp, hpMax);
                                    state.Hp = hpMax;
                                    SetRemotePlayerProperty(key, nameof(PlayerState.Hp), state.Hp);
                                }
                            }
                        }
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(ev), ev, null);
            }
        }

        private void EnterPvP()
        {
            _inPvP = true;
            if (IsMasterClient)
            {
                PhotonClient.CurrentRoom.IsOpen = false;
            }
        }

        private void ExitPvP()
        {
            _inPvP = false;
            if (IsMasterClient)
            {
                PhotonClient.CurrentRoom.IsOpen = true;
            }
        }

        private void OnPlayerReadinessChanged(Player player, bool isReady)
        {
            var playersReady = ConnectedPlayers.Values.Count(x => x.IsReadyForPvP) + (LocalPlayerState.IsReadyForPvP ? 1 : 0);
            OnReadinessChange?.Invoke(player.NickName, isReady, playersReady);
        }

        public void StartClient()
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            PhotonPeer.RegisterType(typeof(UnitSpawnData), 255, UnitSpawnData.Serialize, UnitSpawnData.Deserialize);
            PhotonPeer.RegisterType(typeof(FVector), 254, SerializationHelpers.SerializeFVector, SerializationHelpers.DeserializeFVector);
            PhotonPeer.RegisterType(typeof(FRotator), 253, SerializationHelpers.SerializeFRotator, SerializationHelpers.DeserializeFRotator);
            PhotonPeer.RegisterType(typeof(EMoveSpeedLevel), 252, (stream, obj) =>
            {
                stream.WriteByte((byte)obj);
                return 1;
            }, (stream, length) => (EMoveSpeedLevel)stream.ReadByte());

            PhotonPeer.RegisterType(typeof(MontageCallbackData), 251, MontageCallbackData.Serialize, MontageCallbackData.Deserialize);
            PhotonPeer.RegisterType(typeof(MonsterMontageCallbackData), 250, MonsterMontageCallbackData.Serialize, MonsterMontageCallbackData.Deserialize);
            PhotonPeer.RegisterType(typeof(EquipmentState), 249, EquipmentState.Serialize, EquipmentState.Deserialize);
            PhotonPeer.RegisterType(typeof(DamageNumParam), 248, SerializationHelpers.SerializeDamageNumParam, SerializationHelpers.DeserializeDamageNumParam);
            PhotonPeer.RegisterType(typeof(PlayerTransformData), 247, PlayerTransformData.Serialize, PlayerTransformData.Deserialize);
            PhotonPeer.RegisterType(typeof(ImmobilizeData), 246, ImmobilizeData.Serialize, ImmobilizeData.Deserialize);

            PhotonClient.AddCallbackTarget(this);
            PhotonClient.StateChanged += OnStateChange;

            OnBeforeJoinRoom?.Invoke();

            PhotonClient.AuthValues = _authValues;
            PhotonClient.ConnectUsingSettings(new AppSettings
            {
                // DEVELOPMENT (Jakub's machine)
                // AppIdRealtime = "4fefdae2-db02-446c-bd5b-382a8ff41c08",
                // PRODUCTION
                AppIdRealtime = "882e235d-5fa3-457a-babb-a08c96da7238",
                AuthMode = AuthModeOption.AuthOnce,
                Protocol = ConnectionProtocol.Udp,
                EnableProtocolFallback = false,
                UseNameServer = true,
                FixedRegion = "usw",
            });

            new Thread(LoopGame).Start();

            Logging.LogDebug("Running forever.");
        }

        public void StopClient()
        {
            Logging.LogDebug("Stopping client...");

            _isExit = true;

            WukongChat.Disconnect();
            WukongChat = null;

            PhotonClient.Disconnect();

            Logging.LogDebug("Stopped client.");

            PhotonClient.RemoveCallbackTarget(this);

            // destroy all connected players
            foreach (var player in ConnectedPlayers.Values)
            {
                BGU_UnrealWorldUtil.DestroyActor(player.Pawn);
            }
        }

        // ReSharper disable once FunctionNeverReturns
        private void LoopGame()
        {
            while (!_isExit)
            {
                PhotonClient.Service();
                Thread.Sleep(33);
            }
        }

        public void SpawnClone()
        {
            var clone = new WukongClientClone();
            _photonClones.Add(clone);

            clone.StartClient();
        }

        public IEnumerable<Player> GetOtherPlayersInRoom()
        {
            if (PhotonClient.CurrentRoom is null)
            {
                Logging.LogError("No room joined.");
                yield break;
            }

            foreach (var player in PhotonClient.CurrentRoom.Players)
            {
                Logging.LogDebug($"Other player: {player.Value.ActorNumber} {player.Value.UserId} local: {player.Value.IsLocal}");
                if (!player.Value.IsLocal)
                    yield return player.Value;
            }
        }

        private async Task JoinRandomOrCreateRoom()
        {
            await PhotonClient.JoinLobbyAsync(_lobby);

            switch (_gameMode)
            {
                case GameMode.Private:
                {
                    var propertiesForRoomCreation = new RoomOptions
                    {
                        CustomRoomProperties = new PhotonHashtable
                        {
                            [nameof(RoomState.RoundsTotal)] = 3,
                            [nameof(RoomState.RoundWinners)] = "",
                            [nameof(RoomState.GameMode)] = _gameMode
                        },
                        MaxPlayers = 10,
                        IsOpen = true,
                        IsVisible = false,
                        PublishUserId = true,
                    };

                    var createArgs = new EnterRoomArgs
                    {
                        RoomOptions = propertiesForRoomCreation,
                        RoomName = _roomName,
                    };

                    Logging.LogDebug($"Joining or creating private room {_roomName}");
                    await PhotonClient.JoinOrCreateRoomAsync(createArgs);
                    break;
                }
                case GameMode.XvX:
                {
                    var propertiesForRoomCreation = new RoomOptions
                    {
                        CustomRoomProperties = new PhotonHashtable
                        {
                            [nameof(RoomState.RoundsTotal)] = 3,
                            [nameof(RoomState.RoundWinners)] = "",
                            [nameof(RoomState.GameMode)] = _gameMode
                        },
                        MaxPlayers = 2 * _playersPerTeam,
                        IsOpen = true,
                        IsVisible = true,
                        PublishUserId = false,
                        CustomRoomPropertiesForLobby = [nameof(RoomState.GameMode)]
                    };

                    var createArgs = new EnterRoomArgs
                    {
                        RoomOptions = propertiesForRoomCreation,
                    };

                    var joinArgs = new JoinRandomRoomArgs
                    {
                        ExpectedMaxPlayers = _gameMode == GameMode.XvX ? 2 * _playersPerTeam : 10,
                        MatchingType = MatchmakingMode.FillRoom,
                        ExpectedCustomRoomProperties = new PhotonHashtable
                        {
                            [nameof(RoomState.GameMode)] = _gameMode
                        },
                    };

                    Logging.LogDebug($"Joining or creating {_playersPerTeam}v{_playersPerTeam} room");
                    await PhotonClient.JoinRandomOrCreateRoomAsync(joinArgs, createArgs);
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static void OnStateChange(ClientState arg1, ClientState arg2)
        {
            Logging.LogDebug($"{arg1} -> {arg2}");
        }

        public void SpawnUnit(string id, string unitName, int teamId, float x, float y, float z)
        {
            const byte eventCode = 1;
            var evData = new UnitSpawnData(id, unitName, teamId, x, y, z);
            PhotonClient.OpRaiseEvent(eventCode, evData, RaiseEventArgs.Default, SendOptions.SendReliable);
        }

        public void SendMontageCallback(EMontageBindReason reason, string montagePath, EMontageCallbackState state)
        {
            const byte eventCode = 2;
            var evData = new MontageCallbackData(reason, montagePath, state);
            PhotonClient.OpRaiseEvent(eventCode, evData, RaiseEventArgs.Default, SendOptions.SendReliable);

            foreach (var clone in _photonClones)
            {
                clone.SendMontageCallback(reason, montagePath, state);
            }
        }

        public void SendMonsterMontageCallback(string monsterId, EMontageBindReason reason, string montagePath, EMontageCallbackState state)
        {
            const byte eventCode = 4;
            var evData = new MonsterMontageCallbackData(monsterId, reason, montagePath, state);
            PhotonClient.OpRaiseEvent(eventCode, evData, RaiseEventArgs.Default, SendOptions.SendReliable);
        }

        public void SendMonsterWakeUp(string guid)
        {
            const byte eventCode = 5;
            PhotonClient.OpRaiseEvent(eventCode, guid, RaiseEventArgs.Default, SendOptions.SendReliable);
        }

        public void SendDamageNum(DamageNumParam damageNumParam)
        {
            const byte eventCode = 6;
            PhotonClient.OpRaiseEvent(eventCode, damageNumParam, RaiseEventArgs.Default, SendOptions.SendUnreliable);
        }

        public void BroadcastPlayerRebirth(int playerId)
        {
            const byte eventCode = 7;
            PhotonClient.OpRaiseEvent(eventCode, playerId, new RaiseEventArgs
            {
                Receivers = ReceiverGroup.All
            }, SendOptions.SendReliable);
        }

        public void SendPvPEvent(PvPEvent ev, int data = 0)
        {
            if (!IsMasterClient)
            {
                Logging.LogError("Only room owner can send start countdown.");
                return;
            }

            Logging.LogDebug($"Sending PvP event: {ev}");

            const byte eventCode = 8;
            var evData = new[] { (int)ev, data };
            PhotonClient.OpRaiseEvent(eventCode, evData, new RaiseEventArgs
            {
                Receivers = ReceiverGroup.All
            }, SendOptions.SendReliable);
        }

        public void KillCurrentPlayer()
        {
            const byte eventCode = 9;
            PhotonClient.OpRaiseEvent(eventCode, PhotonId, new RaiseEventArgs
            {
                Receivers = ReceiverGroup.MasterClient,
            }, SendOptions.SendReliable);
        }

        public void BroadcastPlayerTransform(int playerId, FVector location, FRotator rotation)
        {
            const byte eventCode = 10;
            var evData = new PlayerTransformData(playerId, location, rotation);
            PhotonClient.OpRaiseEvent(eventCode, evData, new RaiseEventArgs
            {
                Receivers = ReceiverGroup.All
            }, SendOptions.SendReliable);
        }

        public void SendPhantomRush(ESkillDirection phantomRushDir)
        {
            const byte eventCode = 11;
            PhotonClient.OpRaiseEvent(eventCode, phantomRushDir, RaiseEventArgs.Default, SendOptions.SendReliable);
        }

        public void BroadcastImmobilize(int playerId, int otherPlayerId, ImmobilizeActionType immobilizeActionType, bool hasBuff)
        {
            const byte eventCode = 12;
            var evData = new ImmobilizeData(playerId, otherPlayerId, immobilizeActionType, hasBuff);
            PhotonClient.OpRaiseEvent(eventCode, evData, RaiseEventArgs.Default, SendOptions.SendReliable);
        }

        public void SendTarget(int playerId)
        {
            const byte eventCode = 13;
            PhotonClient.OpRaiseEvent(eventCode, playerId, RaiseEventArgs.Default, SendOptions.SendReliable);
        }

        public void ExitPhantomRush(int playerId)
        {
            const byte eventCode = 14;
            PhotonClient.OpRaiseEvent(eventCode, playerId, RaiseEventArgs.Default, SendOptions.SendReliable);
        }

        public void CacheEquipmentChange(EquipPosition position, int newEq)
        {
            LocalPlayerState.Equipment.SetEquipment(position, newEq);
            CachePlayerProperty(nameof(PlayerState.Equipment), LocalPlayerState.Equipment);
        }

        public void RequestStartPvP()
        {
            if (!IsMasterClient)
            {
                GameUtils.ShowTip("Only room owner can start PvP.");
                return;
            }

            StartPvP();
        }

        public void StartPvP()
        {
            if (!IsMasterClient)
            {
                return;
            }

            // clear previous round winners
            CurrentRoomState.RoundWinners = Enumerable.Empty<int>();

            Task.Run(LobbyManager.StartRoundAsync);
        }

        protected virtual void ApplyMonsterMove(PhotonHashtable props)
        {
            foreach (var (key, value) in props)
            {
                var compositeKey = (string)key;
                var parts = compositeKey.Split(MonsterHashtableKeySeparator);
                if (parts.Length != 2)
                {
                    Logging.LogDebug($"Invalid key: {compositeKey}");
                    continue;
                }

                var guid = parts[0];
                var propName = parts[1];

                if (!SyncedMonsters.TryGetValue(guid, out var monsterState))
                {
                    Logging.LogDebug($"Monster {guid} not found.");
                    continue;
                }

                if (!MonsterSetters.TryGetValue(propName, out var setter))
                {
                    setter = CreateSetter<MonsterState>(propName);
                    MonsterSetters[propName] = setter;
                }

                setter(monsterState, value);
            }
        }

        private ConcurrentDictionary<string, object> _playerProperties = new ConcurrentDictionary<string, object>();

        private ConcurrentDictionary<string, object> _playerPropertiesRo = new ConcurrentDictionary<string, object>();

        private readonly object _playerPropertiesLock = new object();

        public void SetCachedPlayerProperties()
        {
            lock (_playerPropertiesLock)
            {
                (_playerProperties, _playerPropertiesRo) = (_playerPropertiesRo, _playerProperties);

                if (_playerPropertiesRo.Count == 0)
                    return;

                var hashtable = new PhotonHashtable();
                foreach (var (key, value) in _playerPropertiesRo)
                {
                    hashtable[key] = value;
                }

                _playerPropertiesRo.Clear();
                PhotonClient.LocalPlayer.SetCustomProperties(hashtable);
            }

            foreach (var clone in _photonClones)
            {
                clone.SetCachedPlayerProperties();
            }
        }

        public virtual void CachePlayerProperty(string key, object value)
        {
            _playerProperties[key] = value;
            if (!(value is FVector || value is FRotator || key == nameof(PlayerState.TurnInplaceRemainAngle)))
            {
                Logging.LogDebug($"Set player property: {key} = {value}");
            }

            foreach (var clone in _photonClones)
            {
                clone.CachePlayerProperty(key, value);
            }
        }

        public void CachePlayerAttribute(EBGUAttrFloat attr, float value)
        {
            CachePlayerProperty($"{Constants.AttributePrefix}{attr}", value);
        }

        public void SetRemotePlayerProperty(int playerId, string key, object value)
        {
            if (!IsMasterClient)
            {
                Logging.LogDebug("Only room owner can send remote player properties.");
                return;
            }

            var hashtable = new PhotonHashtable
            {
                [key] = value
            };

            Logging.LogDebug($"Sending remote player property: {key} = {value}");

            PhotonClient.OpSetCustomPropertiesOfActor(playerId, hashtable);
        }

        private ConcurrentDictionary<string, object> _monsterProperties = new ConcurrentDictionary<string, object>();

        private ConcurrentDictionary<string, object> _monsterPropertiesRo = new ConcurrentDictionary<string, object>();

        private readonly object _monsterPropertiesLock = new object();

        public void SendUpdatedMonsterProperties()
        {
            lock (_monsterPropertiesLock)
            {
                (_monsterProperties, _monsterPropertiesRo) = (_monsterPropertiesRo, _monsterProperties);

                if (_monsterPropertiesRo.Count == 0)
                    return;

                var hashtable = new PhotonHashtable();
                foreach (var (key, value) in _monsterPropertiesRo)
                {
                    hashtable[key] = value;
                }

                _monsterPropertiesRo.Clear();

                const byte eventCode = 3;
                PhotonClient.OpRaiseEvent(eventCode, hashtable, RaiseEventArgs.Default, SendOptions.SendUnreliable);
            }
        }

        public void CacheMonsterProperty(string guid, string prop, object value)
        {
            _monsterProperties[$"{guid}{MonsterHashtableKeySeparator}{prop}"] = value;

            if (!(value is FVector || value is FRotator))
            {
                Logging.LogDebug($"Set monster property [{guid}]: {prop} = {value}");
            }
        }

        #region IConnectionCallbacks

        public void OnConnected()
        {
            Logging.LogDebug("Connected");
        }

        public async void OnConnectedToMaster()
        {
            try
            {
                Logging.LogDebug("Connected to master server: " + PhotonClient.RealtimePeer.ServerIpAddress);
                await JoinRandomOrCreateRoom();
            }
            catch (Exception e)
            {
                Logging.LogException(e);
            }
        }

        public void OnDisconnected(DisconnectCause cause)
        {
            Logging.LogDebug($"Disconnected: {cause}");
        }

        public void OnRegionListReceived(RegionHandler regionHandler)
        {
            Logging.LogDebug("Region list received");
        }

        public void OnCustomAuthenticationResponse(Dictionary<string, object> data)
        {
            Logging.LogDebug("Custom authentication response");

            foreach (var kvp in data)
            {
                Logging.LogDebug($"{kvp.Key}: {kvp.Value}");
            }
        }

        public void OnCustomAuthenticationFailed(string debugMessage)
        {
            Logging.LogDebug("Custom authentication failed: " + debugMessage);
        }

        #endregion

        #region IMatchmakingCallbacks

        public void OnFriendListUpdate(List<FriendInfo> friendList)
        {
            Logging.LogDebug("Friend list update");
        }

        public void OnCreatedRoom()
        {
            Logging.LogDebug("Created room");
        }

        public void OnCreateRoomFailed(short returnCode, string message)
        {
            Logging.LogDebug("Create room failed: " + message);
        }

        public int GetTeamIdForPlayer()
        {
            var r = new Random();
            var index = r.Next(0, Constants.AvailableTeamIds.Count);
            return Constants.AvailableTeamIds[index];
        }

        public virtual void OnJoinedRoom()
        {
            Logging.LogDebug($"Joined room {PhotonClient.CurrentRoom.Name}");

            var teamId = GetTeamIdForPlayer();
            LocalPlayerState = new PlayerState(PhotonId, GameUtils.GetControlledPawn(), teamId);
            CachePlayerProperty(nameof(PlayerState.TeamId), teamId);

            if (IsMasterClient)
            {
                LobbyManager = new LobbyManager(this);
            }

            Utils.TryRunOnGameThread(PhotonUtils.DiscoverMonsters);

            _joinedRoomCallback?.Invoke();
            WukongChat.InitializeChat(PhotonClient.NickName);
        }

        public void OnJoinRoomFailed(short returnCode, string message)
        {
            Logging.LogDebug("Join room failed: " + message);
        }

        public void OnJoinRandomFailed(short returnCode, string message)
        {
            Logging.LogDebug("Join random failed: " + message);
        }

        public void OnLeftRoom()
        {
            Logging.LogDebug("Left room");
        }

        #endregion

        public void OnPlayerEnteredRoom(Player newPlayer)
        {
            Logging.LogDebug($"Player {newPlayer.ActorNumber} entered the room");
            _playerJoinedCallback?.Invoke(newPlayer);
        }

        public void OnPlayerLeftRoom(Player otherPlayer)
        {
            Logging.LogDebug($"Player {otherPlayer.ActorNumber} left the room");

            var playerState = ConnectedPlayers[otherPlayer.ActorNumber];
            ConnectedPlayers.Remove(otherPlayer.ActorNumber);
            OnPlayerLeft?.Invoke(playerState);

            if (IsMasterClient)
            {
                WukongChat.SendChatMessage(WukongChatter.ServerChannelName, $"{playerState.NickName} has left!");
            }
        }

        public void OnRoomPropertiesUpdate(PhotonHashtable changedProps)
        {
            // empty, RoomState is a proxy to this hashtable
        }

        public virtual void OnPlayerPropertiesUpdate(Player targetPlayer, PhotonHashtable changedProps)
        {
            var id = targetPlayer.ActorNumber;

            PlayerState playerState;

            if (targetPlayer.IsLocal)
            {
                playerState = LocalPlayerState;
            }
            else if (!ConnectedPlayers.TryGetValue(id, out playerState))
            {
                Logging.LogDebug($"Player {id} not found.");
                return;
            }

            foreach (var kvp in changedProps)
            {
                if (!(kvp.Key is string propertyName))
                {
                    if (kvp.Key is byte numId && numId == ActorProperties.NickName)
                    {
                        playerState.NickName = (string)kvp.Value;
                        Logging.LogDebug($"Assigning NickName = {playerState.NickName} for player {id}");
                    }
                    else
                    {
                        Logging.LogWarning($"Unhandled player state key: {kvp.Key}");
                    }

                    continue;
                }

                // attributes have special treatment
                if (propertyName.StartsWith(Constants.AttributePrefix))
                {
                    Logging.LogDebug($"Assigning {propertyName} = {kvp.Value} for player {id}");

                    var key = propertyName.Substring(Constants.AttributePrefix.Length);

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

                if (!(kvp.Value is FVector || kvp.Value is FRotator || kvp.Value is float))
                {
                    Logging.LogDebug($"Assigning {propertyName} = {kvp.Value} to player {id}");
                }

                setter(playerState, kvp.Value);

                // special handlers for some properties
                switch (propertyName)
                {
                    case nameof(PlayerState.Equipment):
                        OnEquipmentChange?.Invoke(id, (EquipmentState)kvp.Value);
                        break;
                    case nameof(PlayerState.IsReadyForPvP):
                        OnPlayerReadinessChanged(targetPlayer, (bool)kvp.Value);
                        continue;
                    case nameof(PlayerState.TeamId):
                        OnTeamChange?.Invoke(playerState, (int)kvp.Value);
                        continue;
                }
            }
        }

        public void OnMasterClientSwitched(Player newMasterClient)
        {
            if (newMasterClient.ActorNumber == PhotonId)
            {
                // assume control of lobby manager
                LobbyManager = new LobbyManager(this);
            }
        }

        private static readonly Dictionary<string, Action<PlayerState, object>> PlayerSetters = new Dictionary<string, Action<PlayerState, object>>();
        private static readonly Dictionary<string, Action<MonsterState, object>> MonsterSetters = new Dictionary<string, Action<MonsterState, object>>();

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
}