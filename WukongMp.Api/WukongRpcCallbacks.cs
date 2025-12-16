using System;
using System.Reflection;
using b1;
using b1.BGW;
using HarmonyLib;
using Microsoft.Extensions.Logging;
using ReadyM.Api.Multiplayer.Client;
using ReadyM.Api.Multiplayer.ECS.Values;
using ReadyM.Api.Multiplayer.Generators;
using ReadyM.Api.Multiplayer.Idents;
using ReadyM.Api.Multiplayer.Protocol.Enums;
using ReadyM.Relay.Client;
using ReadyM.Relay.Client.State;
using ReadyM.Relay.Common.Serialization;
using UnrealEngine.Engine;
using UnrealEngine.Runtime;
using WukongMp.Api.DTO;
using WukongMp.Api.ECS.Entities;
using WukongMp.Api.NameCompressors;
using WukongMp.Api.State;
using WukongMp.Api.UI;
using WukongMp.Api.WukongUtils;

namespace WukongMp.Api;

public partial class WukongRpcCallbacks : IDisposable
{
    protected readonly RelaySerializer Serializer;
    protected readonly IRelayClient RelayClient;
    private readonly ClientState _state;
    private readonly WukongAreaState _areaState;
    private readonly ClientNetworkedEntityState _netEntity;
    private readonly WukongPlayerState _playerState;
    private readonly WukongPawnState _pawnState;
    private readonly ClientOwnershipManager _clientOwnership;
    private readonly FreeCameraManager _freeCameraManager;
    private readonly GameplayEventRouter _eventRouter;
    private readonly IClientEcsUpdateLoop _ecsLoop;
    private readonly ILogger _logger;

    public WukongRpcCallbacks(
        RelaySerializer serializer,
        IRelayClient relayClient,
        ClientState state,
        WukongAreaState areaState,
        ClientNetworkedEntityState netEntity,
        WukongPlayerState playerState,
        WukongPawnState pawnState,
        ClientOwnershipManager clientOwnership,
        FreeCameraManager freeCameraManager,
        GameplayEventRouter eventRouter,
        IClientEcsUpdateLoop ecsLoop,
        ILogger logger)
    {
        Serializer = serializer;
        RelayClient = relayClient;
        _state = state;
        _areaState = areaState;
        _netEntity = netEntity;
        _playerState = playerState;
        _pawnState = pawnState;
        _clientOwnership = clientOwnership;
        _freeCameraManager = freeCameraManager;
        _eventRouter = eventRouter;
        _ecsLoop = ecsLoop;
        _logger = logger;

        InitRpc();
    }

    public void Dispose()
    {
        DeInitRpc();
    }

    public void SendMontageCallback(NetworkId netId, UAnimMontage montage, float position, bool reset)
    {
        var shortened = Compressors.MontageNameCompressor.Compress(montage.PathName, out var shortMontagePath);
        var data = shortened ? shortMontagePath : montage.PathName;
        var evData = new MontageCallbackData(netId, shortened, data, position, reset);
        SendMontageCallback(evData);
    }

    public void SendMontageCancel(NetworkId netId)
    {
        var evData = new MontageCallbackData(netId, false, "", 0f, false);
        SendMontageCallback(evData);
    }

    public void SendTriggerMagicallyChange(PlayerId player, UBGWDataAsset config, int skillID, int recoverSkillID, int curVigorSkillID, ECastReason_MagicallyChange castReason)
    {
        var shortened = Compressors.VigorNameCompressor.Compress(config.PathName, out var shortMontagePath);
        var configName = shortened ? shortMontagePath : config.PathName;
        _logger.LogDebug("Sending magically change for player {PlayerId} with config {Config}, skillID {SkillID}, recoverSkillID {RecoverSkillId}, curVigorSkillID {CurVigorSkillID}, castReason {CastReason}", player, configName, skillID, recoverSkillID, curVigorSkillID, castReason);
        var evData = new MagicallyChangeData(configName, shortened, skillID, recoverSkillID, curVigorSkillID, castReason);
        SendTriggerMagicallyChange(evData);
    }

