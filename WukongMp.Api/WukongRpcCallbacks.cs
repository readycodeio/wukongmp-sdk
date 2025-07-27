using System;
using b1;
using b1.BGW;
using BtlShare;
using ReadyM.Api.ECS.Idents;
using ReadyM.Api.Multiplayer;
using ReadyM.Api.Multiplayer.Client;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.Api.Multiplayer.Protocol.Enums;
using ReadyM.Relay.Client.State;
using ReadyM.Relay.Common;
using UnrealEngine.Engine;
using WukongMp.Api.DTO;
using WukongMp.Api.Old;
using WukongMp.Api.Old.Api;
using WukongMp.Api.Patches;
using WukongMp.Api.WukongUtils;
using WukongMp.Api.NameCompressors;

namespace WukongMp.Api;

public partial class WukongRpcCallbacks : IDisposable
{
    protected readonly RelaySerializer Serializer;
    protected readonly IRelayClient RelayClient;
    private readonly ClientState _state;
    private readonly WukongRoomState _roomState;
    private readonly EntityManagerWithLogs _entityManager;
    private readonly WukongPlayerRegistry _playerRegistry;
    private readonly WukongPawnRegistry _pawnRegistry;

    public WukongRpcCallbacks(
        RelaySerializer serializer,
        IRelayClient relayClient,
        ClientState state,
        WukongRoomState roomState,
        EntityManagerWithLogs entityManager,
        WukongPlayerRegistry playerRegistry,
        WukongPawnRegistry pawnRegistry)
    {
        Serializer = serializer;
        RelayClient = relayClient;
        _state = state;
        _roomState = roomState;
        _entityManager = entityManager;
        _playerRegistry = playerRegistry;
        _pawnRegistry = pawnRegistry;

        InitRpc();
    }

    public void Dispose()
    {
        DeInitRpc();
    }

    public void SendMontageCallback(NetworkIdComponent netId, UAnimMontage montage, float position, bool reset)
    {
        Logging.LogTrace("Sending montage callback: {Montage} {Position}", montage.PathName, position);
        var shortened = Compressors.MontageNameCompressor.Compress(montage.PathName, out var shortMontagePath);
        var data = shortened ? shortMontagePath : montage.PathName;
        var evData = new MontageCallbackData(netId, shortened, data, position, reset);
        SendMontageCallback(evData);
    }

    public void SendMontageCancel(NetworkIdComponent netId)
    {
        Logging.LogDebug("Sending montage cancel");
        var evData = new MontageCallbackData(netId, false, "", 0f, false);
        SendMontageCallback(evData);
    }

    public void SendTriggerMagicallyChange(PlayerId player, UBGWDataAsset config, int skillID, int recoverSkillID)
    {
        var shortened = Compressors.VigorNameCompressor.Compress(config.PathName, out var shortMontagePath);
        var configName = shortened ? shortMontagePath : config.PathName;
        Logging.LogTrace("Sending magically change for player {PlayerId} with config {Config} and skillID {SkillID}", player, configName, skillID);
        var evData = new MagicallyChangeData(configName, shortened, skillID, recoverSkillID);
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
        var playerState = _playerRegistry.GetPlayerById(playerId);
        if (playerState == null)
        {
            Logging.LogError("Player not found: {Id}", playerId);
            return;
        }

        Logging.LogDebug("Received exit phantom rush for player {Nickname}", playerState.NickName);
        var events = BUS_EventCollectionCS.Get(playerState.Pawn);
        playerState.ReceivedPhantomRushExit = true;
        events?.Evt_RelievePhantomRush.Invoke();
    }

