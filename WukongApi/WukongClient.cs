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
using ReadyM.Relay.Common.Protocol;
using ReadyM.Relay.Common.Protocol.Enums;
using UnrealEngine.Engine;
using UnrealEngine.Runtime;
using WukongApi.State;
using WukongApi.UI;
using Player = ReadyM.Relay.Client.Player;
using PlayerState = WukongApi.State.PlayerState;

namespace WukongApi
{
    public sealed class WukongClient
    {
        public readonly RelayClient RelayClient;

        private const char MonsterHashtableKeySeparator = ';';

        private int PeerId => RelayClient.LocalPlayer.PeerId; // is -1 before joining room
        public bool IsMasterClient => RoomState.MasterClientId == PeerId;
        public bool ConnectedAndInRoom => RelayClient.InRoom;

        private readonly Action _beforeJoinedRoomCallback;
        private readonly Action _afterJoinedRoomCallback;
        private readonly Action<int> _playerJoinedCallback;
        public event Action<MontageCallbackData>? OnMontageCallback;
        public event Action<int, int, string, string, int, float, float, float>? OnUnitSpawn;
        public event Action<int>? OnTeleportFinish;
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
        public event Action<int, int, bool>? OnTargetSet;
        public event Action? OnMatchmakingEnded;
        public event Action<int, int, float>? OnBuffAdded;
        public event Action<int, int, EBuffEffectTriggerType, int, bool>? OnBuffRemoved;
        public event Action<int, EBuffEffectTriggerType, bool>? OnBuffAllRemoved;
        public event Action<int, EBUStateTrigger, float, bool>? OnStateTriggerSet;
        public event Action<int, EBGUSimpleState, bool>? OnSimpleStateSet;
        public event Action<int, string>? OnFsmStateSet;
        public event Action<int, EState_MM>? OnMotionMatchingChanged;

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

        public readonly Dictionary<int, PlayerState> ConnectedPlayers = new();
        public readonly Dictionary<string, MonsterState> SyncedMonsters = new();

        public IEnumerable<PlayerState> AllConnectedPlayers
            => ConnectedPlayers.Values.Append(LocalPlayerState);

        public IEnumerable<PlayerState> SpectatingPlayers
            => ConnectedPlayers.Values.Where(p => p.IsSpectator).Concat(LocalPlayerState.IsSpectator ? [LocalPlayerState] : []);

        public IEnumerable<PlayerState> AllPvPPlayers
            => ConnectedPlayers.Values.Where(p => !p.IsSpectator).Concat(LocalPlayerState.IsSpectator ? [] : [LocalPlayerState]);

        public IEnumerable<CharacterState> AllPvPCharacters
            => ConnectedPlayers.Values.Where(p => !p.IsSpectator).ToList<CharacterState>().Concat(LocalPlayerState.IsSpectator ? [] : [LocalPlayerState]).Concat(SyncedMonsters.Values);

        public WukongClient(Action onBeforeJoinedRoom, Action onAfterJoindRoom, Action<int> playerJoinedCallback)
        {
            WukongChat = new WukongChatter(this);
            LobbyManager = new LobbyManager(this);
            RelayClient = new RelayClient(
                CmdLineParams.Instance.UserGuid,
                CmdLineParams.Instance.ServerIp!,
                CmdLineParams.Instance.ServerPort!.Value,
                (level, s, args) => Logging.Log(level, s, args.AsSpan())
            );
            RoomState = new RoomStateProxy(RelayClient);

            _beforeJoinedRoomCallback = onBeforeJoinedRoom;
            _afterJoinedRoomCallback = onAfterJoindRoom;
            _playerJoinedCallback = playerJoinedCallback;

            ConfigureRelay();
        }