    public void SendPlayMovieRequest(FPlayMovieRequest playMovieRequest)
    {
        SendPlayMovieRequest(new PlayMovieData(
            playMovieRequest.SequenceID,
            playMovieRequest.bDisablePlayerControl,
            playMovieRequest.bDisableMovementInput,
            playMovieRequest.bDisableLookAtInput,
            playMovieRequest.bHidePlayer,
            playMovieRequest.bHideHud,
            "",
            playMovieRequest.MatchType));
    }

    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    internal void OnExitPhantomRush(PlayerId playerId)
    {
        _ecsLoop.Scheduler.Schedule(static (_, self, playerId0) =>
        {
            if (self._playerState.GetMainCharacterById(playerId0) is not { } mainEntity)
                return;
            ref var mainComp = ref mainEntity.GetState();
            ref var localMainComp = ref mainEntity.GetLocalState();
            self._logger.LogDebug("Received exit phantom rush for player {Nickname}", mainComp.CharacterNickName);
            var events = BUS_EventCollectionCS.Get(localMainComp.Pawn);
            localMainComp.ReceivedPhantomRushExit = true;
            events?.Evt_RelievePhantomRush.Invoke();
        }, this, playerId);
    }

    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    internal void OnAddBuff(PlayerId __sender, BuffAddData data)
    {
        _ecsLoop.Scheduler.Schedule(static (_, self, data0) =>
        {
            var pawn = self._pawnState.GetPawnByNetworkId(data0.Id);

            if (pawn == null)
                return;

            BuffUtils.AddBuff(pawn, data0.BuffId, data0.Duration);
        }, this, data);
    }

    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    internal void OnRemoveBuff(PlayerId __sender, BuffRemoveData data)
    {
        _ecsLoop.Scheduler.Schedule(static (_, self, sender, data0) =>
        {
            var pawn = self._pawnState.GetPawnByNetworkId(data0.Id);

            if (pawn == null)
                return;

            BuffUtils.RemoveBuff(pawn, data0.BuffId, data0.TriggerType, data0.Layer, data0.WithTriggerRemoveEffect);
        }, this, __sender, data);
    }

    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    internal void OnRemoveAllBuffs(PlayerId __sender, BuffRemoveAllData data)
    {
        _ecsLoop.Scheduler.Schedule(static (_, self, sender, data0) =>
        {
            var pawn = self._pawnState.GetPawnByNetworkId(data0.Id);

            if (pawn == null)
                return;

            BuffUtils.RemoveAllBuffs(pawn, data0.TriggerType, data0.WithTriggerRemoveEffect);
        }, this, __sender, data);
    }

    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    internal void OnUnitStateTrigger(StateTriggerData data)
    {
        _ecsLoop.Scheduler.Schedule(static (_, self, data0) =>
        {
            var character = self._pawnState.GetPawnByNetworkId(data0.NetId);
            NpcLocomotionUtils.SetStateTrigger(character, data0.Trigger, data0.Time, data0.NeedForceUpdate);
        }, this, data);
    }

    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    internal void OnUnitSimpleState(SimpleStateData data)
    {
        _ecsLoop.Scheduler.Schedule(static (_, self, data0) =>
        {
            var character = self._pawnState.GetPawnByNetworkId(data0.NetId);
            NpcLocomotionUtils.SetSimpleState(character, data0.SimpleState, data0.IsRemove);
        }, this, data);
    }

    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    internal void OnTriggerFsmState(FsmStateData data)
    {
        _ecsLoop.Scheduler.Schedule(static (_, self, data0) =>
        {
            var character = self._pawnState.GetPawnByNetworkId(data0.NetId);
            NpcLocomotionUtils.SetFsmState(character, data0.FsmStateName);
        }, this, data);
    }

    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    internal void OnMotionMatchingState(MotionMatchingStateData data)
    {
        _ecsLoop.Scheduler.Schedule(static (_, self, data0) =>
        {
            var character = self._pawnState.GetPawnByNetworkId(data0.NetId);
            NpcLocomotionUtils.SetMotionMatchingState(character, data0.State);
        }, this, data);
    }

    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    internal void OnSpawnSummon(SummonRequestData data)
    {
        _ecsLoop.Scheduler.Schedule(static (_, self, data0) =>
        {
            self._logger.LogDebug("Received OnSpawnSummon for summoner {Summoner} with guid {Guid} for tamer path {Path}", data0.SummonerId, data0.SummonGuid, data0.SummonClassPath);
            SpawningUtils.SpawnSummonedUnitWithGuid(data0.ToGame());
        }, this, data);
    }