    [RpcEvent(RelayMode.AreaOfInterestAll)]
    internal void OnEndMatchmaking()
    {
        PvPUtils.OnMatchmakingEnded();
    }

    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    internal void OnAddBuff(PlayerId __sender, BuffAddData data)
    {
        var playerState = _playerRegistry.GetPlayerById(__sender);
        BuffUtils.AddBuff(playerState?.Pawn, data.BuffId, data.Duration);
    }

    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    internal void OnRemoveBuff(PlayerId __sender, BuffRemoveData data)
    {
        var state = _playerRegistry.GetPlayerById(__sender);
        BuffUtils.RemoveBuff(state?.Pawn, data.BuffId, data.TriggerType, data.Layer, data.WithTriggerRemoveEffect);
    }

    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    internal void OnRemoveAllBuffs(PlayerId __sender, BuffRemoveAllData data)
    {
        var playerState = _playerRegistry.GetPlayerById(__sender);
        BuffUtils.RemoveAllBuffs(playerState?.Pawn, data.TriggerType, data.WithTriggerRemoveEffect);
    }

    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    internal void OnUnitStateTrigger(StateTriggerData data)
    {
        var character = _pawnRegistry.GetPawnByNetworkId(data.NetId);
        NpcLocomotionUtils.SetStateTrigger(character, data.Trigger, data.Time, data.NeedForceUpdate);
    }

    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    internal void OnUnitSimpleState(SimpleStateData data)
    {
        var character = _pawnRegistry.GetPawnByNetworkId(data.NetId);
        NpcLocomotionUtils.SetSimpleState(character, data.SimpleState, data.IsRemove);
    }

    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    internal void OnTriggerFsmState(FsmStateData data)
    {
        var character = _pawnRegistry.GetPawnByNetworkId(data.NetId);
        NpcLocomotionUtils.SetFsmState(character, data.FsmStateName);
    }

    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    internal void OnMotionMatchingState(MotionMatchingStateData data)
    {
        var character = _pawnRegistry.GetPawnByNetworkId(data.NetId);
        NpcLocomotionUtils.SetMotionMatchingState(character, data.State);
    }

    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    internal void OnSpawnSummon(UnitSummonData data)
    {
        GameLoopPatch.QueueOnGameThread(() => { SummonPatch.ExecuteSummon(data.SummonerId, data.SummonId, data.Guid, data.Name, data.TeamId); }, nameof(OnSpawnSummon));
    }

    [RpcEvent(RelayMode.EntityOwner)]
    internal void OnSpawnUnits(PlayerId __sender, UnitSpawnRequestData data)
    {
        SpawningUtils.SpawnUnitsMaster(__sender, data.UnitName, data.Count, data.TeamId);
    }

    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    internal void OnPlayerTransBegin(PlayerId __sender, PlayerTransBeginData data)
    {
        TransformationUtils.TransformPlayer(__sender, data.UnitResId, data.UnitBornSkillId, data.EnableBlendViewTarget, data.TransBeginType);
    }

    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    internal void OnPlayerTransEnd(PlayerId __sender, PlayerTransEndData data)
    {
        TransformationUtils.TransformPlayerBack(__sender, data.UnitResId, data.UnitBornSkillId, data.EnableBlendViewTarget, data.TransEndType);
    }

    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    internal void OnPlayMovieRequest(PlayMovieData data)
    {
        CutsceneUtils.PlayCutscene(data);
    }

    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    internal void OnSetTarget(TargetData data)
    {
        var pawn = _pawnRegistry.GetPawnByNetworkId(data.Character);
        if (pawn == null)
        {
            Logging.LogNull(nameof(data.Character));
            return;
        }

        if (data.ClearTarget)
        {
            TargetingApi.ClearTarget(pawn);
            return;
        }

        var target = _pawnRegistry.GetPawnByNetworkId(data.Target);

        if (target == null)
        {
            Logging.LogNull(nameof(data.Target));
            return;
        }

        TargetingApi.SetTarget(pawn, target);
    }

    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    internal void OnCastImmobilize(NetworkIdComponent caster)
    {
        if (_roomState.IsMasterClient)
        {
            var character = _pawnRegistry.GetPawnByNetworkId(caster);
            if (character == null)
            {
                Logging.LogNull(nameof(caster));
                return;
            }

            ImmobilizeUtils.CastImmobilize(character);
        }
    }

    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    internal void OnTriggerImmobilize(TriggerImmobilizeData data)
    {
        var caster = _pawnRegistry.GetPawnByNetworkId(data.PlayerId);
        var target = _pawnRegistry.GetPawnByNetworkId(data.Target);
        ImmobilizeUtils.TriggerImmobilize(caster, target, data.GreatSageTalentActiveBuff);
    }

    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    internal void OnRelieveImmobilize(NetworkIdComponent affected)
    {
        var character = _pawnRegistry.GetPawnByNetworkId(affected);
        if (character == null)
        {
            Logging.LogNull(nameof(affected));
            return;
        }

        ImmobilizeUtils.RelieveImmobilize(character);
    }

    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    internal void OnBreakImmobilize(NetworkIdComponent entity)
    {
        // TODO
        Logging.LogWarning("BreakImmobilize not implemented");
    }

