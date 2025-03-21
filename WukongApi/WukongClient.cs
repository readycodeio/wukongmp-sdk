using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Reflection;
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
using WukongApi.Patches;
using WukongApi.State;
using WukongApi.UI;
using PlayerState = WukongApi.State.PlayerState;

namespace WukongApi
{
    public sealed class WukongClient : IConnectionCallbacks, IOnEventCallback, IMatchmakingCallbacks, IInRoomCallbacks
    {
        internal readonly RealtimeClient PhotonClient = new();
        private readonly TypedLobby _lobby = new("pvpLobby", LobbyType.Default);

        private const char MonsterHashtableKeySeparator = ';';

        private bool _isStopped = true;
        public bool JoinedRoomCallbacksDone { get; private set; } // prevent race condition where Photon sets InRoom = true before calling OnJoinedRoom

        private int PhotonId => PhotonClient.LocalPlayer!.ActorNumber; // LocalPlayer is never null, but can be invalid
        public bool IsMasterClient => PhotonClient.CurrentRoom?.MasterClientId == PhotonId;
        public bool ConnectedAndReady => PhotonClient.IsConnectedAndReady;

        private readonly Action _joinedRoomCallback;
        private readonly Action<Player> _playerJoinedCallback;
        public event Action<int, MontageCallbackData>? OnMontageCallback;
        public event Action<int, MonsterMontageCallbackData>? OnMonsterMontageCallback;
        public event Action<int, string, string, int, float, float, float>? OnUnitSpawn;
        public event Action<string>? OnMonsterWakeUp;
        public event Action<int, EquipmentState>? OnEquipmentChange;
        public event Action<string, bool, int>? OnReadinessChange;
        public event Action<PlayerState, int>? OnTeamChange;
        public event Action<PlayerState>? OnPlayerLeft;
        public event Action<int>? OnPlayerRebirth;
        public event Action<int>? OnKillPlayer;
        public event Action<FVector, FRotator>? OnSetPlayerTransform;
        public event Action? OnBeforeJoinRoom;
        public event Action<DamageNumParam>? OnDamageNum;
        public event Action<int, ESkillDirection>? OnPhantomRush;
        public event Action<int>? OnExitPhantomRush;
        public event Action<int, int, ImmobilizeActionType, bool>? OnHandleImmobilize;
        public event Action<int, int>? OnTargetSet;
        public event Action? OnMatchmakingEnded;

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

        public RoomState CurrentRoomState { get; }

        public readonly Dictionary<int, PlayerState> ConnectedPlayers = new();
        public readonly Dictionary<string, MonsterState> SyncedMonsters = new();

        public IEnumerable<PlayerState> AllConnectedPlayers
            => ConnectedPlayers.Values.Append(LocalPlayerState);

        public IEnumerable<PlayerState> AllPvPPlayers
            => ConnectedPlayers.Values.Where(p => !p.IsSpectator).Concat(LocalPlayerState.IsSpectator ? [] : [LocalPlayerState]);

        public WukongClient(Action onJoinedRoom, Action<Player> playerJoinedCallback)
        {
            WukongChat = new WukongChatter(this, ChatWidget.Instance.GetMessage);
            CurrentRoomState = new RoomState(this);
            LobbyManager = new LobbyManager(this);

            _joinedRoomCallback = onJoinedRoom;
            _playerJoinedCallback = playerJoinedCallback;

            ConfigurePhoton();
        }

        ~WukongClient()
        {
            Logging.LogInformation("WukongClient finalizer called");
            StopClient();
            PhotonClient.RemoveCallbackTarget(this);
        }

        public void RegisterPlayer(PlayerState state)
        {
            Logging.LogDebug("Registering player {PlayerId}", state.PhotonId);
            ConnectedPlayers.Add(state.PhotonId, state);
        }

        public PlayerState? GetByActor(AActor? actor)
        {
            if (actor == null)
                return null;

            return actor == LocalPlayerState.Pawn
                ? LocalPlayerState
                : ConnectedPlayers.FirstOrDefault(x => x.Value!.Pawn == actor).Value;
        }