    [RpcEvent(RelayMode.AreaOfInterestAll)]
    internal void OnRequestSpawnUnits(PlayerId __sender, UnitSpawnRequestData data)
    {
        _ecsLoop.Scheduler.Schedule(static (_, self, data0) =>
        {
            if (self._areaState.IsMasterClient)
            {
                SpawningUtils.SpawnUnitsAsOwner(data0.UnitName, data0.Count, data0.TeamId, data0.Location);
            }
        }, this, data);
    }

    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    internal void OnSpawnUnit(PlayerId __sender, UnitSpawnData data)
    {
        _ecsLoop.Scheduler.Schedule(static (_, data0) => { SpawningUtils.SpawnUnitLocallyByName(data0.Guid, data0.UnitName, data0.Location); }, data);
    }

    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    internal void OnPlayerTransBegin(PlayerId __sender, PlayerTransBeginData data)
    {
        _ecsLoop.Scheduler.Schedule(static (_, self, sender, data0) =>
        {
            if (self._playerState.GetMainCharacterById(sender) is not { } mainEntity)
                return;
            TransformationUtils.TransformPlayer(mainEntity, data0.UnitResId, data0.UnitBornSkillId, data0.EnableBlendViewTarget, data0.TransBeginType);
        }, this, __sender, data);
    }

    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    internal void OnPlayerTransEnd(PlayerId __sender, PlayerTransEndData data)
    {
        _ecsLoop.Scheduler.Schedule(static (_, self, sender, data0) =>
        {
            if (self._playerState.GetMainCharacterById(sender) is not { } mainEntity)
                return;
            TransformationUtils.TransformPlayerBack(mainEntity, data0.UnitResId, data0.UnitBornSkillId, data0.EnableBlendViewTarget, data0.TransEndType);
        }, this, __sender, data);
    }

    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    internal void OnPlayMovieRequest(PlayMovieData data)
    {
        _ecsLoop.Scheduler.Schedule(static (_, data0) => { CutsceneUtils.PlayCutscene(data0); }, data);
    }

    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    internal void OnSetTarget(TargetData data)
    {
        _ecsLoop.Scheduler.Schedule(static (_, self, data0) =>
        {
            var pawn = self._pawnState.GetPawnByNetworkId(data0.Character);
            if (pawn == null)
            {
                self._logger.LogNullDebug(nameof(data0.Character));
                return;
            }

            if (data0.ClearTarget)
            {
                TargetingUtils.ClearTarget(pawn);
                return;
            }

            var target = self._pawnState.GetPawnByNetworkId(data0.Target);

            if (target == null)
            {
                self._logger.LogNull(nameof(data0.Target));
                return;
            }

            TargetingUtils.SetTarget(pawn, target);
        }, this, data);
    }

    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    internal void OnCastImmobilize(NetworkId caster)
    {
        _ecsLoop.Scheduler.Schedule(static (_, self, caster0) =>
        {
            if (self._areaState.IsMasterClient)
            {
                var character = self._pawnState.GetPawnByNetworkId(caster0);
                if (character == null)
                {
                    self._logger.LogNull(nameof(caster0));
                    return;
                }

                ImmobilizeUtils.CastImmobilize(character);
            }
        }, this, caster);
    }

    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    internal void OnTriggerImmobilize(TriggerImmobilizeData data)
    {
        _ecsLoop.Scheduler.Schedule(static (_, self, data0) =>
        {
            var caster = self._pawnState.GetPawnByNetworkId(data0.PlayerId);
            var target = self._pawnState.GetPawnByNetworkId(data0.Target);
            ImmobilizeUtils.TriggerImmobilize(caster, target, data0.GreatSageTalentActiveBuff);
        }, this, data);
    }

    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    internal void OnRelieveImmobilize(NetworkId affected)
    {
        _ecsLoop.Scheduler.Schedule(static (_, self, affected0) =>
        {
            var character = self._pawnState.GetPawnByNetworkId(affected0);
            if (character == null)
            {
                self._logger.LogNullDebug(nameof(affected0));
                return;
            }

            ImmobilizeUtils.RelieveImmobilize(character);
        }, this, affected);
    }

    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    internal void OnBreakImmobilize(NetworkId netId)
    {
        // TODO
    }

    [Obsolete("To be removed once per-file RPC is implemented")]
    public event Action<ChatMessage>? OnGetChatMessage;

    [RpcEvent(RelayMode.AreaOfInterestAll)]
    internal void OnChatMessage(ChatMessage message)
    {
        _ecsLoop.Scheduler.Schedule(static (_, self, message0) => { self.OnGetChatMessage?.Invoke(message0); }, this, message);
    }

    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    internal void OnPhantomRush(PlayerId __sender, ESkillDirection direction)
    {
        _ecsLoop.Scheduler.Schedule(static (_, self, sender, direction0) =>
        {
            if (self._playerState.GetMainCharacterById(sender) is not { } senderEntity)
                return;
            ref var senderMainComp = ref senderEntity.GetState();
            ref var senderLocalMainComp = ref senderEntity.GetLocalState();
            if (senderLocalMainComp.Pawn == null)
            {
                self._logger.LogError("Player not found: {PlayerId}", senderMainComp.PlayerId);
                return;
            }

            self._logger.LogDebug("Received phantom rush for player {Nickname} in direction {Direction}", senderMainComp.CharacterNickName, direction0);
            var events = BUS_EventCollectionCS.Get(senderLocalMainComp.Pawn);
            events?.Evt_TriggerPhantomRush.Invoke(direction0);

            // reset mana and cooldowns of the sender's pawn, since it's a remote player who needs to keep track of them
            PlayerUtils.ResetCooldown(senderLocalMainComp.Pawn);
            PlayerUtils.ResetMana(senderLocalMainComp.Pawn);

            // unattach tracking camera if target was the sender
            if (self._playerState.LocalMainCharacter.HasValue)
            {
                var localPlayer = self._playerState.LocalMainCharacter.Value.GetLocalState().Pawn;
                if (localPlayer != null)
                {
                    var targetData = BGU_DataUtil.GetReadOnlyData<IBUC_TargetInfoData, BUC_TargetInfoData>(localPlayer);
                    if (targetData?.GetTargetInfo()?.LockTargetActor == senderLocalMainComp.Pawn)
                    {
                        var localEvents = BUS_EventCollectionCS.Get(localPlayer);
                        localEvents.Evt_ClearCameraLock?.Invoke();
                    }
                }
            }
        }, this, __sender, direction);
    }

