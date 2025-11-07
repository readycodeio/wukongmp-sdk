using System;
using b1;
using b1.BGW;
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
using WukongMp.Api.Chat;
using WukongMp.Api.DTO;
using WukongMp.Api.ECS.Entities;
using WukongMp.Api.NameCompressors;
using WukongMp.Api.Patches;
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

    public void SendTriggerMagicallyChange(PlayerId player, UBGWDataAsset config, int skillID, int recoverSkillID, ECastReason_MagicallyChange castReason)
    {
        var shortened = Compressors.VigorNameCompressor.Compress(config.PathName, out var shortMontagePath);
        var configName = shortened ? shortMontagePath : config.PathName;
        _logger.LogDebug("Sending magically change for player {PlayerId} with config {Config}, skillID {SkillID}, recoverSkillID {RecoverSkillId}, castReason {CastReason}", player, configName, skillID, recoverSkillID, castReason);
        var evData = new MagicallyChangeData(configName, shortened, skillID, recoverSkillID, castReason);
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

    [RpcEvent(RelayMode.AreaOfInterestAll)]
    internal void OnEndMatchmaking()
    {
        _ecsLoop.Scheduler.Schedule(_ => { PvPUtils.OnMatchmakingEnded(); });
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

    [RpcEvent(RelayMode.AreaOfInterestAll)]
    internal void OnChatMessage(ChatMessage message)
    {
        _ecsLoop.Scheduler.Schedule(static (_, message0) => { WukongChatter.OnGetMessage(message0); }, message);
    }

    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    internal void OnPhantomRush(PlayerId __sender, ESkillDirection direction)
    {
        _ecsLoop.Scheduler.Schedule(static (_, self, sender, direction0) =>
        {
            if (self._playerState.GetMainCharacterById(sender) is not { } mainEntity)
                return;
            ref var mainComp = ref mainEntity.GetState();
            ref var localMainComp = ref mainEntity.GetLocalState();
            if (localMainComp.Pawn == null)
            {
                self._logger.LogError("Player not found: {PlayerId}", mainComp.PlayerId);
                return;
            }

            self._logger.LogDebug("Received phantom rush for player {Nickname} in direction {Direction}", mainComp.CharacterNickName, direction0);
            var events = BUS_EventCollectionCS.Get(localMainComp.Pawn);
            events?.Evt_TriggerPhantomRush.Invoke(direction0);

            PlayerUtils.ResetCooldown(localMainComp.Pawn);
            PlayerUtils.ResetMana(localMainComp.Pawn);
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
                FreeCameraManager.Instance.LeaveFreeCameraMode();
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
                FreeCameraManager.Instance.LeaveFreeCameraMode();
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
            self._logger.LogDebug("Received trigger magically change for character {Nickname} with config {ConfigAssetPath}, skillID {SkillID}, recoverSkillID {RecoverSkillID}", mainComp.CharacterNickName, fullConfigPath, data0.SkillID, data0.RecoverSkillID);
            MagicallyChangeUtils.TriggerMagicallyChange(localMainComp.Pawn, fullConfigPath, data0.SkillID, data0.RecoverSkillID, data0.CastReason);
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
            PlayerUtils.RebirthPlayer(localMainComp.Pawn, shrineId);
            localMainComp.IsRespawning = false;
        }, this, birthPointId);
    }

    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    private void OnRestAtShrine(int birthPointId)
    {
        _ecsLoop.Scheduler.Schedule(static (_, self, shrineId) =>
        {
            if (self._playerState.LocalMainCharacter is not { } mainEntity)
                return;

            ref var localMainComp = ref mainEntity.GetLocalState();
            if (localMainComp.Pawn == null)
                return;

            if (mainEntity.GetState().IsDead)
            {
                localMainComp.IsRespawning = true;
                PlayerUtils.RebirthPlayer(localMainComp.Pawn, shrineId);
                localMainComp.IsRespawning = false;
            }
            else
            {
                PlayerUtils.RestPlayer(localMainComp.Pawn);
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
}