        public PlayerState? GetById(int playerId)
        {
            return playerId == LocalPlayerState.PhotonId
                ? LocalPlayerState
                : ConnectedPlayers.GetValueOrDefault(playerId);
        }

        public MonsterState? GetByTamerActor(BUTamerActor owner)
        {
            return SyncedMonsters.FirstOrDefault(x => x.Value!.Pawn == owner).Value;
        }

        private void SetReadyState(bool isReady)
        {
            CachePlayerProperty(nameof(PlayerState.IsReadyForPvP), isReady);
        }

        private void SetIsSpectatorState(bool isSpectator)
        {
            CachePlayerProperty(nameof(PlayerState.IsSpectator), isSpectator);
        }

        public void SwitchReadyState()
        {
            if (PhotonClient.InRoom && CurrentRoomState is { InPvP: false, InMatchmaking: false })
            {
                var isReady = LocalPlayerState.IsReadyForPvP;
                SetReadyState(!isReady);
                WukongMP.Instance.SwitchReadyState(!isReady);
            }
        }

        public void SwitchTeam(bool force = false)
        {
            if (force || (PhotonClient.InRoom && !LocalPlayerState.IsReadyForPvP && CurrentRoomState is { InPvP: false, InMatchmaking: false }))
            {
                var teamId = LocalPlayerState.TeamId == Constants.AvailableTeamIds[0] ? Constants.AvailableTeamIds[1] : Constants.AvailableTeamIds[0];
                CachePlayerProperty(nameof(PlayerState.TeamId), teamId);
            }
        }

        public MonsterState? GetMonsterByCharacter(BGUCharacterCS? owner)
        {
            if (owner == null)
                return null;

            var kvp = SyncedMonsters.FirstOrDefault(x => x.Value!.Pawn?.GetMonster() == owner);
            return kvp.Value;
        }

        public void RemoveMonster(string monsterGuid)
        {
            SyncedMonsters.Remove(monsterGuid);
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
                    ApplyMonsterMove((PhotonHashtable)photonEvent.CustomData);
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
                case 15:
                    // end matchmaking phase
                    OnMatchmakingEnded?.Invoke();
                    return;
            }
        }

        private void HandlePvPEvent(PvPEvent ev, int winnerTeamId)
        {
            Logging.LogDebug("Received PvP event: {Event}", ev);

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
                        GameUtils.ShowTip("Round ended: Draw");
                    }
                    else
                    {
                        GameUtils.ShowTip($"Round ended. Team {GameUtils.GetTeamName(winnerTeamId)} won");
                    }

                    if (winnerTeamId == Constants.DrawTeamId)
                        return;