    [RpcEvent(RelayMode.AreaOfInterestAll)]
    public void OnBroadcastPlayerTransform(PlayerTransformData data)
    {
        _ecsLoop.Scheduler.Schedule(static (_, self, data0) =>
        {
            if (self._playerState.LocalMainCharacter is not { } mainEntity)
                return;

            ref var mainComp = ref mainEntity.GetState();
            if (data0.PlayerId != mainComp.PlayerId)
                return;

            PlayerUtils.TeleportLocalPlayer(mainEntity, data0.Location, data0.Rotation, true);
        }, this, data);
    }

    [RpcEvent(RelayMode.AreaOfInterestAll)]
    internal void OnRebirthPlayer(PlayerId playerId)
    {
        _ecsLoop.Scheduler.Schedule(static (_, self, playerId0) =>
        {
            self._logger.LogDebug("RebirthPlayer for player {PlayerId} called", playerId0);

            if (self._playerState.GetMainCharacterById(playerId0) is not { } mainEntity)
                return;

            if (playerId0 == self._state.LocalPlayerId)
            {
                self._freeCameraManager.LeaveFreeCameraMode();
            }

            ref var localMainComp = ref mainEntity.GetLocalState();
            if (localMainComp.Pawn != null)
            {
                PlayerUtils.RebirthPlayerInPlace(localMainComp.Pawn);
            }
        }, this, playerId);
    }

    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    internal void OnDamageNum(DamageNumParam damageNum)
    {
        _ecsLoop.Scheduler.Schedule(static (_, damageNum0) =>
        {
            var uiEvt = BGW_UIEventCollection.Get(GameUtils.GetWorld());
            uiEvt.Evt_UI_ShowHPChangeNum(damageNum0);
        }, damageNum);
    }