    [RpcEvent(RelayMode.AreaOfInterestAll, EventCaching.AddToRoomCacheGlobal)]
    internal void OnChatMessage(ChatMessage message)
    {
        WukongChatter.OnGetMessage(message);
    }

    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    internal void OnPhantomRush(PlayerId __sender, ESkillDirection direction)
    {
        GameLoopPatch.QueueOnGameThread(() =>
        {
            var playerState = _playerRegistry.GetPlayerById(__sender);
            if (playerState?.Pawn == null)
            {
                Logging.LogError("Player not found: {PlayerId}", __sender);
                return;
            }

            Logging.LogDebug("Received phantom rush for player {Nickname} in direction {Direction}", playerState.NickName, direction);
            var events = BUS_EventCollectionCS.Get(playerState.Pawn);
            events?.Evt_TriggerPhantomRush.Invoke(direction);

            PlayerUtils.ResetCooldown(playerState.Pawn);
            PlayerUtils.ResetMana(playerState.Pawn);
        }, nameof(OnPhantomRush));
    }

    [RpcEvent(RelayMode.AreaOfInterestAll)]
    public void OnBroadcastPlayerTransform(PlayerTransformData data)
    {
        // TODO: Use targeted RPC mode (select which peers to send to)
        if (data.PlayerId != _playerRegistry.LocalPlayerState.PlayerId)
            return;

        PlayerUtils.TeleportLocalPlayer(data.Location, data.Rotation, false);
    }

    [RpcEvent(RelayMode.EntityOwner)]
    internal void OnSuicide(PlayerId __sender)
    {
        GameLoopPatch.QueueOnGameThread(() =>
        {
            var player = _playerRegistry.GetPlayerById(__sender)?.Pawn;
            if (player == null)
                return;

            var events = BUS_EventCollectionCS.Get(player);
            events?.Evt_IncreaseAttrFloat.Invoke(EBGUAttrFloat.Hp, -2000f);
            if (_roomState.IsMasterClient)
            {
                events?.Evt_UnitDead.Invoke(player, EDeadReason.Suicide);
            }
        }, nameof(OnSuicide));
    }

    [RpcEvent(RelayMode.AreaOfInterestAll)]
    internal void OnRebirthPlayer(PlayerId playerId)
    {
        GameLoopPatch.QueueOnGameThread(() =>
        {
            Logging.LogDebug("RebirthPlayer for player {PlayerId} called", playerId);

            var player = _playerRegistry.GetPlayerById(playerId);
            if (player == null)
                return;

            if (player.PlayerId == _state.LocalPlayerId)
            {
                FreeCameraManager.Instance.LeaveFreeCameraMode();
            }

            var events = BUS_EventCollectionCS.Get(player.Pawn);
            if (events != null)
            {
                events.Evt_OnLeaveFalling.Invoke(); // Reset falling timer.
                events.Evt_RebirthTeleportFinish.Invoke(ERebirthType.RebirthPoint); // Rest state and play anim montage.
                events.Evt_TriggerTeleportResetPlayer.Invoke(); // Reset player stats, will set IsDead flag to false.
            }
        }, nameof(OnRebirthPlayer));
    }

    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    internal void OnDamageNum(DamageNumParam damageNum)
    {
        GameLoopPatch.QueueOnGameThread(() =>
        {
            var uiEvt = BGW_UIEventCollection.Get(GameUtils.GetWorld());
            uiEvt.Evt_UI_ShowHPChangeNum(damageNum);
        }, nameof(OnDamageNum), BGW_TickGroupMask.TG_PreAnim);
    }