                    if (winnerTeamId == LocalPlayerState.TeamId)
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
                        WukongMP.Instance.EndTournament(winnerTeamId);
                        ExitPvP();
                        SetReadyState(false);
                        SetIsSpectatorState(false);
                    });

                    break;
                }
                case PvPEvent.ResetStats:
                    if (!LocalPlayerState.IsDead)
                    {
                        Utils.TryRunOnGameThread(() =>
                        {
                            var events = BUS_EventCollectionCS.Get(LocalPlayerState.Pawn!);

                            if (events == null)
                            {
                                Logging.LogError("events is null in {Patch}", nameof(HandlePvPEvent));
                                return;
                            }

                            events.Evt_TriggerTeleportResetPlayer!.Invoke();
                        });
                    }

                    if (IsMasterClient)
                    {
                        // reset other players' Hp to HpMax if they are not dead
                        foreach (var (key, state) in ConnectedPlayers)
                        {
                            if (!state.IsDead)
                            {
                                if (state.Pawn == null)
                                {
                                    Logging.LogError("Pawn is null in {Patch}", nameof(HandlePvPEvent));
                                    return;
                                }

                                var attrContainer = (BUC_AttrContainer?)BGU_DataUtil.GetReadOnlyData<IBUC_AttrContainer, BUC_AttrContainer>(state.Pawn);
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
                    throw new ArgumentOutOfRangeException(nameof(ev));
            }
        }

        public void CheckRoundEndCondition()
        {
            if (!IsMasterClient || !CurrentRoomState.InPvP)
            {
                return;
            }

            // check if all players but one are dead
            var players = AllPvPPlayers.ToList();
            var alivePlayers = players.Where(p => !p.IsDead).ToList();
            if (alivePlayers.Count == 0)
            {
                Logging.LogInformation("All players are dead, ending round");
                Task.Run(async () => await LobbyManager.EndRoundAsync(Constants.DrawTeamId));
                return;
            }

            var alivePlayersTeams = alivePlayers.Select(p => p.TeamId).Distinct().Count();
            if (alivePlayersTeams == 1)
            {
                Logging.LogInformation("One team with alive players, ending round");
                var winner = players.First(p => !p.IsDead);
                Task.Run(async () => await LobbyManager.EndRoundAsync(winner.TeamId));
            }
        }

        private void EnterPvP()
        {
            if (!IsMasterClient) return;

            if (PhotonClient.CurrentRoom == null)
            {
                Logging.LogError("No room joined.");
                return;
            }

            CurrentRoomState.InPvP = true;
            if (CurrentRoomState.GameMode == GameMode.XvX)
            {
                PhotonClient.CurrentRoom.IsOpen = false;
            }
        }

        private void ExitPvP()
        {
            if (!IsMasterClient) return;

            if (PhotonClient.CurrentRoom == null)
            {
                Logging.LogError("No room joined.");
                return;
            }

            CurrentRoomState.InPvP = false;
            if (CurrentRoomState.GameMode == GameMode.XvX)
            {
                PhotonClient.CurrentRoom.IsOpen = true;
            }
        }

        private void OnPlayerReadinessChanged(Player player, bool isReady)
        {
            var playersReady = ConnectedPlayers.Values.Count(x => x.IsReadyForPvP) + (LocalPlayerState.IsReadyForPvP ? 1 : 0);
            OnReadinessChange?.Invoke(player.NickName, isReady, playersReady);
        }

        public void Reconnect()
        {
            StopClient();
            StartClient();
        }

        private void SubscribeToPlayerMontageCallbacks()
        {
            var myPawn = GameUtils.GetControlledPawn();
            LocalPlayerState.Pawn = myPawn;

            var events = BUS_EventCollectionCS.Get(myPawn);
            events.Evt_PlayMontageCallback += OnPlayMontageCallback;
        }

        private void UnsubscribeFromPlayerMontageCallbacks()
        {
            var myPawn = GameUtils.GetControlledPawn();
            var events = BUS_EventCollectionCS.Get(myPawn);

            if (events != null)
            {
                events.Evt_PlayMontageCallback -= OnPlayMontageCallback;
            }
        }

        private void OnPlayMontageCallback(EMontageBindReason reason, UAnimMontage montage, EMontageCallbackState state)
        {
            var montagePath = montage.GetPathName();
            Logging.LogDebug("Montage callback: {Reason} {Montage} {State}", reason, montagePath, state);
            SendMontageCallback(reason, montagePath, state);
        }

        private void ConfigurePhoton()
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            PhotonPeer.RegisterType(typeof(UnitSpawnData), 255, UnitSpawnData.Serialize, UnitSpawnData.Deserialize);
            PhotonPeer.RegisterType(typeof(FVector), 254, SerializationHelpers.SerializeFVector, SerializationHelpers.DeserializeFVector);
            PhotonPeer.RegisterType(typeof(FRotator), 253, SerializationHelpers.SerializeFRotator, SerializationHelpers.DeserializeFRotator);
            PhotonPeer.RegisterType(typeof(EMoveSpeedLevel), 252, (stream, obj) =>
            {
                stream.WriteByte((byte)obj);
                return 1;
            }, (stream, _) => (EMoveSpeedLevel)stream.ReadByte());

            PhotonPeer.RegisterType(typeof(MontageCallbackData), 251, MontageCallbackData.Serialize, MontageCallbackData.Deserialize);
            PhotonPeer.RegisterType(typeof(MonsterMontageCallbackData), 250, MonsterMontageCallbackData.Serialize, MonsterMontageCallbackData.Deserialize);
            PhotonPeer.RegisterType(typeof(EquipmentState), 249, EquipmentState.Serialize, EquipmentState.Deserialize);
            PhotonPeer.RegisterType(typeof(DamageNumParam), 248, SerializationHelpers.SerializeDamageNumParam, SerializationHelpers.DeserializeDamageNumParam);
            PhotonPeer.RegisterType(typeof(PlayerTransformData), 247, PlayerTransformData.Serialize, PlayerTransformData.Deserialize);
            PhotonPeer.RegisterType(typeof(ImmobilizeData), 246, ImmobilizeData.Serialize, ImmobilizeData.Deserialize);

            PhotonClient.AddCallbackTarget(this);
            PhotonClient.StateChanged += OnStateChange;
            PhotonClient.AuthValues = CmdLineParams.Instance.RealtimeAuthentication!;
        }

        public void StartClient()
        {
            if (!_isStopped)
            {
                Logging.LogError("Client is already running.");
                return;
            }

            OnBeforeJoinRoom?.Invoke();

            PhotonClient.ConnectUsingSettings(new AppSettings
            {
                AppIdRealtime = Constants.RealtimeAppId,
                AuthMode = AuthModeOption.AuthOnce,
                Protocol = ConnectionProtocol.Udp,
                EnableProtocolFallback = false,
                UseNameServer = true,
                FixedRegion = "usw",
            });

            _isStopped = false;
            new Thread(LoopGame).Start();

            Logging.LogInformation("Client started");
        }

        public void StopClient()
        {
            if (_isStopped)
            {
                Logging.LogDebug("Client is already stopped.");
                return;
            }

            Logging.LogInformation("Stopping client...");

            if (GameUtils.IsWorldValid())
            {
                UnsubscribeFromPlayerMontageCallbacks();
            }

            _isStopped = true;

            WukongChat.StopClient();
            PhotonClient.Disconnect();

            // destroy all connected players
            foreach (var player in ConnectedPlayers.Values)
            {
                if (player.Pawn != null)
                {
                    BGU_UnrealWorldUtil.DestroyActor(player.Pawn);
                }
            }

            // clear state
            ConnectedPlayers.Clear();
            SyncedMonsters.Clear();

            _localPlayerState = null;

            Logging.LogInformation("Stopped client.");
        }

        private void LoopGame()
        {
            Logging.LogInformation("Photon Realtime service loop started");
            while (!_isStopped)
            {
                PhotonClient.Service();
                Thread.Sleep(33);
            }

            Logging.LogInformation("Photon Realtime service loop finished");
        }

        public IEnumerable<Player> GetOtherPlayersInRoom()
        {
            if (PhotonClient.CurrentRoom == null)
            {
                Logging.LogError("No room joined.");
                yield break;
            }

            foreach (var player in PhotonClient.CurrentRoom.Players!.Values)
            {
                Logging.LogDebug("Other player: {ActorNumber} {Nickname} local: {IsLocal}", player.ActorNumber, player.NickName!, player.IsLocal);
                if (!player.IsLocal)
                    yield return player;
            }
        }

        private async Task JoinRandomOrCreateRoom()
        {
            await PhotonClient.JoinLobbyAsync(_lobby);

            var gameMode = CmdLineParams.Instance.MatchmakingMode;
            switch (gameMode)
            {
                case GameMode.Private:
                {
                    var roomName = CmdLineParams.Instance.RoomName!; // not null if game mode is private
                    var propertiesForRoomCreation = new RoomOptions
                    {
                        CustomRoomProperties = new PhotonHashtable
                        {
                            [nameof(RoomState.RoundsTotal)] = 3,
                            [nameof(RoomState.RoundWinners)] = "",
                            [nameof(RoomState.GameMode)] = gameMode
                        },
                        MaxPlayers = 10,
                        IsOpen = true,
                        IsVisible = false,
                        PublishUserId = true,
                        EmptyRoomTtl = Constants.PhotonTtlMs
                    };

                    var createArgs = new EnterRoomArgs
                    {
                        RoomOptions = propertiesForRoomCreation,
                        RoomName = roomName,
                    };

                    Logging.LogInformation("Joining or creating private room {RoomName}", roomName);
                    await PhotonClient.JoinOrCreateRoomAsync(createArgs);
                    break;
                }
                case GameMode.XvX:
                {
                    var playersPerTeam = CmdLineParams.Instance.PlayersPerTeam!.Value; // not null when game mode is XvX
                    var propertiesForRoomCreation = new RoomOptions
                    {
                        CustomRoomProperties = new PhotonHashtable
                        {
                            [nameof(RoomState.RoundsTotal)] = 3,
                            [nameof(RoomState.RoundWinners)] = "",
                            [nameof(RoomState.GameMode)] = gameMode
                        },
                        MaxPlayers = 2 * playersPerTeam,
                        IsOpen = true,
                        IsVisible = true,
                        PublishUserId = false,
                        CustomRoomPropertiesForLobby = [nameof(RoomState.GameMode)],
                        EmptyRoomTtl = Constants.PhotonTtlMs
                    };

                    var createArgs = new EnterRoomArgs
                    {
                        RoomOptions = propertiesForRoomCreation,
                    };

                    var joinArgs = new JoinRandomRoomArgs
                    {
                        ExpectedMaxPlayers = 2 * playersPerTeam,
                        MatchingType = MatchmakingMode.FillRoom,
                        ExpectedCustomRoomProperties = new PhotonHashtable
                        {
                            [nameof(RoomState.GameMode)] = gameMode
                        },
                    };

                    Logging.LogInformation("Joining or creating {Players}v{Players} room", playersPerTeam, playersPerTeam);
                    await PhotonClient.JoinRandomOrCreateRoomAsync(joinArgs, createArgs);
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException(nameof(gameMode));
            }
        }

        private static void OnStateChange(ClientState arg1, ClientState arg2)
        {
            Logging.LogDebug("Photon state change: {From} -> {To}", arg1, arg2);
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

            Logging.LogInformation("Sending PvP event: {Event}", ev);

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

        public void SendEndMatchmaking()
        {
            const byte eventCode = 15;
            PhotonClient.OpRaiseEvent(eventCode, null, new RaiseEventArgs
            {
                Receivers = ReceiverGroup.All
            }, SendOptions.SendReliable);
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
            CurrentRoomState.RoundWinners = [];

            Task.Run(LobbyManager.StartRoundAsync);
        }

        private void ApplyMonsterMove(PhotonHashtable props)
        {
            foreach (var (key, value) in props)
            {
                var compositeKey = (string)key;
                var parts = compositeKey.Split(MonsterHashtableKeySeparator);
                if (parts.Length != 2)
                {
                    Logging.LogDebug("Invalid key: {Key}", compositeKey);
                    continue;
                }

                var guid = parts[0];
                var propName = parts[1];

                if (!SyncedMonsters.TryGetValue(guid, out var monsterState))
                {
                    Logging.LogDebug("Monster {Guid} not found.", guid);
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

        private ConcurrentDictionary<string, object> _playerProperties = new();

        private ConcurrentDictionary<string, object> _playerPropertiesRo = new();

        private readonly object _playerPropertiesLock = new();

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
            CachePlayerProperty($"{Constants.AttributePrefix}{attr}", value);
        }

        public void SetRemotePlayerProperty(int playerId, string key, object value)
        {
            if (!IsMasterClient)
            {
                Logging.LogWarning("Only room owner can send remote player properties.");
                return;
            }

            var hashtable = new PhotonHashtable
            {
                [key] = value
            };

            Logging.LogDebug("Sending remote player property: {Property} = {Value}", key, value);

            PhotonClient.OpSetCustomPropertiesOfActor(playerId, hashtable);
        }

        private ConcurrentDictionary<string, object> _monsterProperties = new();

        private ConcurrentDictionary<string, object> _monsterPropertiesRo = new();

        private readonly object _monsterPropertiesLock = new();

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

            if (value is not (FVector or FRotator))
            {
                Logging.LogDebug("Set monster property [{Guid}]: {Property} = {Value}", guid, prop, value);
            }
        }

        #region IConnectionCallbacks

        public void OnConnected()
        {
            Logging.LogInformation("Connected");
        }

        public async void OnConnectedToMaster()
        {
            try
            {
                Logging.LogInformation("Connected to master server: {ServerIp}", PhotonClient.RealtimePeer.ServerIpAddress);
                await JoinRandomOrCreateRoom();
            }
            catch (Exception e)
            {
                Logging.LogException(e);
            }
        }

        public void OnDisconnected(DisconnectCause cause)
        {
            JoinedRoomCallbacksDone = false;
            if (cause == DisconnectCause.DisconnectByClientLogic)
            {
                Logging.LogInformation("Disconnected: {Cause}", cause);
            }
            else
            {
                Logging.LogWarning("Disconnected: {Cause}", cause);
            }

            if (cause is DisconnectCause.ClientTimeout or DisconnectCause.ServerTimeout)
            {
                // something must've gone wrong, let's try to reconnect
                Logging.LogInformation("Attempting to reconnect...");
                if (!PhotonClient.ReconnectAndRejoin())
                {
                    Logging.LogWarning("Quick reconnect failed, attempting full reconnect...");
                    Reconnect();
                }
            }
        }

        public void OnRegionListReceived(RegionHandler regionHandler)
        {
            Logging.LogDebug("Region list received: {Regions}", regionHandler.AvailableRegionCodes);
            regionHandler.PingAvailableRegions(OnPingComplete);
        }

        private static void OnPingComplete(RegionHandler regionHandler)
        {
            Logging.LogDebug("Region ping complete: {PingResults}", regionHandler.GetResults());
        }

        public void OnCustomAuthenticationResponse(Dictionary<string, object> data)
        {
            foreach (var kvp in data)
            {
                Logging.LogDebug("Custom authentication response {Key}: {Value}", kvp.Key, kvp.Value);
            }
        }

        public void OnCustomAuthenticationFailed(string debugMessage)
        {
            Logging.LogError("Custom authentication failed: {Message}", debugMessage);
        }

        #endregion

        #region IMatchmakingCallbacks

        public void OnFriendListUpdate(List<FriendInfo> friendList)
        {
            Logging.LogDebug("Friend list update");
        }

        public void OnCreatedRoom()
        {
            Logging.LogInformation("Created room");
        }

        public void OnCreateRoomFailed(short returnCode, string message)
        {
            Logging.LogError("Create room failed [{Code}]: {Message}", returnCode, message);
        }

        private int GetTeamIdForPlayer()
        {
            Dictionary<int, int> teamsCount = [];
            var team1Id = Constants.AvailableTeamIds[0];
            var team2Id = Constants.AvailableTeamIds[1];
            teamsCount[team1Id] = 0;
            teamsCount[team2Id] = 0;

            foreach (var player in GetOtherPlayersInRoom())
            {
                if (player.CustomProperties.TryGetValue(nameof(PlayerState.TeamId), out var assignedTeamId))
                {
                    teamsCount[(int)assignedTeamId]++;
                }
            }

            return teamsCount[team1Id] > teamsCount[team2Id] ? team2Id : team1Id;
        }

        public void OnJoinedRoom()
        {
            Logging.LogInformation("Joined room {Name}", PhotonClient.CurrentRoom.Name);

            var teamId = GetTeamIdForPlayer();
            var controlledPawn = GameUtils.GetControlledPawn();

            if (controlledPawn.IsNullOrDestroyed())
            {
                Logging.LogError("Controlled pawn is null or destroyed.");
                return;
            }

            LocalPlayerState = new PlayerState(PhotonId, controlledPawn, teamId);
            CachePlayerProperty(nameof(PlayerState.TeamId), teamId);

            Utils.TryRunOnGameThread(PhotonUtils.DiscoverMonsters);

            SubscribeToPlayerMontageCallbacks();
            _joinedRoomCallback.Invoke();
            WukongChat.StartClient(PhotonClient.UserId);

            JoinedRoomCallbacksDone = true;
        }

        public void OnJoinRoomFailed(short returnCode, string message)
        {
            Logging.LogError("Join room failed [{Code}]: {Message}", returnCode, message);

            if (message == "Game does not exist")
            {
                // quick reconnect via PhotonClient.ReconnectAndRejoin failed, try normal reconnect
                Logging.LogWarning("Quick reconnect failed, attempting full reconnect...");
                Reconnect();
            }
        }

        public void OnJoinRandomFailed(short returnCode, string message)
        {
            Logging.LogError("Join random failed [{Code}]: {Message}", returnCode, message);
            JoinedRoomCallbacksDone = false;
        }

        public void OnLeftRoom()
        {
            Logging.LogInformation("Left room");
            JoinedRoomCallbacksDone = false;
        }

        #endregion

        public void OnPlayerEnteredRoom(Player newPlayer)
        {
            Logging.LogInformation("Player {Nickname} ({PlayerId}) entered the room", newPlayer.NickName, newPlayer.ActorNumber);
            _playerJoinedCallback.Invoke(newPlayer);
        }

        public void OnPlayerLeftRoom(Player otherPlayer)
        {
            Logging.LogInformation("Player {Nickname} ({PlayerId}) left the room", otherPlayer.NickName, otherPlayer.ActorNumber);

            if (ConnectedPlayers.Remove(otherPlayer.ActorNumber, out var playerState))
            {
                OnPlayerLeft?.Invoke(playerState);
            }
            else
            {
                Logging.LogWarning("Player {Id} not in ConnectedPlayers.", otherPlayer.ActorNumber);
            }

            if (IsMasterClient)
            {
                WukongChat.SendServerMessage($"{otherPlayer.NickName} has left!");
                CheckRoundEndCondition();
            }
        }

        public void OnRoomPropertiesUpdate(PhotonHashtable changedProps)
        {
            // empty, RoomState is a proxy to this hashtable
        }

        public void OnPlayerPropertiesUpdate(Player targetPlayer, PhotonHashtable changedProps)
        {
            var id = targetPlayer.ActorNumber;

            PlayerState playerState;

            if (targetPlayer.IsLocal)
            {
                playerState = LocalPlayerState;
            }
            else if (!ConnectedPlayers.TryGetValue(id, out playerState))
            {
                Logging.LogDebug("Player {Id} not found.", id); // TODO: Investigate why this is spammed
                return;
            }

            foreach (var kvp in changedProps)
            {
                if (kvp.Key is not string propertyName)
                {
                    if (kvp.Key is ActorProperties.NickName)
                    {
                        playerState.NickName = (string)kvp.Value;
                        Logging.LogDebug("Assigning NickName = {Nickname} for player {PlayerId}", playerState.NickName, id);
                    }
                    else
                    {
                        Logging.LogWarning("Unhandled player state key: {Key}", kvp.Key);
                    }

                    continue;
                }

                // attributes have special treatment
                if (propertyName.StartsWith(Constants.AttributePrefix))
                {
                    Logging.LogTrace("Assigning {Property} = {Value} for player {PlayerId}", propertyName, kvp.Value, id);

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
                    Logging.LogTrace("Assigning {Property} = {Value} for player {PlayerId}", propertyName, kvp.Value, id);
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
            // do nothing
        }

        private static readonly Dictionary<string, Action<PlayerState, object>> PlayerSetters = new();
        private static readonly Dictionary<string, Action<MonsterState, object>> MonsterSetters = new();

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