    [RpcEvent(RelayMode.AreaOfInterestAll)]
    internal void OnTeleportFinish(PlayerId __sender)
    {
        _ecsLoop.Scheduler.Schedule(static (_, self, sender) =>
        {
            if (self._playerState.GetMainCharacterById(sender) is not { } mainEntity)
            {
                self._logger.LogError("Player not found: {PlayerId}", sender);
                return;
            }

            ref var localMainComp = ref mainEntity.GetLocalState();
            var events = BUS_EventCollectionCS.Get(localMainComp.Pawn);
            events?.Evt_UnitStateTrigger.Invoke(EBUStateTrigger.TeleportEnd, -1f);
            events?.Evt_TeleportFinish.Invoke();
        }, this, __sender);
    }

    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    public void OnMontageCallback(MontageCallbackData data)
    {
        _ecsLoop.Scheduler.Schedule(static (_, self, data0) =>
        {
            var pawn = self._pawnState.GetPawnByNetworkId(data0.NetId);
            if (pawn == null)
            {
                self._logger.LogNullDebug(nameof(data0.NetId));
                return;
            }

            if (string.IsNullOrEmpty(data0.MontagePath))
            {
                pawn.StopAnimMontage(null);
                return;
            }

            var fullMontagePath = data0.Compressed ? Compressors.MontageNameCompressor.Decompress(data0.MontagePath) : data0.MontagePath;

            if (pawn.Mesh == null)
            {
                self._logger.LogError("pawn.Mesh is null");
                return;
            }

            var animInstance = pawn.Mesh.GetAnimInstance();
            if (animInstance == null)
            {
                self._logger.LogError("AnimInstance is null");
                return;
            }

            var currentMontage = animInstance.GetCurrentActiveMontage();

            // if the same montage is currently playing an no reset flag is given, do not play new montage
            if (currentMontage != null && currentMontage.PathName == fullMontagePath && !data0.Reset)
            {
                return;
            }

            var montage = BGW_PreloadAssetMgr.Get(GameUtils.GetWorld()).TryGetCachedResourceObj<UAnimMontage>(fullMontagePath, ELoadResourceType.SyncLoadAndCache);

            if (montage == null)
            {
                if (!fullMontagePath.Contains("Engine/Transient.AnimMontage"))
                {
                    self._logger.LogWarning("Montage not found: {Montage}", fullMontagePath);
                }

                return;
            }

            var events = BUS_EventCollectionCS.Get(pawn);

            if (events == null)
            {
                self._logger.LogError("events are null");
                return;
            }

            animInstance.Montage_Play(montage, 1f, EMontagePlayReturnType.MontageLength, data0.Position);
            events.Evt_PlayMontageCallback.Invoke(EMontageBindReason.Default, montage, EMontageCallbackState.OnStarted);
        }, this, data);
    }

    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    public void OnPreAnimationSyncing(PreAnimationSyncingData data)
    {
        _logger.LogDebug("OnPreAnimationSyncing called for Host {Host} and Guest {Guest}", data.Host, data.Guest);
        _ecsLoop.Scheduler.Schedule(static (_, self, data0) =>
        {
            var hostPawn = self._pawnState.GetPawnByNetworkId(data0.Host);
            if (hostPawn == null)
            {
                self._logger.LogNullDebug(nameof(data0.Host));
                return;
            }
            
            var guestPawn = self._pawnState.GetPawnByNetworkId(data0.Guest);
            if (guestPawn == null)
            {
                self._logger.LogNullDebug(nameof(data0.Guest));
                return;
            }

            var events = BUS_EventCollectionCS.Get(hostPawn);
            if (events == null)
            {
                self._logger.LogError("Failed to get event collection for unit {Unit}", hostPawn.GetName());
                return;
            }

            events.Evt_NotifyEnterPreAnimationSyncingStateOnHost?.Invoke(guestPawn, []);
        }, this, data);
    }

    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    public void OnAnimationSyncing(MontageCallbackData data)
    {
        _logger.LogDebug("OnAnimationSyncing called for NetId {NetId} with MontagePath '{MontagePath}'", data.NetId, data.MontagePath);
        _ecsLoop.Scheduler.Schedule(static (_, self, data0) =>
        {
            var pawn = self._pawnState.GetPawnByNetworkId(data0.NetId);
            if (pawn == null)
            {
                self._logger.LogNullDebug(nameof(data0.NetId));
                return;
            }
            
            var fullMontagePath = data0.Compressed ? Compressors.MontageNameCompressor.Decompress(data0.MontagePath) : data0.MontagePath;
            var montage = string.IsNullOrEmpty(fullMontagePath) ? null : BGW_PreloadAssetMgr.Get(GameUtils.GetWorld()).TryGetCachedResourceObj<UAnimMontage>(fullMontagePath, ELoadResourceType.SyncLoadAndCache);

            var events = BUS_EventCollectionCS.Get(pawn);
            if (events == null)
            {
                self._logger.LogError("Failed to get event collection for unit {Unit}", pawn.GetName());
                return;
            }

            events.Evt_NotifyEnterAnimationSyncingStateOnHost?.Invoke([], montage);
        }, this, data);
    }

    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    internal void OnUnitDead(UnitDeadPacket data)
    {
        _ecsLoop.Scheduler.Schedule(static (_, self, data0) =>
        {
            var pawn = self._pawnState.GetPawnByNetworkId(data0.NetworkId);
            if (pawn == null)
            {
                self._logger.LogNullDebug(nameof(data0.NetworkId));
                return;
            }

            var events = BUS_EventCollectionCS.Get(pawn);
            if (events == null)
            {
                self._logger.LogError("Failed to get event collection for unit {Unit}", pawn.GetName());
                return;
            }

            self._logger.LogDebug("OnUnitDead for unit {Unit}", pawn.GetName());
            events.Evt_UnitDead.Invoke(GameUtils.GetControlledPawn(), data0.DeadReason, data0.DmgId, data0.StiffLevel, null, default, data0.IsDotDmg, data0.AbnormalType);
        }, this, data);
    }

    [RpcEvent(RelayMode.GlobalOthers)]
    internal void OnWaitingForSequence(SequenceWaitingData data)
    {
        _ecsLoop.Scheduler.Schedule(static (_, self, data0) =>
        {
            CutsceneUtils.SetJoiningCutsceneStatus(data0);
            if (self._playerState.LocalMainCharacter is not { } mainEntity)
                return;

            ref var localMainComp = ref mainEntity.GetLocalState();
            if (localMainComp.Pawn == null)
                return;

            if (mainEntity.GetState().IsDead)
            {
                self._freeCameraManager.LeaveFreeCameraMode();
                PlayerUtils.RebirthPlayerInPlace(localMainComp.Pawn);
                CutsceneUtils.TeleportLocalPlayerToCutsceneLocation();
            }
        }, this, data);
    }

    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    internal void OnIronBodyStart(PlayerId __sender)
    {
        _ecsLoop.Scheduler.Schedule(static (_, self, sender) =>
        {
            if (self._playerState.GetMainCharacterById(sender) is not { } mainEntity)
            {
                self._logger.LogError("Player not found: {Id}", sender);
                return;
            }

            ref var localMainComp = ref mainEntity.GetLocalState();
            if (localMainComp.Pawn == null)
            {
                self._logger.LogError("Player pawn is null for player {Id}", sender);
                return;
            }

            IronBodyUtils.TriggerIronBody(localMainComp.Pawn);
        }, this, __sender);
    }