    [RpcEvent(RelayMode.AreaOfInterestAll)]
    internal void OnTeleportFinish(PlayerId __sender)
    {
        GameLoopPatch.QueueOnGameThread(() =>
        {
            var playerState = _playerRegistry.GetPlayerById(__sender);
            if (playerState == null)
            {
                Logging.LogError("Player not found: {PlayerId}", __sender);
                return;
            }

            var events = BUS_EventCollectionCS.Get(playerState.Pawn);
            events?.Evt_UnitStateTrigger.Invoke(EBUStateTrigger.TeleportEnd, -1f);
            events?.Evt_TeleportFinish.Invoke();
        }, nameof(OnTeleportFinish));
    }

    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    public void OnMontageCallback(MontageCallbackData data)
    {
        GameLoopPatch.QueueOnGameThread(() =>
        {
            var id = data.NetId;
            var pawn = _pawnRegistry.GetPawnByNetworkId(id);
            if (pawn == null)
            {
                Logging.LogNull(nameof(data.NetId));
                return;
            }

            if (string.IsNullOrEmpty(data.MontagePath))
            {
                Logging.LogTrace("Stopping montage playback for character {CharacterId}", id);
                pawn.StopAnimMontage(null);
                return;
            }

            var fullMontagePath = data.Compressed ? Compressors.MontageNameCompressor.Decompress(data.MontagePath) : data.MontagePath;
            Logging.LogTrace("Received montage: {Montage}, position: {Position}, reset: {Reset}", fullMontagePath, data.Position, data.Reset);

            var animInstance = pawn.Mesh.GetAnimInstance();
            if (animInstance == null)
            {
                Logging.LogError("AnimInstance is null");
                return;
            }

            var currentMontage = animInstance.GetCurrentActiveMontage();
            Logging.LogTrace("Current montage: {Montage}", currentMontage?.PathName);

            // if the same montage is currently playing an no reset flag is given, do not play new montage
            if (currentMontage != null && currentMontage.PathName == fullMontagePath && !data.Reset)
            {
                Logging.LogTrace("Skipping montage playback: {Montage}, is reset: {Reset}", fullMontagePath, data.Reset);
                return;
            }

            var montage = BGW_PreloadAssetMgr.Get(GameUtils.GetWorld()).TryGetCachedResourceObj<UAnimMontage>(fullMontagePath, ELoadResourceType.SyncLoadAndCache);

            if (montage == null)
            {
                Logging.LogWarning("Montage not found: {Montage}", fullMontagePath);
                return;
            }

            var events = BUS_EventCollectionCS.Get(pawn);

            if (events == null)
            {
                Logging.LogError("events are null");
                return;
            }

            Logging.LogTrace("Applying montage callback for character {CharacterId} with montage {Montage} @ {Position}", id, fullMontagePath, data.Position);
            animInstance.Montage_Play(montage, 1f, EMontagePlayReturnType.MontageLength, data.Position);
            events.Evt_PlayMontageCallback.Invoke(EMontageBindReason.Default, montage, EMontageCallbackState.OnStarted);
        }, nameof(OnMontageCallback));
    }

    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    internal void OnUnitDead(UnitDeadPacket data)
    {
        GameLoopPatch.QueueOnGameThread(() =>
        {
            var pawn = _pawnRegistry.GetPawnByNetworkId(data.NetworkId);
            if (pawn == null)
            {
                Logging.LogNull(nameof(data.NetworkId));
                return;
            }

            var events = BUS_EventCollectionCS.Get(pawn);
            if (events == null)
            {
                Logging.LogError("Failed to get event collection for unit {Unit}", pawn.GetName());
                return;
            }

            Logging.LogDebug("OnUnitDead for unit {Unit}", pawn.GetName());
            events.Evt_UnitDead.Invoke(GameUtils.GetControlledPawn(), data.DeadReason, data.DmgId, data.StiffLevel, null, default, data.IsDotDmg, data.AbnormalType);
        }, nameof(OnUnitDead));
    }

    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    internal void OnWaitingForSequence(PlayerId __sender, SequenceWaitingData data)
    {
        CutsceneUtils.SetWaitingForCutsceneStatus(__sender, data);
    }

    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    internal void OnIronBodyStart(PlayerId __sender)
    {
        var player = _playerRegistry.GetPlayerById(__sender);
        if (player == null)
        {
            Logging.LogError("Player not found: {Id}", __sender);
            return;
        }

        if (player.Pawn == null)
        {
            Logging.LogError("Player pawn is null for player {Id}", __sender);
            return;
        }

        IronBodyUtils.TriggerIronBody(player.Pawn);
    }