        ~WukongClient()
        {
            Logging.LogInformation("WukongClient finalizer called");
            StopRelayClient();

            RelayClient.OnPingUpdated -= OnPingUpdated;
            RelayClient.OnCustomEvent -= OnCustomEvent;
            RelayClient.OnPlayerPropertiesChanged -= OnPlayerPropertiesChanged;
            RelayClient.OnRoomPropertiesChanged -= OnRoomPropertiesChanged;
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

        public MonsterState? GetMonsterByActor(AActor? actor)
        {
            if (actor == null)
                return null;

            return SyncedMonsters.FirstOrDefault(x => x.Value!.Pawn == actor).Value;
        }

        public CharacterState? GetCharacterByActor(AActor? actor)
        {
            var characterState = GetPlayerByActor(actor);
            return characterState == null ? GetMonsterByActor(actor) : characterState;
        }

        public PlayerState? GetPlayerById(int playerId)
        {
            return playerId == LocalPlayerState.PeerId
                ? LocalPlayerState
                : ConnectedPlayers.GetValueOrDefault(playerId);
        }

        public MonsterState? GetMonsterById(int monsterId)
        {
            return SyncedMonsters.Values.FirstOrDefault(x => x.PeerId == monsterId);
        }

        public CharacterState? GetCharacterById(int id)
        {
            var player = GetPlayerById(id);
            return player == null ? GetMonsterById(id) : player;
        }

        public MonsterState? GetByTamerActor(BUTamerActor owner)
        {
            return SyncedMonsters.FirstOrDefault(x => x.Value!.Tamer == owner).Value;
        }

        public void SetMasterClient(string newMasterName)
        {
            if (IsMasterClient)
            {
                var newMasterPlayer = AllConnectedPlayers.FirstOrDefault(x => x.NickName == newMasterName);
                if (newMasterPlayer != null)
                {
                    RoomState.MasterClientId = newMasterPlayer.PeerId;
                    WukongChat.SendServerMessage($"Master client: {newMasterName}");
                }
                else
                {
                    Logging.LogError("Player {PlayerName} not found", newMasterName);
                }
            }
        }

        private void SetReadyState(bool isReady)
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
                var teamId = GameUtils.GetOppositeTeam(LocalPlayerState.TeamId);
                CachePlayerProperty(nameof(PlayerState.TeamId), teamId);
            }
        }

        public MonsterState? GetMonsterByCharacter(BGUCharacterCS? owner)
        {
            if (owner == null)
                return null;

            var kvp = SyncedMonsters.FirstOrDefault(x => x.Value!.Tamer?.GetMonster() == owner);
            return kvp.Value;
        }

        public void RemoveSyncedMonster(MonsterState monster)
        {
            SyncedMonsters.Remove(monster.Guid);
        }

        private void OnPingUpdated(int ping)
        {
            PingIndicatorWidget.Instance.SetPingText(ping);
        }