    // TODO: Find a better way to synchronize this event. If it's sent in entity owner mode, the server may not have the entity's data yet.
    [RpcEvent(RelayMode.AreaOfInterestAll)]
    private void OnUnitSpawned(PlayerId __sender, NetworkId netId)
    {
        _ecsLoop.Scheduler.Schedule(static (_, self, sender, netEntity0) =>
        {
            self._logger.LogDebug("OnUnitSpawned called for player {PlayerId} with entity netId: {NetId}", sender, netEntity0);
            if (!self._clientOwnership.OwnsEntity(netEntity0))
            {
                return;
            }

            if (self._playerState.GetMainCharacterById(sender) == null)
            {
                self._logger.LogError("Player not found: {Id}", sender);
                return;
            }

            if (self._netEntity.TryGetEntityByNetworkId(netEntity0, out var entity))
            {
                TamerUtils.AddSpawnedUnitRefCount(sender, new TamerEntity(entity.Value));
            }
        }, this, __sender, netId);
    }

    [RpcEvent(RelayMode.EntityOwner)]
    private void OnUnitDespawn(PlayerId __sender, NetworkId netId)
    {
        _ecsLoop.Scheduler.Schedule(static (_, self, sender, netEntity0) =>
        {
            self._logger.LogDebug("OnUnitDespawn called for player {PlayerId} with entity {Entity}", sender, netEntity0);
            if (self._playerState.GetMainCharacterById(sender) == null)
            {
                self._logger.LogError("Player not found: {Id}", sender);
                return;
            }

            if (self._netEntity.TryGetEntityByNetworkId(netEntity0, out var entity))
            {
                TamerUtils.SubtractSpawnedUnitRefCount(sender, new TamerEntity(entity.Value));
            }
        }, this, __sender, netId);
    }

    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    private void OnTamerSkillInteract(SkillInteractData interactData)
    {
        _ecsLoop.Scheduler.Schedule(static (_, self, interactData0) =>
        {
            if (self._netEntity.TryGetEntityByNetworkId(interactData0.InteractiveId, out var entity))
            {
                TamerUtils.TriggerSkillInteract(entity.Value, interactData0.SkillId);
            }
        }, this, interactData);
    }

    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    private void OnTriggerMagicallyChange(PlayerId __sender, MagicallyChangeData data)
    {
        _ecsLoop.Scheduler.Schedule(static (_, self, sender, data0) =>
        {
            if (self._playerState.GetMainCharacterById(sender) is not { } mainEntity)
            {
                self._logger.LogError("Player not found: {Id}", sender);
                return;
            }

            ref var mainComp = ref mainEntity.GetState();
            ref var localMainComp = ref mainEntity.GetLocalState();
            if (localMainComp.Pawn == null)
            {
                self._logger.LogError("Player pawn is null for player {Id}", sender);
                return;
            }

            var fullConfigPath = data0.Compressed ? Compressors.VigorNameCompressor.Decompress(data0.ConfigAssetName) : data0.ConfigAssetName;
            self._logger.LogDebug("Received trigger magically change for character {Nickname} with config {ConfigAssetPath}, skillID {SkillID}, recoverSkillID {RecoverSkillID}, curVigorSkillID {CurVigorSkillID}", mainComp.CharacterNickName, fullConfigPath, data0.SkillID, data0.RecoverSkillID, data0.CurVigorSkillID);
            MagicallyChangeUtils.TriggerMagicallyChange(localMainComp.Pawn, fullConfigPath, data0.SkillID, data0.RecoverSkillID, data0.CurVigorSkillID, data0.CastReason);
        }, this, __sender, data);
    }

    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    private void OnResetMagicallyChange(PlayerId __sender, EResetReason_MagicallyChange reason)
    {
        _ecsLoop.Scheduler.Schedule(static (_, self, sender, reason0) =>
        {
            if (self._playerState.GetMainCharacterById(sender) is not { } mainEntity)
            {
                self._logger.LogError("Player not found: {Id}", sender);
                return;
            }

            ref var mainComp = ref mainEntity.GetState();
            ref var localMainComp = ref mainEntity.GetLocalState();
            if (localMainComp.Pawn == null)
            {
                self._logger.LogError("Player pawn is null for player {Id}", sender);
                return;
            }

            self._logger.LogDebug("Received reset magically change for character {Nickname} with reason {Reason}", mainComp.CharacterNickName, reason0);
            MagicallyChangeUtils.ResetMagicallyChange(localMainComp.Pawn, reason0);
        }, this, __sender, reason);
    }

    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    void OnProjectileTarget(PlayerId __sender, ProjectileTargetData targetData)
    {
        _ecsLoop.Scheduler.Schedule(static (_, self, sender, targetData0) =>
        {
            if (self._playerState.GetMainCharacterById(sender) is not { } mainEntity)
            {
                self._logger.LogError("Player not found: {Id}", sender);
                return;
            }

            ref var localMainComp = ref mainEntity.GetLocalState();
            if (localMainComp.Pawn == null)
            {
                self._logger.LogError("Player pawn is null for player {Id}", sender);
                return;
            }

            var target = self._pawnState.GetPawnByNetworkId(targetData0.Target);
            if (target == null)
            {
                self._logger.LogNull(nameof(targetData0.Target));
                return;
            }

            ProjectileUtils.SetProjectileTarget(localMainComp.Pawn, targetData0.ProjectileName, target, targetData0.SocketName);
        }, this, __sender, targetData);
    }

    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    void OnSwitchOneProjectile(PlayerId __sender, ProjectileSwitchData switchData)
    {
        _ecsLoop.Scheduler.Schedule(static (_, self, sender, switchData0) =>
        {
            if (self._playerState.GetMainCharacterById(sender) is not { } mainEntity)
            {
                self._logger.LogError("Player not found: {Id}", sender);
                return;
            }

            ref var localMainComp = ref mainEntity.GetLocalState();
            if (localMainComp.Pawn == null)
            {
                self._logger.LogError("Player pawn is null for player {Id}", sender);
                return;
            }

            ProjectileUtils.SwitchProjectileInfo(localMainComp.Pawn, switchData0.ProjectileClassName, switchData0.BulletSwitchID, switchData0.SwitchIdx);
        }, this, __sender, switchData);
    }

    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    void OnProjectileDead(PlayerId __sender, ProjectileDeadData data)
    {
        _ecsLoop.Scheduler.Schedule(static (_, self, sender, data0) =>
        {
            if (self._playerState.GetMainCharacterById(sender) is not { } mainEntity)
            {
                self._logger.LogError("Player not found: {Id}", sender);
                return;
            }

            ref var localMainComp = ref mainEntity.GetLocalState();
            if (localMainComp.Pawn == null)
            {
                self._logger.LogError("Player pawn is null for player {Id}", sender);
                return;
            }

            ProjectileUtils.DestroyProjectile(localMainComp.Pawn, data0.ProjectileClassName, data0.Reason);
        }, this, __sender, data);
    }

    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    void OnMagicFieldDead(string magicFieldClassName, EBGUBulletDestroyReason reason)
    {
        _ecsLoop.Scheduler.Schedule(static (_, self, magicFieldClassName0, reason0) => { MagicFieldUtils.DestroyMagicField(magicFieldClassName0, reason0); }, this, magicFieldClassName, reason);
    }

    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    void OnProjectileMoveMode(PlayerId __sender, ProjectileMoveModeData data)
    {
        _ecsLoop.Scheduler.Schedule(static (_, self, sender, data0) =>
        {
            if (self._playerState.GetMainCharacterById(sender) is not { } mainEntity)
            {
                self._logger.LogError("Player not found: {Id}", sender);
                return;
            }

            ref var localMainComp = ref mainEntity.GetLocalState();
            if (localMainComp.Pawn == null)
            {
                self._logger.LogError("Player pawn is null for player {Id}", sender);
                return;
            }

            ProjectileUtils.SetProjectileMode(localMainComp.Pawn, data0.ProjectileClassName, data0.MoveMode);
        }, this, __sender, data);
    }