    [RpcEvent(RelayMode.EntityOwner)]
    void OnUnitSpawned(PlayerId __sender, NetworkIdComponent netEntity)
    {
        Logging.LogDebug("OnUnitSpawned called for player {PlayerId} with entity {Entity}", __sender, netEntity);
        var player = DI.Instance.Players.GetPlayerById(__sender);
        if (player == null)
        {
            Logging.LogError("Player not found: {Id}", __sender);
            return;
        }

        if (_entityManager.TryGetEntityByNetworkId(netEntity, out var entity))
        {
            TamerUtils.AddSpawnedUnit(player.PlayerId, entity.Value);
        }
    }

    [RpcEvent(RelayMode.EntityOwner)]
    void OnUnitDespawn(PlayerId __sender, NetworkIdComponent netEntity)
    {
        Logging.LogDebug("OnUnitDespawn called for player {PlayerId} with entity {Entity}", __sender, netEntity);
        var player = DI.Instance.Players.GetPlayerById(__sender);
        if (player == null)
        {
            Logging.LogError("Player not found: {Id}", __sender);
            return;
        }

        if (_entityManager.TryGetEntityByNetworkId(netEntity, out var entity))
        {
            TamerUtils.SubtractSpawnedUnit(player.PlayerId, entity.Value);
        }
    }

    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    void OnTamerSkillInteract(SkillInteractData interactData)
    {
        if (DI.Instance.NetManager.TryGetEntityByNetworkId(interactData.InteractiveId, out var entity))
        {
            if (entity.HasValue)
            {
                TamerUtils.TriggerSkillInteract(entity.Value, interactData.SkillId);
            }
        }
    }

    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    void OnTriggerMagicallyChange(PlayerId __sender, MagicallyChangeData data)
    {
        var player = DI.Instance.Players.GetPlayerById(__sender);
        if (player == null)
        {
            Logging.LogError("Player not found: {Id}", __sender);
            return;
        }
        if (player.Pawn == null)
        {
            Logging.LogError("Player pawn is null for player {Id}", __sender);
            return;
        }

        var fullConfigPath = data.Compressed ? Compressors.VigorNameCompressor.Decompress(data.ConfigAssetName) : data.ConfigAssetName;
        Logging.LogDebug("Received trigger magically change for character {Nickname} with config {ConfigAssetPath}, skillID {SkillID}, recoverSkillID {RecoverSkillID}", player.NickName, fullConfigPath, data.SkillID, data.RecoverSkillID);
        MagicallyChangeUtils.TriggerMagicallyChange(player.Pawn, fullConfigPath, data.SkillID, data.RecoverSkillID);
    }

    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    void OnResetMagicallyChange(PlayerId __sender, EResetReason_MagicallyChange reason)
    {
        var player = DI.Instance.Players.GetPlayerById(__sender);
        if (player == null)
        {
            Logging.LogError("Player not found: {Id}", __sender);
            return;
        }
        if (player.Pawn == null)
        {
            Logging.LogError("Player pawn is null for player {Id}", __sender);
            return;
        }

        Logging.LogDebug("Received reset magically change for character {Nickname} with reason {Reason}", player.NickName, reason);
        MagicallyChangeUtils.ResetMagicallyChange(player.Pawn, reason);
    }
}