        public void OnCustomEvent(CustomEventHeader header, NetPacketReader reader)
        {
            switch (header.EventCode)
            {
                case 1:
                    // unit spawn
                    var unitData = RelayClient.DeserializeObject<UnitSpawnData>(reader);
                    OnUnitSpawn?.Invoke(header.Sender, unitData.Id, unitData.Guid, unitData.Name, unitData.TeamId, unitData.X, unitData.Y, unitData.Z);
                    break;
                case 2:
                    // montage callback
                    var montData = RelayClient.DeserializeObject<MontageCallbackData>(reader);
                    OnMontageCallback?.Invoke(montData);
                    break;
                case 3:
                    // monster properties
                    var monsterData = RelayClient.DeserializeObject<Dictionary<object, object>>(reader);
                    ApplyMonsterMove(monsterData);
                    break;
                case 4:
                    // teleport finish
                    OnTeleportFinish?.Invoke(header.Sender);
                    break;
                case 5:
                    // monster wake up
                    var guid = RelayClient.DeserializeObject<string>(reader);
                    OnMonsterWakeUp?.Invoke(guid);
                    break;
                case 6:
                    // damage num
                    var damageNumParam = RelayClient.DeserializeObject<DamageNumParam>(reader);
                    OnDamageNum?.Invoke(damageNumParam);
                    break;
                case 7:
                {
                    // player rebirth
                    var playerId = RelayClient.DeserializeObject<int>(reader);
                    OnPlayerRebirth?.Invoke(playerId);
                    break;
                }
                case 8:
                    // PvP event
                    var ev = RelayClient.DeserializeObject<int[]>(reader);
                    HandlePvPEvent((PvPEvent)ev[0], ev[1]);
                    break;
                case 9:
                    // kill player
                    var id = RelayClient.DeserializeObject<int>(reader);
                    OnKillPlayer?.Invoke(id);
                    break;
                case 10:
                    // player transform
                    var playerData = RelayClient.DeserializeObject<PlayerTransformData>(reader);
                    if (playerData.PlayerId == LocalPlayerState.PeerId)
                        OnSetPlayerTransform?.Invoke(playerData.Location, playerData.Rotation);
                    break;
                case 11:
                    // start phantom rush
                    var direction = RelayClient.DeserializeObject<ESkillDirection>(reader);
                    OnPhantomRush?.Invoke(header.Sender, direction);
                    break;
                case 12:
                    // immobilize
                    var immobilizeData = RelayClient.DeserializeObject<ImmobilizeData>(reader);
                    OnHandleImmobilize?.Invoke(immobilizeData.PlayerId, immobilizeData.OtherPlayerId, immobilizeData.ImmobilizeActionType, immobilizeData.GreatSageTalentActiveBuff);
                    break;
                case 13:
                    // target
                    var targetData = RelayClient.DeserializeObject<int[]>(reader);
                    OnTargetSet?.Invoke(targetData[0], targetData[1], targetData[2] != 0);
                    break;
                case 14:
                    // exit phantom rush
                    var phantomRushPlayerId = RelayClient.DeserializeObject<int>(reader);
                    OnExitPhantomRush?.Invoke(phantomRushPlayerId);
                    break;
                case 15:
                    // end matchmaking phase
                    OnMatchmakingEnded?.Invoke();
                    break;
                case 16:
                    // buff add
                    var buffData = RelayClient.DeserializeObject<byte[]>(reader);
                    var buffId = BitConverter.ToInt32(buffData, 0);
                    var buffDuration = BitConverter.ToSingle(buffData, 4);
                    OnBuffAdded?.Invoke(header.Sender, buffId, buffDuration);
                    break;
                case 17:
                    // buff remove
                    var data = RelayClient.DeserializeObject<int[]>(reader);
                    OnBuffRemoved?.Invoke(header.Sender, data[0], (EBuffEffectTriggerType)data[1], data[2], data[3] != 0);
                    break;
                case 18:
                    // buff all remove
                    var evData = RelayClient.DeserializeObject<byte[]>(reader);
                    OnBuffAllRemoved?.Invoke(header.Sender, (EBuffEffectTriggerType)evData[0], evData[1] != 0);
                    break;
                case 19:
                    // state trigger
                    var stateTriggerData = RelayClient.DeserializeObject<StateTriggerData>(reader);
                    OnStateTriggerSet?.Invoke(stateTriggerData.CharacterId, stateTriggerData.Trigger, stateTriggerData.Time, stateTriggerData.NeedForceUpdate);
                    break;
                case 20:
                    // simple state
                    var simpleStateData = RelayClient.DeserializeObject<SimpleStateData>(reader);
                    OnSimpleStateSet?.Invoke(simpleStateData.CharacterId, simpleStateData.SimpleState, simpleStateData.IsRemove);
                    break;
                case 21:
                    // fsm state
                    var fsmStateData = RelayClient.DeserializeObject<FsmStateData>(reader);
                    OnFsmStateSet?.Invoke(fsmStateData.CharacterId, fsmStateData.FsmStateName);
                    break;
                case 22:
                    // motion matching
                    var motionaMatchingData = RelayClient.DeserializeObject<int[]>(reader);
                    OnMotionMatchingChanged?.Invoke(motionaMatchingData[0], (EState_MM)motionaMatchingData[1]);
                    break;
                case 23:
                    // chat message received
                    var chatMessage = RelayClient.DeserializeObject<ChatMessage>(reader);
                    WukongChat.OnGetMessage(chatMessage);
                    break;
            }
        }