    [RpcEvent(RelayMode.AreaOfInterestAll)]
    private void OnPartyRespawn(int birthPointId)
    {
        _ecsLoop.Scheduler.Schedule(static (_, self, shrineId) =>
        {
            if (self._playerState.LocalMainCharacter is not { } mainEntity)
                return;

            ref var localMainComp = ref mainEntity.GetLocalState();
            if (localMainComp.Pawn == null)
                return;

            localMainComp.IsRespawning = true;
            self._freeCameraManager.LeaveFreeCameraMode();
            CutsceneUtils.ClearLocalJoiningCutsceneStatus(mainEntity);
            self._eventRouter.RaiseOnLocalPlayerBeforeRebirth();
            PlayerUtils.RebirthPlayer(localMainComp.Pawn, shrineId);
        }, this, birthPointId);
    }

    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    private void OnAfterRebirth(PlayerId __sender)
    {
        var playerEntity = _playerState.GetMainCharacterById(__sender);
        if (playerEntity is not { } mainEntity)
            return;

        var playerPawn = mainEntity.GetLocalState().Pawn;
        var events = BUS_EventCollectionCS.Get(playerPawn);
        if (events != null)
        {
            events.Evt_AfterUnitRebirth.Invoke(ERebirthType.RebirthPoint);
        }
    }

    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    private void OnRestAtShrine(int birthPointId)
    {
        _ecsLoop.Scheduler.Schedule(static (_, self, shrineId) =>
        {
            foreach (var player in DI.Instance.State.AreaPlayers)
            {
                var playerEntity = self._playerState.GetMainCharacterById(player);

                if (playerEntity is not { } mainEntity)
                    continue;

                ref var localMainComp = ref mainEntity.GetLocalState();
                if (localMainComp.Pawn == null)
                    continue;

                if (mainEntity.GetState().IsDead)
                {
                    localMainComp.IsRespawning = true;
                    self._freeCameraManager.LeaveFreeCameraMode();
                    PlayerUtils.RebirthPlayer(localMainComp.Pawn, shrineId);
                }
                else
                {
                    PlayerUtils.RestPlayer(localMainComp.Pawn);
                }
            }
        }, this, birthPointId);
    }

    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    private void OnStartJump(PlayerId __sender, StartJumpData jumpData)
    {
        _ecsLoop.Scheduler.Schedule(static (_, self, sender, jumpData0) =>
        {
            if (self._playerState.GetMainCharacterById(sender) is not { } mainEntity)
            {
                self._logger.LogError("Player not found: {Id}", sender);
                return;
            }

            ref var localMainComp = ref mainEntity.GetLocalState();
            if (localMainComp.Pawn == null)
            {
                self._logger.LogError("Player pawn is null for player {Id}", sender);
                return;
            }

            PlayerUtils.StartJump(localMainComp.Pawn, jumpData0.StartJumpDir, jumpData0.InputVector);
        }, this, __sender, jumpData);
    }

    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    private void OnStopJump(PlayerId __sender)
    {
        _ecsLoop.Scheduler.Schedule(static (_, self, sender) =>
        {
            if (self._playerState.GetMainCharacterById(sender) is not { } mainEntity)
            {
                self._logger.LogError("Player not found: {Id}", sender);
                return;
            }

            ref var localMainComp = ref mainEntity.GetLocalState();
            if (localMainComp.Pawn == null)
            {
                self._logger.LogError("Player pawn is null for player {Id}", sender);
                return;
            }

            PlayerUtils.StopJump(localMainComp.Pawn);
        }, this, __sender);
    }

    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    private void OnMonsterWakeUp(NetworkId netId)
    {
        _ecsLoop.Scheduler.Schedule(static (_, self, netId0) =>
        {
            var pawn = self._pawnState.GetPawnByNetworkId(netId0);
            if (pawn == null)
            {
                self._logger.LogNullDebug(nameof(netId0));
                return;
            }

            var guid = BGU_DataUtil.GetActorGuid(pawn);
            Logging.LogDebug("OnMonsterWakeup called for monster {Guid}", guid);

            TamerUtils.TriggerWakeUp(pawn);
        }, this, netId);
    }

    // ReSharper disable once InconsistentNaming
    private static readonly MethodInfo PlayDBC_ByType = AccessTools.Method(typeof(BGU_AbnormalStateHandlerBase), "PlayDBC_ByType");
    private static readonly MethodInfo EndAllDBC = AccessTools.Method(typeof(BGU_AbnormalStateHandlerBase), "EndAllDBC");

    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    private void OnPlayBaneEffect(PlayBaneEffectData data)
    {
        _ecsLoop.Scheduler.Schedule(static (_, self, data0) =>
        {
            var pawn = self._pawnState.GetPawnByNetworkId(data0.Id);
            if (pawn == null)
            {
                self._logger.LogNullDebug(nameof(data0.Id));
                return;
            }

            var handlers = BGU_DataUtil.GetUnPersistentReadOnlyData<BUC_AbnormalStateHandlers>(pawn);

            if (handlers == null)
                return;

            var handler = handlers.GetAbnormalHanddler(data0.StateType);
            PlayDBC_ByType.Invoke(handler, [data0.ActionType, default(FTransform), -1]);
        }, this, data);
    }

    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    private void OnStopBaneEffect(StopBaneEffectData data)
    {
        _ecsLoop.Scheduler.Schedule(static (_, self, data0) =>
        {
            var pawn = self._pawnState.GetPawnByNetworkId(data0.Id);
            if (pawn == null)
            {
                self._logger.LogNullDebug(nameof(data0.Id));
                return;
            }

            var handlers = BGU_DataUtil.GetUnPersistentReadOnlyData<BUC_AbnormalStateHandlers>(pawn);
            if (handlers == null)
                return;

            var handler = handlers.GetAbnormalHanddler(data0.StateType);
            EndAllDBC.Invoke(handler, []);
        }, this, data);
    }

    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    internal void OnCastSkill(NetworkId caster, int skillId, ECastSkillSourceType skillType)
    {
        _ecsLoop.Scheduler.Schedule(static (_, self, caster0, skillId0, skillType0) =>
        {
            if (self._pawnState.GetPawnByNetworkId(caster0) is not { } casterPawn)
            {
                self._logger.LogError("Caster pawn not found: {NetId}", caster0);
                return;
            }

            Logging.LogDebug("OnCastSkill called for caster {Caster} with skillId {SkillId} and skillType {SkillType}", BGU_DataUtil.GetActorGuid(casterPawn), skillId0, skillType0);
            BUS_EventCollectionCS.Get(casterPawn)?.Evt_UnitCastSkillTry.Invoke(new FCastSkillInfo(skillId0, skillType0));
        }, this, caster, skillId, skillType);
    }

    #region PvpRPC

    [Obsolete("To be removed once per-project RPC is implemented")]
    public event Action<int[]>? OnPvpEventReceived;

    [RpcEvent(RelayMode.AreaOfInterestAll)]
    internal void OnPvpEvent(int[] data)
    {
        OnPvpEventReceived?.Invoke(data);
    }

    #endregion
}