        public void SendChatMessage(ChatMessage message)
        {
            const byte eventCode = 23;
            RelayClient.OpRaiseEvent(eventCode, message, EventCaching.AddToRoomCacheGlobal);
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
                        if (IsMasterClient)
                        {
                            foreach (var playerState in WukongMP.Instance.Client.SpectatingPlayers)
                            {
                                SetRemotePlayerProperty(playerState.PeerId, nameof(PlayerState.IsSpectator), false);
                            }
                        }

                        await Task.Delay(2000);
                        WukongMP.Instance.EndTournament(winnerTeamId);
                        ExitPvP();
                        LocalPlayerState.IsReadyForPvP = false;
                        SetReadyState(false);
                    });

                    break;
                }
                case PvPEvent.ResetStats:
                    WukongMP.Instance.ResetRoundState();

                    if (!LocalPlayerState.IsDead)
                    {
                        Utils.TryRunOnGameThread(() =>
                        {
                            GameUtils.DestroyAllTamers();
                            var events = BUS_EventCollectionCS.Get(LocalPlayerState.Pawn!);

                            if (events == null)
                            {
                                Logging.LogError("events are null");
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
            if (!IsMasterClient || !RoomState.InPvP)
            {
                return;
            }

            // check if all players but one are dead
            var players = AllPvPPlayers.ToList();
            var alivePlayers = players.Where(p => !p.IsDead).ToList<CharacterState>();
            var aliveCharacters = alivePlayers.Concat(SyncedMonsters.Values.Where(m => !m.IsDead)).ToList();
            var aliveCharactersTeams = aliveCharacters.Select(p => p.TeamId).Distinct().Count();

            var aliveTeams = aliveCharacters
                .Select(character => character.TeamId)
                .GroupBy(teamId => teamId)
                .Select(group => new { TeamId = group.Key, Count = group.Count() })
                .OrderByDescending(item => item.Count).ToList();

            if (alivePlayers.Count == 0)
            {
                Logging.LogInformation("All players are dead, ending round");
                var aliveTeamId = aliveTeams.Count > 0 ? aliveTeams[0].TeamId : Constants.DrawTeamId;
                if (aliveCharacters.Count == 0)
                {
                    Task.Run(async () => await LobbyManager.EndRoundAsync(GameUtils.GetOppositeTeam(aliveTeamId)));
                }
                else
                {
                    Task.Run(async () => await LobbyManager.EndRoundAsync(aliveTeamId));
                }

                return;
            }

            if (aliveCharactersTeams == 1)
            {
                Logging.LogInformation("One team with alive players, ending round");
                var winner = players.First(p => !p.IsDead);
                Task.Run(async () => await LobbyManager.EndRoundAsync(winner.TeamId));
            }
        }

        private void EnterPvP()
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

        private void ExitPvP()
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

        private void OnPlayerReadinessChanged(string playerNickname, bool isReady)
        {
            var playersReady = ConnectedPlayers.Values.Count(x => x.IsReadyForPvP) + (LocalPlayerState.IsReadyForPvP ? 1 : 0);
            OnReadinessChange?.Invoke(playerNickname, isReady, playersReady);
        }

        public void Reconnect()
        {
            Logging.LogInformation("Attempting to reconnect...");
            StopRelayClient();
            StartClient();
        }

        private void ConfigureRelay()
        {
            RelayClient.RegisterType(typeof(ChatMessage), ChatMessage.Serialize, ChatMessage.Deserialize);
            RelayClient.RegisterType(typeof(UnitSpawnData), UnitSpawnData.Serialize, UnitSpawnData.Deserialize);
            RelayClient.RegisterType(typeof(FVector), SerializationHelpers.SerializeFVector, SerializationHelpers.DeserializeFVector);
            RelayClient.RegisterType(typeof(FRotator), SerializationHelpers.SerializeFRotator, SerializationHelpers.DeserializeFRotator);
            RelayClient.RegisterType(typeof(MontageCallbackData), MontageCallbackData.Serialize, MontageCallbackData.Deserialize);
            RelayClient.RegisterType(typeof(EquipmentState), EquipmentState.Serialize, EquipmentState.Deserialize);
            RelayClient.RegisterType(typeof(DamageNumParam), SerializationHelpers.SerializeDamageNumParam, SerializationHelpers.DeserializeDamageNumParam);
            RelayClient.RegisterType(typeof(PlayerTransformData), PlayerTransformData.Serialize, PlayerTransformData.Deserialize);
            RelayClient.RegisterType(typeof(ImmobilizeData), ImmobilizeData.Serialize, ImmobilizeData.Deserialize);
            RelayClient.RegisterType(typeof(FsmStateData), FsmStateData.Serialize, FsmStateData.Deserialize);
            RelayClient.RegisterType(typeof(StateTriggerData), StateTriggerData.Serialize, StateTriggerData.Deserialize);
            RelayClient.RegisterType(typeof(SimpleStateData), SimpleStateData.Serialize, SimpleStateData.Deserialize);

            RelayClient.OnPingUpdated += OnPingUpdated;
            RelayClient.OnCustomEvent += OnCustomEvent;
            RelayClient.OnPlayerPropertiesChanged += OnPlayerPropertiesChanged;
            RelayClient.OnRoomPropertiesChanged += OnRoomPropertiesChanged;
            RelayClient.OnBeforeJoinedRoom += OnBeforeJoinedRoomHandler;
            RelayClient.OnAfterJoinedRoom += OnAfterJoinedRoomHandler;
            RelayClient.OnDisconnected += OnDisconnectedHandler;
            RelayClient.OnOtherPlayerJoined += OtherPlayerJoinedRoomHandler;
            RelayClient.OnOtherPlayerLeft += OnPlayerLeftRoomHandler;
        }

        public void StartClient()
        {
            OnBeforeJoinRoom?.Invoke();
            RelayClient.Start();
            Logging.LogInformation("Client started");
        }

        public void StopRelayClient()
        {
            Logging.LogInformation("Stopping relay client...");

            if (GameUtils.IsWorldValid())
            {
                UnsubscribeFromPlayerEvents();
            }

            RelayClient.Stop();

            // clear the chat window
            ChatWidget.Instance.ClearMessages();

            // destroy all connected players
            foreach (var player in ConnectedPlayers.Values)
            {
                WukongMP.Instance.RemovePlayer(player);
            }

            // clear state
            ConnectedPlayers.Clear();
            Utils.TryRunOnGameThread(WukongMP.Instance.DestroySyncedMonsters);
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

        public void SpawnUnit(int id, string guid, string unitName, int teamId, float x, float y, float z)
        {
            const byte eventCode = 1;
            var evData = new UnitSpawnData(id, guid, unitName, teamId, x, y, z);
            RelayClient.OpRaiseEvent(eventCode, evData, RelayMode.Others, DeliveryMethod.ReliableOrdered);
        }

        private void SpawnUnitForNewPlayer(int playerId, int id, string guid, string unitName, int teamId, float x, float y, float z)
        {
            const byte eventCode = 1;
            var evData = new UnitSpawnData(id, guid, unitName, teamId, x, y, z);
            RelayClient.OpRaiseEvent(eventCode, evData, [playerId], DeliveryMethod.ReliableOrdered);
        }

        public void SendMontageCallback(int characterId, UAnimMontage montage, float position, bool reset)
        {
            Logging.LogDebug("Sending montage callback: {Montage} {Position}", montage.PathName, position);
            const byte eventCode = 2;

            var shortened = MontageHelpers.CompressMontageName(montage.PathName, out var shortMontagePath);
            var data = shortened ? shortMontagePath : montage.PathName;
            var evData = new MontageCallbackData(characterId, shortened, data, position, reset);

            RelayClient.OpRaiseEvent(eventCode, evData, RelayMode.Others, DeliveryMethod.ReliableOrdered);
        }

        public void SendMontageCancel(int characterId)
        {
            Logging.LogDebug("Sending montage cancel");
            const byte eventCode = 2;

            var evData = new MontageCallbackData(characterId, false, "", 0f, false);

            RelayClient.OpRaiseEvent(eventCode, evData, RelayMode.Others, DeliveryMethod.ReliableOrdered);
        }

        public void SendTeleportFinish()
        {
            const byte eventCode = 4;
            RelayClient.OpRaiseEvent(eventCode, null, RelayMode.Master, DeliveryMethod.ReliableOrdered);
        }

        public void SendMonsterWakeUp(string guid)
        {
            const byte eventCode = 5;
            RelayClient.OpRaiseEvent(eventCode, guid, RelayMode.Others, DeliveryMethod.ReliableOrdered);
        }

        public void SendDamageNum(DamageNumParam damageNumParam)
        {
            const byte eventCode = 6;
            RelayClient.OpRaiseEvent(eventCode, damageNumParam, RelayMode.Others, DeliveryMethod.ReliableOrdered);
        }

        public void BroadcastPlayerRebirth(int playerId)
        {
            const byte eventCode = 7;
            RelayClient.OpRaiseEvent(eventCode, playerId, RelayMode.All, DeliveryMethod.ReliableOrdered);
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
            RelayClient.OpRaiseEvent(eventCode, evData, RelayMode.All, DeliveryMethod.ReliableOrdered);
        }

        public void KillCurrentPlayer()
        {
            const byte eventCode = 9;
            RelayClient.OpRaiseEvent(eventCode, PeerId, RelayMode.Master, DeliveryMethod.ReliableOrdered);
        }

        public void BroadcastPlayerTransform(int playerId, FVector location, FRotator rotation)
        {
            const byte eventCode = 10;
            var evData = new PlayerTransformData(playerId, location, rotation);
            RelayClient.OpRaiseEvent(eventCode, evData, RelayMode.All, DeliveryMethod.ReliableOrdered);
        }

        public void SendPhantomRush(ESkillDirection phantomRushDir)
        {
            const byte eventCode = 11;
            RelayClient.OpRaiseEvent(eventCode, phantomRushDir, RelayMode.Others, DeliveryMethod.ReliableOrdered);
        }

        public void BroadcastImmobilize(int playerId, int otherPlayerId, ImmobilizeActionType immobilizeActionType, bool hasBuff)
        {
            const byte eventCode = 12;
            var evData = new ImmobilizeData(playerId, otherPlayerId, immobilizeActionType, hasBuff);
            RelayClient.OpRaiseEvent(eventCode, evData, RelayMode.Others, DeliveryMethod.ReliableOrdered);
        }

        public void SendTarget(int characterId, int targetId, int clearTarget)
        {
            const byte eventCode = 13;
            int[] evData = [characterId, targetId, clearTarget];
            RelayClient.OpRaiseEvent(eventCode, evData, RelayMode.Others, DeliveryMethod.ReliableOrdered);
        }

        public void ExitPhantomRush(int playerId)
        {
            const byte eventCode = 14;
            RelayClient.OpRaiseEvent(eventCode, playerId, RelayMode.Others, DeliveryMethod.ReliableOrdered);
        }

        public void SendEndMatchmaking()
        {
            const byte eventCode = 15;
            RelayClient.OpRaiseEvent(eventCode, null, RelayMode.All, DeliveryMethod.ReliableOrdered);
        }

        public void SendUnitStateTrigger(int characterId, EBUStateTrigger trigger, float time, bool needForceUpdate)
        {
            const byte eventCode = 19;
            var evData = new StateTriggerData(characterId, trigger, time, needForceUpdate);
            RelayClient.OpRaiseEvent(eventCode, evData, RelayMode.Others, DeliveryMethod.ReliableOrdered);
        }

        public void SendUnitSimpleState(int characterId, EBGUSimpleState simpleState, bool isRemove)
        {
            const byte eventCode = 20;
            var evData = new SimpleStateData(characterId, simpleState, isRemove);
            RelayClient.OpRaiseEvent(eventCode, evData, RelayMode.Others, DeliveryMethod.ReliableOrdered);
        }

        public void SendTriggerFsmState(int characterId, FGameplayTag eventTag)
        {
            const byte eventCode = 21;
            var evData = new FsmStateData(characterId, eventTag.TagName.ToString());
            RelayClient.OpRaiseEvent(eventCode, evData, RelayMode.Others, DeliveryMethod.ReliableOrdered);
        }

        public void SendMotionMatchingState(int characterId, EState_MM MMState)
        {
            const byte eventCode = 22;
            int[] evData = [characterId, (int)MMState];
            RelayClient.OpRaiseEvent(eventCode, evData, RelayMode.Others, DeliveryMethod.ReliableOrdered);
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

        private void ApplyMonsterMove(Dictionary<object, object> props)
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

        public void SetRemotePlayerProperty(int playerId, string key, object value)
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

            RelayClient.OpSetCustomPropertiesOfActor(playerId, hashtable);
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

                var hashtable = new Dictionary<object, object>();
                foreach (var (key, value) in _monsterPropertiesRo)
                {
                    hashtable[key] = value;
                }

                _monsterPropertiesRo.Clear();

                const byte eventCode = 3;
                RelayClient.OpRaiseEvent(eventCode, hashtable, RelayMode.Others, DeliveryMethod.ReliableOrdered);
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

        private void SubscribeToPlayerEvents()
        {
            var events = BUS_EventCollectionCS.Get(LocalPlayerState.Pawn);
            events.Evt_BuffAdd += HandleBuffAdd;
            events.Evt_BuffRemove += HandleBuffRemove;
            events.Evt_BuffRemoveImmediately += HandleBuffRemoveImmediately;
            events.Evt_BuffAllRemove += HandleBuffAllRemove;
        }

        private void UnsubscribeFromPlayerEvents()
        {
            var myPawn = GameUtils.GetControlledPawn();

            if (myPawn == null)
                return;

            var events = BUS_EventCollectionCS.Get(myPawn);

            if (events != null)
            {
                events.Evt_BuffAdd -= HandleBuffAdd;
                events.Evt_BuffRemove -= HandleBuffRemove;
                events.Evt_BuffRemoveImmediately -= HandleBuffRemoveImmediately;
                events.Evt_BuffAllRemove -= HandleBuffAllRemove;
            }
        }

        private void HandleBuffAllRemove(EBuffEffectTriggerType removetriggertype, bool withtriggerremmoveeffect)
        {
            const byte eventCode = 18;
            byte[] evData = [(byte)removetriggertype, (byte)(withtriggerremmoveeffect ? 1 : 0)];
            RelayClient.OpRaiseEvent(eventCode, evData, RelayMode.Others, DeliveryMethod.ReliableOrdered);
        }

        private void HandleBuffRemove(int buffid, EBuffEffectTriggerType removetriggertype, int layer, bool withtriggerremmoveeffect)
        {
            const byte eventCode = 17;
            int[] evData = [buffid, (int)removetriggertype, layer, withtriggerremmoveeffect ? 1 : 0];
            RelayClient.OpRaiseEvent(eventCode, evData, RelayMode.Others, DeliveryMethod.ReliableOrdered);
        }

        private void HandleBuffRemoveImmediately(int buffid, EBuffEffectTriggerType removetriggertype, bool withtriggerremmoveeffect)
            => HandleBuffRemove(buffid, removetriggertype, -1, withtriggerremmoveeffect);

        private void HandleBuffAdd(int buffid, AActor caster, AActor rootcaster, float duration, EBuffSourceType buffsourcetype, bool brecursed, FBattleAttrSnapShot battleattrsnapshot)
        {
            const byte eventCode = 16;
            byte[] evData = BitConverter.GetBytes(buffid).Concat(BitConverter.GetBytes(duration)).ToArray();
            RelayClient.OpRaiseEvent(eventCode, evData, RelayMode.Others, DeliveryMethod.ReliableOrdered);
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

            Utils.TryRunOnGameThread(ClientUtils.DiscoverMonsters);

            SubscribeToPlayerEvents();
            _beforeJoinedRoomCallback.Invoke();

            WukongChat.SendServerMessage($"{LocalPlayerState.NickName} has joined!");
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

        private void OtherPlayerJoinedRoomHandler(int playerId)
        {
            Logging.LogInformation("Player {PlayerId} entered the room", playerId);

            _playerJoinedCallback.Invoke(playerId);

            // send current monsters to the new player 
            foreach (var monsterState in SyncedMonsters.Values)
            {
                SpawnUnitForNewPlayer(playerId, monsterState.PeerId, monsterState.Guid, monsterState.UnitName, monsterState.TeamId, monsterState.Location.X, monsterState.Location.Y, monsterState.Location.Z);
            }
        }

        private void OnPlayerLeftRoomHandler(int playerId)
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
                WukongChat.SendServerMessage($"{nickname} has left!");

                _ = Task.Run(async () =>
                {
                    await Task.Delay(Constants.PlayerTtlMs);
                    CheckRoundEndCondition();
                });
            }
        }

        private void OnPlayerPropertiesChanged(int playerId, Dictionary<object, object?> changes)
        {
            PlayerState playerState;

            if (playerId == RelayClient.LocalPlayer.PeerId) // local player
            {
                if (_localPlayerState == null)
                {
                    Logging.LogWarning("Local player state is null.");
                    return;
                }

                playerState = LocalPlayerState;
            }
            else if (!ConnectedPlayers.TryGetValue(playerId, out playerState))
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
                        OnEquipmentChange?.Invoke(playerId, (EquipmentState)kvp.Value);
                        break;
                    case nameof(PlayerState.IsReadyForPvP):
                        var state = RelayClient.GetPlayerState(playerId);

                        if (state == null)
                        {
                            Logging.LogError("Player {Id} not found.", playerId);
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
                        Logging.LogDebug("Player {Id} spectator status changed: {Spectator}", playerId, isSpectator);

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

        private void OnRoomPropertiesChanged(Dictionary<object, object?> diff)
        {
            if (diff.TryGetValue(RoomProperties.MasterClientId, out var id) && id is int newMasterId)
            {
                Logging.LogInformation("Master client changed to {NewMasterId}", newMasterId);
            }
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