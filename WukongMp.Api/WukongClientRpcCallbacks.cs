using System;
using System.Diagnostics;
using b1;
using BtlShare;
using Friflo.Engine.ECS;
using Microsoft.Extensions.Logging;
using ReadyM.Api.Idents;
using ReadyM.Api.Mapping.Events;
using ReadyM.Api.Multiplayer.Client;
using ReadyM.Api.Multiplayer.ECS.Values;
using ReadyM.Api.Multiplayer.Generators;
using ReadyM.Api.Multiplayer.Protocol.Enums;
using ReadyM.Relay.Client;
using ReadyM.Relay.Client.State;
using ReadyM.Relay.Common.Mapping;
using ReadyM.Relay.Common.Serialization;
using UnrealEngine.Engine;
using WukongMp.Api.DTO;
using WukongMp.Api.ECS.Entities;
using WukongMp.Api.ECS.GameEvents;
using WukongMp.Api.Helpers;
using WukongMp.Api.Mapping;
using WukongMp.Api.Mapping.Events;
using WukongMp.Api.NameCompressors;
using WukongMp.Api.Resources;
using WukongMp.Api.State;
using WukongMp.Api.UI;

// ReSharper disable InconsistentNaming

namespace WukongMp.Api;

public partial class WukongClientRpcCallbacks : IDisposable
{
    protected readonly RelaySerializer Serializer;
    protected readonly IRelayClient RelayClient;
    private readonly IClientEcsUpdateLoop _ecsLoop;
    private readonly MappedEventManager _mappedEvent;
    private readonly WukongPlayerState _playerState;
    private readonly WukongAreaState _areaState;
    private readonly WukongMappingPolicyDirectory _policyDir;
    private readonly ClientNetworkedEntityManager _netEntity;
    private readonly WukongWidgetManager _widgetManager;
    private readonly TimerController _timerController;
    private readonly ILogger _logger;

    public WukongClientRpcCallbacks(
        IClientEcsUpdateLoop ecsLoop,
        WukongPlayerState playerState,
        WukongAreaState areaState,
        MappedEventManager mappedEvent,
        WukongMappingPolicyDirectory policyDir,
        RelaySerializer serializer,
        IRelayClient relayClient,
        ClientNetworkedEntityManager netEntity,
        WukongWidgetManager widgetManager,
        TimerController timerController,
        ILogger logger)
    {
        Serializer = serializer;
        RelayClient = relayClient;
        _mappedEvent = mappedEvent;
        _netEntity = netEntity;
        _playerState = playerState;
        _areaState = areaState;
        _policyDir = policyDir;
        _ecsLoop = ecsLoop;
        _widgetManager = widgetManager;
        _timerController = timerController;
        _logger = logger;

        InitRpc();

        _mappedEvent.RegisterEcsEventHandler<ExitPhantomRushEvent, WukongClientRpcCallbacks>(static (ev, self) => { self.SendExitPhantomRush(ev.Entity.GetNetId()); }, this);

        _mappedEvent.RegisterEcsEventHandler<AddBuffEvent, WukongClientRpcCallbacks>(static (ev, self) =>
        {
            self.SendAddBuff(new BuffAddData(
                netId: ev.Entity.GetNetId(),
                buffId: ev.BuffId,
                duration: ev.Duration
            ));
        }, this);

        _mappedEvent.RegisterEcsEventHandler<RemoveBuffEvent, WukongClientRpcCallbacks>(static (ev, self) =>
        {
            self.SendRemoveBuff(new BuffRemoveData(
                netId: ev.Entity.GetNetId(),
                buffId: ev.BuffId,
                triggerType: ev.TriggerType,
                layer: ev.Layer,
                withTriggerRemoveEffect: ev.WithTriggerRemoveEffect
            ));
        }, this);

        _mappedEvent.RegisterEcsEventHandler<RemoveAllBuffsEvent, WukongClientRpcCallbacks>(static (ev, self) =>
        {
            self.SendRemoveAllBuffs(new BuffRemoveAllData(
                netId: ev.Entity.GetNetId(),
                triggerType: ev.TriggerType,
                withTriggerRemoveEffect: ev.WithTriggerRemoveEffect
            ));
        }, this);

        _mappedEvent.RegisterEcsEventHandler<UnitStateTriggerEvent, WukongClientRpcCallbacks>(static (ev, self) =>
        {
            self.SendUnitStateTrigger(new StateTriggerData(
                netId: ev.Entity.GetNetId(),
                trigger: ev.Trigger,
                time: ev.Time,
                needForceUpdate: ev.NeedForceUpdate
            ));
        }, this);

        _mappedEvent.RegisterEcsEventHandler<UnitSimpleStateEvent, WukongClientRpcCallbacks>(static (ev, self) =>
        {
            self.SendUnitSimpleState(new SimpleStateData(
                netId: ev.Entity.GetNetId(),
                simpleState: ev.SimpleState,
                isRemove: ev.IsRemove
            ));
        }, this);

        _mappedEvent.RegisterEcsEventHandler<TriggerFsmStateEvent, WukongClientRpcCallbacks>(static (ev, self) =>
        {
            self.SendTriggerFsmState(new FsmStateData(
                netId: ev.Entity.GetNetId(),
                fsmStateName: ev.FsmStateName
            ));
        }, this);

        _mappedEvent.RegisterEcsEventHandler<MotionMatchingStateEvent, WukongClientRpcCallbacks>(static (ev, self) =>
        {
            self.SendMotionMatchingState(new MotionMatchingStateData(
                netId: ev.Entity.GetNetId(),
                state: ev.State
            ));
        }, this);

        _mappedEvent.RegisterEcsEventHandler<SpawnSummonEvent, WukongClientRpcCallbacks>(static (ev, self) =>
        {
            self.SendSpawnSummon(new SpawnSummonData(
                summonerNetId: ev.Summoner.GetNetId(),
                summonGuid: ev.SummonGuid,
                summonClassPath: ev.SummonClassPath,
                location: ev.Location,
                rotation: ev.Rotation,
                safeClampToLand: ev.SafeClampToLand,
                summonId: ev.SummonId,
                summonInstanceId: ev.SummonInstanceId,
                servantType: ev.ServantType,
                searchTargetType: ev.SearchTargetType,
                cooperativeSCGuid: ev.CooperativeSCGuid,
                aliveTime: ev.AliveTime,
                catchTargetNetId: ev.CatchTarget.GetNetId(),
                delayBornTime: ev.DelayBornTime,
                bornMontagePath: ev.BornMontagePath,
                bornSkill: ev.BornSkill,
                delayEffectTime: ev.DelayEffectTime,
                delaySummonTime: ev.DelaySummonTime,
                isSummonerAsMaster: ev.IsSummonerAsMaster,
                equipmentState: ev.EquipmentState,
                initSpeed: ev.InitSpeed,
                bornEffectPath: ev.BornEffectPath,
                disappearMontagePathList: ev.DisappearMontagePathList,
                destroyDelayTime: ev.DestroyDelayTime
            ));
        }, this);

        _mappedEvent.RegisterEcsEventHandler<RequestSpawnUnitsEvent, WukongClientRpcCallbacks>(static (ev, self) =>
        {
            self.SendRequestSpawnUnits(new RequestSpawnUnitsData(
                unitName: ev.UnitName,
                count: ev.Count,
                teamId: ev.TeamId,
                location: ev.Location
            ));
        }, this);

        _mappedEvent.RegisterEcsEventHandler<BroadcastUnitSpawnEvent, WukongClientRpcCallbacks>(static (ev, self) =>
        {
            self.SendBroadcastUnitSpawn(new BroadcastUnitSpawnData(
                netId: ev.Entity.GetNetId(),
                unitName: ev.UnitName,
                guid: ev.Guid,
                location: ev.Location
            ));
        }, this);

        _mappedEvent.RegisterEcsEventHandler<PlayerTransBeginEvent, WukongClientRpcCallbacks>(static (ev, self) =>
        {
            self.SendPlayerTransBegin(new PlayerTransBeginData(
                netId: ev.Entity.GetNetId(),
                unitResId: ev.UnitResId,
                unitBornSkillId: ev.UnitBornSkillId,
                enableBlendViewTarget: ev.EnableBlendViewTarget,
                transBeginType: ev.TransBeginType
            ));
        }, this);

        _mappedEvent.RegisterEcsEventHandler<PlayerTransEndEvent, WukongClientRpcCallbacks>(static (ev, self) =>
        {
            self.SendPlayerTransEnd(new PlayerTransEndData(
                netId: ev.Entity.GetNetId(),
                unitResId: ev.UnitResId,
                unitBornSkillId: ev.UnitBornSkillId,
                enableBlendViewTarget: ev.EnableBlendViewTarget,
                transEndType: ev.TransEndType
            ));
        }, this);

        _mappedEvent.RegisterEcsEventHandler<PlayMovieRequestEvent, WukongClientRpcCallbacks>(static (ev, self) =>
        {
            self.SendPlayMovieRequest(new PlayMovieRequestData(
                sequenceID: ev.SequenceId,
                disablePlayerControl: ev.DisablePlayerControl,
                disableMovementInput: ev.DisableMovementInput,
                disableLookAtInput: ev.DisableLookAtInput,
                hidePlayer: ev.HidePlayer,
                hideHud: ev.HideHud,
                overlapBoxGuid: ev.OverlapBoxGuid,
                matchType: ev.MatchType
            ));
        }, this);

        _mappedEvent.RegisterEcsEventHandler<SetTargetEvent, WukongClientRpcCallbacks>(static (ev, self) =>
        {
            if (ev.ClearTarget)
            {
                self.SendSetTarget(new SetTargetData(
                    characterNetId: ev.Character.GetNetId(),
                    targetNetId: default,
                    clearTarget: true
                ));
            }
            else
            {
                self.SendSetTarget(new SetTargetData(
                    characterNetId: ev.Character.GetNetId(),
                    targetNetId: ev.Target.GetNetId(),
                    clearTarget: false
                ));
            }
        }, this);

        _mappedEvent.RegisterEcsEventHandler<CastImmobilizeEvent, WukongClientRpcCallbacks>(static (ev, self) => { self.SendCastImmobilize(ev.Caster.GetNetId()); }, this);

        _mappedEvent.RegisterEcsEventHandler<TriggerImmobilizeEvent, WukongClientRpcCallbacks>(static (ev, self) =>
        {
            self.SendTriggerImmobilize(new TriggerImmobilizeData(
                netId: ev.Entity.GetNetId(),
                targetNetId: ev.Target.GetNetId(),
                greatSageTalentActiveBuff: ev.GreatSageTalentActiveBuff
            ));
        }, this);

        _mappedEvent.RegisterEcsEventHandler<RelieveImmobilizeEvent, WukongClientRpcCallbacks>(static (ev, self) => { self.SendRelieveImmobilize(ev.Affected.GetNetId()); }, this);

        _mappedEvent.RegisterEcsEventHandler<PhantomRushEvent, WukongClientRpcCallbacks>(static (ev, self) =>
        {
            self.SendPhantomRush(
                ev.Entity.GetNetId(),
                ev.Direction
            );
        }, this);

        _mappedEvent.RegisterEcsEventHandler<BroadcastPlayerTransformEvent, WukongClientRpcCallbacks>(static (ev, self) =>
        {
            self.SendBroadcastPlayerTransform(new BroadcastPlayerTransformData(
                netId: ev.Entity.GetNetId(),
                location: ev.Location,
                rotation: ev.Rotation
            ));
        }, this);

        _mappedEvent.RegisterEcsEventHandler<RebirthPlayerEvent, WukongClientRpcCallbacks>(static (ev, self) => { self.SendRebirthPlayer(ev.Entity.GetNetId(), ev.Teleport); }, this);

        _mappedEvent.RegisterEcsEventHandler<DamageNumEvent, WukongClientRpcCallbacks>(static (ev, self) =>
        {
            var damageNumParam = new DamageNumParam()
            {
                DamageType = ev.DamageType,
                DamageNum = ev.DamageNum,
                RealHitLocation = ev.RealHitLocation,
                RealHitDir = ev.RealHitDir,
                Amplitude = ev.Amplitude,
                AttackerTeamType = ev.AttackerTeamType,
            };
            self.SendDamageNum(damageNumParam, ev.Entity.GetNetId());
        }, this);

        _mappedEvent.RegisterEcsEventHandler<TeleportFinishEvent, WukongClientRpcCallbacks>(static (ev, self) => { self.SendTeleportFinish(ev.Entity.GetNetId()); }, this);

        _mappedEvent.RegisterEcsEventHandler<MontageCallbackEvent, WukongClientRpcCallbacks>(static (ev, self) =>
        {
            self.SendMontageCallback(
                ev.Entity.GetNetId(),
                ev.FullMontagePath,
                ev.Position,
                ev.Reset
            );
        }, this);

        _mappedEvent.RegisterEcsEventHandler<AnimationSyncingEvent, WukongClientRpcCallbacks>((ev, self) =>
        {
            var compressed = Compressors.MontageNameCompressor.Compress(ev.FullMontagePath, out var montage);

            self.SendAnimationSyncing(new AnimationSyncingData(
                hostNetId: ev.Host.GetNetId(),
                guestNetId: ev.Guest.GetNetId(),
                compressed: compressed,
                montage: montage
            ));
        }, this);

        _mappedEvent.RegisterEcsEventHandler<BeginSyncAnimationEvent, WukongClientRpcCallbacks>(static (ev, self) =>
        {
            var compressed = Compressors.MontageNameCompressor.Compress(ev.FullGuestMontage, out var guestMontage);

            self.SendBeginSyncAnimation(new BeginSyncAnimationData(
                hostNetId: ev.Host.GetNetId(),
                compressed: compressed,
                guestMontage: guestMontage,
                bFoundHostSyncPointOnDummyMesh: ev.FoundHostSyncPointOnDummyMesh,
                selfSyncPointOnHost: ev.SelfSyncPointOnHost,
                targetSyncPointOnHost: ev.TargetSyncPointOnHost,
                selfSyncPointOnGuest: ev.SelfSyncPointOnGuest,
                bForceSyncDummyMeshAnimation: ev.ForceSyncDummyMeshAnimation,
                bEnableDebugDraw: ev.EnableDebugDraw,
                notifyBeginTime: ev.NotifyBeginTime,
                totalDuration: ev.TotalDuration,
                animationSyncMontageInstanceId: ev.AnimationSyncMontageInstanceId
            ));
        }, this);

        _mappedEvent.RegisterEcsEventHandler<UnitDeadEvent, WukongClientRpcCallbacks>(static (ev, self) =>
        {
            self.SendUnitDead(new UnitDeadData(
                netId: ev.Entity.GetNetId(),
                deadReason: ev.DeadReason,
                dmgId: ev.DmgId,
                stiffLevel: ev.StiffLevel,
                isDotDmg: ev.IsDotDmg,
                abnormalType: ev.AbnormalType
            ));
        }, this);

        _mappedEvent.RegisterEcsEventHandler<WaitingForSequenceEvent, WukongClientRpcCallbacks>(static (ev, self) =>
        {
            self.SendWaitingForSequence(new SequenceWaitingData(
                sequenceID: ev.SequenceId,
                sequenceLocation: ev.SequenceLocation
            ));
        }, this);

        _mappedEvent.RegisterEcsEventHandler<IronBodyStartEvent, WukongClientRpcCallbacks>(static (ev, self) => { self.SendIronBodyStart(ev.Entity.GetNetId()); }, this);

        _mappedEvent.RegisterEcsEventHandler<UnitSpawnedEvent, WukongClientRpcCallbacks>(static (ev, self) =>
        {
            Debug.Assert(ev.PlayerId == self._playerState.LocalPlayerId);

            self.SendUnitSpawned(ev.Entity.GetNetId());
        }, this);

        _mappedEvent.RegisterEcsEventHandler<UnitDespawnedEvent, WukongClientRpcCallbacks>(static (ev, self) =>
        {
            Debug.Assert(ev.PlayerId == self._playerState.LocalPlayerId);

            self.SendUnitDespawned(ev.Entity.GetNetId());
        }, this);

        _mappedEvent.RegisterEcsEventHandler<TamerSkillInteractEvent, WukongClientRpcCallbacks>(static (ev, self) =>
        {
            self.SendTamerSkillInteract(new TamerSkillInteractData(
                netId: ev.Entity.GetNetId(),
                skillId: ev.SkillId
            ));
        }, this);

        _mappedEvent.RegisterEcsEventHandler<TriggerMagicallyChangeEvent, WukongClientRpcCallbacks>(static (ev, self) =>
        {
            self.SendTriggerMagicallyChange(
                ev.Entity.GetNetId(),
                ev.ConfigPathName,
                ev.SkillId,
                ev.RecoverSkillId,
                ev.CurVigorSkillId,
                ev.CastReason
            );
        }, this);

        _mappedEvent.RegisterEcsEventHandler<ResetMagicallyChangeEvent, WukongClientRpcCallbacks>(static (ev, self) =>
        {
            self.SendResetMagicallyChange(
                ev.Entity.GetNetId(),
                ev.Reason
            );
        }, this);

        _mappedEvent.RegisterEcsEventHandler<ProjectileTargetEvent, WukongClientRpcCallbacks>(static (ev, self) =>
        {
            self.SendProjectileTarget(new ProjectileTargetData(
                characterNetId: ev.Character.GetNetId(),
                projectileName: ev.ProjectileName,
                targetNetId: ev.Target.GetNetId(),
                socketName: ev.SocketName
            ));
        }, this);

        _mappedEvent.RegisterEcsEventHandler<ProjectileSwitchEvent, WukongClientRpcCallbacks>(static (ev, self) =>
        {
            self.SendProjectileSwitch(new ProjectileSwitchData(
                netId: ev.Entity.GetNetId(),
                projectileClassName: ev.ProjectileClassName,
                bulletSwitchID: ev.BulletSwitchId,
                switchIdx: ev.SwitchIdx
            ));
        }, this);

        _mappedEvent.RegisterEcsEventHandler<ProjectileDeadEvent, WukongClientRpcCallbacks>(static (ev, self) =>
        {
            self.SendProjectileDead(new ProjectileDeadData(
                netId: ev.Entity.GetNetId(),
                projectileClassName: ev.ProjectileClassName,
                reason: ev.Reason
            ));
        }, this);

        _mappedEvent.RegisterEcsEventHandler<MagicFieldDeadEvent, WukongClientRpcCallbacks>(static (ev, self) =>
        {
            self.SendMagicFieldDead(
                ev.ClassName,
                ev.Reason,
                ev.Entity.GetNetId()
            );
        }, this);

        _mappedEvent.RegisterEcsEventHandler<ProjectileMoveModeEvent, WukongClientRpcCallbacks>(static (ev, self) =>
        {
            self.OnProjectileMoveMode(new ProjectileMoveModeData(
                netId: ev.Entity.GetNetId(),
                projectileClassName: ev.ProjectileClassName,
                moveMode: ev.MoveMode
            ));
        }, this);

        _mappedEvent.RegisterEcsEventHandler<PartyRespawnEvent, WukongClientRpcCallbacks>(static (ev, self) =>
        {
            self.SendPartyRespawn(
                ev.BirthShrineId,
                ev.Entity.GetNetId()
            );
        }, this);

        _mappedEvent.RegisterEcsEventHandler<AfterRebirthEvent, WukongClientRpcCallbacks>(static (ev, self) => { self.SendAfterRebirth(ev.Entity.GetNetId()); }, this);

        _mappedEvent.RegisterEcsEventHandler<RestAtShrineEvent, WukongClientRpcCallbacks>(static (ev, self) =>
        {
            self.SendRestAtShrine(
                ev.RebirthPointId,
                ev.Entity.GetNetId()
            );
        }, this);

        _mappedEvent.RegisterEcsEventHandler<PartySoftlockEvent, WukongClientRpcCallbacks>(static (ev, self) =>
        {
            self.SendPartySoftlock(
                ev.BirthPointId,
                ev.Entity.GetNetId()
            );
        }, this);

        _mappedEvent.RegisterEcsEventHandler<StartJumpEvent, WukongClientRpcCallbacks>(static (ev, self) =>
        {
            self.SendStartJump(new StartJumpData(
                netId: ev.Entity.GetNetId(),
                startJumpDir: ev.StartJumpDir,
                inputVector: ev.InputVector
            ));
        }, this);

        _mappedEvent.RegisterEcsEventHandler<StopJumpEvent, WukongClientRpcCallbacks>(static (ev, self) => { self.SendStopJump(ev.Entity.GetNetId()); }, this);

        _mappedEvent.RegisterEcsEventHandler<MonsterWakeUpEvent, WukongClientRpcCallbacks>(static (ev, self) => { self.SendMonsterWakeUp(ev.Entity.GetNetId()); }, this);

        _mappedEvent.RegisterEcsEventHandler<PlayBaneEffectEvent, WukongClientRpcCallbacks>(static (ev, self) =>
        {
            self.SendPlayBaneEffect(new PlayBaneEffectData(
                netId: ev.Entity.GetNetId(),
                stateType: ev.StateType,
                actionType: ev.ActionType
            ));
        }, this);

        _mappedEvent.RegisterEcsEventHandler<StopBaneEffectEvent, WukongClientRpcCallbacks>(static (ev, self) =>
        {
            self.SendStopBaneEffect(new StopBaneEffectData(
                netId: ev.Entity.GetNetId(),
                stateType: ev.StateType
            ));
        }, this);

        _mappedEvent.RegisterEcsEventHandler<CastSkillEvent, WukongClientRpcCallbacks>(static (ev, self) =>
        {
            self.SendCastSkill(
                ev.Entity.GetNetId(),
                ev.SkillId,
                ev.SkillType
            );
        }, this);

        _mappedEvent.RegisterEcsEventHandler<ShowAntiStallWarningEvent, WukongClientRpcCallbacks>(static (ev, self) => { self.SendShowAntiStallWarning(ev.WarningTime); }, this);
        _mappedEvent.RegisterEcsEventHandler<ShowAntiStallActionEvent, WukongClientRpcCallbacks>(static (_, self) => { self.SendShowAntiStallAction(); }, this);
        _mappedEvent.RegisterEcsEventHandler<HideAntiStallEvent, WukongClientRpcCallbacks>(static (_, self) => { self.SendHideAntiStall(); }, this);
        _mappedEvent.RegisterEcsEventHandler<StallDamageEvent, WukongClientRpcCallbacks>(static (ev, self) => { self.SendStallDamage(ev.Target, ev.Damage); }, this);
    }

    public void Dispose()
    {
        DeInitRpc();
    }

    public void SendMontageCallback(NetworkId netId, UAnimMontage montage, float position, bool reset)
    {
        SendMontageCallback(netId, montage.PathName, position, reset);
    }

    public void SendMontageCallback(NetworkId netId, string fullPathName, float position, bool reset)
    {
        var shortened = Compressors.MontageNameCompressor.Compress(fullPathName, out var shortMontagePath);
        var data = shortened ? shortMontagePath : fullPathName;
        var evData = new MontageCallbackData(netId, shortened, data, position, reset);

        _logger.LogDebug("Sent montage for {NetId} at {Position} - {Montage}", netId, position, data);
        SendMontageCallback(evData);
    }

    public void SendMontageCancel(NetworkId netId)
    {
        var evData = new MontageCallbackData(netId, false, "", 0f, false);
        _logger.LogDebug("Sent montage cancel for entity {NetId}", netId);
        SendMontageCallback(evData);
    }

    public void SendTriggerMagicallyChange(NetworkId netId, UBGWDataAsset config, int skillID, int recoverSkillID, int curVigorSkillID, ECastReason_MagicallyChange castReason)
    {
        SendTriggerMagicallyChange(netId, config.PathName, skillID, recoverSkillID, curVigorSkillID, castReason);
    }

    public void SendTriggerMagicallyChange(NetworkId netId, string configPathName, int skillID, int recoverSkillID, int curVigorSkillID, ECastReason_MagicallyChange castReason)
    {
        var shortened = Compressors.VigorNameCompressor.Compress(configPathName, out var shortMontagePath);
        var configName = shortened ? shortMontagePath : configPathName;
        _logger.LogDebug("Sending magically change for main character {NetId} " +
                         "with config {Config}, skillID {SkillID}, " +
                         "recoverSkillID {RecoverSkillId}, " +
                         "curVigorSkillID {CurVigorSkillID}, " +
                         "castReason {CastReason}", netId, configName, skillID, recoverSkillID, curVigorSkillID, castReason);
        var evData = new MagicallyChangeData(netId, configName, shortened, skillID, recoverSkillID, curVigorSkillID, castReason);
        SendTriggerMagicallyChange(evData);
    }

    public void SendPlayMovieRequest(FPlayMovieRequest playMovieRequest)
    {
        SendPlayMovieRequest(new PlayMovieRequestData(
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
    internal void OnExitPhantomRush(NetworkId netId)
    {
        _ecsLoop.Scheduler.Schedule(static (_, self, netId0) =>
        {
            if (self._playerState.GetMainCharacterById(netId0) is not { } mainEntity)
                return;

            if (self._policyDir.MainCharacterEvent<ExitPhantomRushEvent>().ShouldEventPropagateToGame(mainEntity))
            {
                self._mappedEvent.PropagateToGame(new ExitPhantomRushEvent(mainEntity.Entity));
            }
        }, this, netId);
    }

    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    internal void OnAddBuff(BuffAddData data)
    {
        _ecsLoop.Scheduler.Schedule(static (_, self, data0) =>
        {
            if (self._playerState.GetMainCharacterById(data0.NetId) is not { } mainEntity)
                return;

            if (self._policyDir.MainCharacterEvent<AddBuffEvent>().ShouldEventPropagateToGame(mainEntity))
            {
                self._mappedEvent.PropagateToGame(new AddBuffEvent(
                    entity: mainEntity.Entity,
                    buffId: data0.BuffId,
                    duration: data0.Duration
                ));
            }
        }, this, data);
    }

    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    internal void OnRemoveBuff(BuffRemoveData data)
    {
        _ecsLoop.Scheduler.Schedule(static (_, self, data0) =>
        {
            if (!self._netEntity.TryGetEntityByNetworkId(data0.NetId, out var entity))
                return;

            if (self._policyDir.ForEvent<RemoveBuffEvent>().ShouldEventPropagateToGame(entity.Value))
            {
                self._mappedEvent.PropagateToGame(new RemoveBuffEvent(
                    entity: entity.Value,
                    buffId: data0.BuffId,
                    triggerType: data0.TriggerType,
                    layer: data0.Layer,
                    withTriggerRemoveEffect: data0.WithTriggerRemoveEffect
                ));
            }
        }, this, data);
    }

    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    internal void OnRemoveAllBuffs(BuffRemoveAllData data)
    {
        _ecsLoop.Scheduler.Schedule(static (_, self, data0) =>
        {
            if (!self._netEntity.TryGetEntityByNetworkId(data0.NetId, out var entity))
                return;

            if (self._policyDir.ForEvent<RemoveAllBuffsEvent>().ShouldEventPropagateToGame(entity.Value))
            {
                self._mappedEvent.PropagateToGame(new RemoveAllBuffsEvent(
                    entity: entity.Value,
                    triggerType: data0.TriggerType,
                    withTriggerRemoveEffect: data0.WithTriggerRemoveEffect
                ));
            }
        }, this, data);
    }

    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    internal void OnUnitStateTrigger(StateTriggerData data)
    {
        _ecsLoop.Scheduler.Schedule(static (_, self, data0) =>
        {
            if (!self._netEntity.TryGetEntityByNetworkId(data0.NetId, out var entity))
                return;

            if (self._policyDir.ForEvent<UnitStateTriggerEvent>().ShouldEventPropagateToGame(entity.Value))
            {
                self._mappedEvent.PropagateToGame(new UnitStateTriggerEvent(
                    entity: entity.Value,
                    trigger: data0.Trigger,
                    time: data0.Time,
                    needForceUpdate: data0.NeedForceUpdate
                ));
            }
        }, this, data);
    }

    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    internal void OnUnitSimpleState(SimpleStateData data)
    {
        _ecsLoop.Scheduler.Schedule(static (_, self, data0) =>
        {
            if (!self._netEntity.TryGetEntityByNetworkId(data0.NetId, out var entity))
                return;

            if (self._policyDir.ForEvent<UnitSimpleStateEvent>().ShouldEventPropagateToGame(entity.Value))
            {
                self._mappedEvent.PropagateToGame(new UnitSimpleStateEvent(
                    entity: entity.Value,
                    simpleState: data0.SimpleState,
                    isRemove: data0.IsRemove
                ));
            }
        }, this, data);
    }

    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    internal void OnTriggerFsmState(FsmStateData data)
    {
        _ecsLoop.Scheduler.Schedule(static (_, self, data0) =>
        {
            if (!self._netEntity.TryGetEntityByNetworkId(data0.NetId, out var entity))
                return;

            if (self._policyDir.ForEvent<TriggerFsmStateEvent>().ShouldEventPropagateToGame(entity.Value))
            {
                self._mappedEvent.PropagateToGame(new TriggerFsmStateEvent(
                    entity: entity.Value,
                    fsmStateName: data0.FsmStateName
                ));
            }
        }, this, data);
    }

    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    internal void OnMotionMatchingState(MotionMatchingStateData data)
    {
        _ecsLoop.Scheduler.Schedule(static (_, self, data0) =>
        {
            if (!self._netEntity.TryGetEntityByNetworkId(data0.NetId, out var entity))
                return;

            if (self._policyDir.ForEvent<MotionMatchingStateEvent>().ShouldEventPropagateToGame(entity.Value))
            {
                self._mappedEvent.PropagateToGame(new MotionMatchingStateEvent(
                    entity: entity.Value,
                    state: data0.State
                ));
            }
        }, this, data);
    }

    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    internal void OnSpawnSummon(SpawnSummonData data)
    {
        _ecsLoop.Scheduler.Schedule(static (_, self, data0) =>
        {
            self._netEntity.TryGetEntityByNetworkId(data0.SummonerNetId, out var summoner);
            self._netEntity.TryGetEntityByNetworkId(data0.CatchTargetNetId, out var catchTarget);
            
            if (!summoner.HasValue)
                return;

            var context = new SpawnSummonContext(summoner, data0.Location);

            if (self._policyDir.ForEvent<SpawnSummonEvent, SpawnSummonContext>().ShouldEventPropagateToGame(context))
            {
                self._mappedEvent.PropagateToGame(new SpawnSummonEvent(
                    summoner: summoner.Value,
                    summonGuid: data0.SummonGuid,
                    summonClassPath: data0.SummonClassPath,
                    location: data0.Location,
                    rotation: data0.Rotation,
                    safeClampToLand: data0.SafeClampToLand,
                    summonId: data0.SummonId,
                    summonInstanceId: data0.SummonInstanceId,
                    servantType: data0.ServantType,
                    searchTargetType: data0.SearchTargetType,
                    cooperativeSCGuid: data0.CooperativeSCGuid,
                    aliveTime: data0.AliveTime,
                    catchTarget: catchTarget ?? default,
                    delayBornTime: data0.DelayBornTime,
                    bornMontagePath: data0.BornMontagePath,
                    bornSkill: data0.BornSkill,
                    delayEffectTime: data0.DelayEffectTime,
                    delaySummonTime: data0.DelaySummonTime,
                    isSummonerAsMaster: data0.IsSummonerAsMaster,
                    equipmentState: data0.EquipmentState,
                    initSpeed: data0.InitSpeed,
                    bornEffectPath: data0.BornEffectPath,
                    disappearMontagePathList: data0.DisappearMontagePathList,
                    destroyDelayTime: data0.DestroyDelayTime
                ));
            }
        }, this, data);
    }

    // NOTE(api): Changed from AreaOfInterestAll
    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    internal void OnRequestSpawnUnits(RequestSpawnUnitsData data)
    {
        _ecsLoop.Scheduler.Schedule(static (_, self, data0) =>
        {
            if (self._policyDir.ForEvent<RequestSpawnUnitsEvent, EmptyContext>().ShouldEventPropagateToGame(default))
            {
                self._mappedEvent.PropagateToGame(new RequestSpawnUnitsEvent(
                    unitName: data0.UnitName,
                    count: data0.Count,
                    teamId: data0.TeamId,
                    location: data0.Location
                ));
            }
        }, this, data);
    }

    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    internal void OnBroadcastUnitSpawn(BroadcastUnitSpawnData data)
    {
        _ecsLoop.Scheduler.Schedule(static (_, self, data0) =>
        {
            if (!self._netEntity.TryGetEntityByNetworkId(data0.NetId, out var entity))
            {
                self._logger.LogError("Entity not found: {NetId}", data0.NetId);
                return;
            }

            if (self._policyDir.ForEvent<BroadcastUnitSpawnEvent>().ShouldEventPropagateToGame(entity.Value))
            {
                self._mappedEvent.PropagateToGame(new BroadcastUnitSpawnEvent(
                    entity: entity.Value,
                    unitName: data0.UnitName,
                    guid: data0.Guid,
                    location: data0.Location
                ));
            }
        }, this, data);
    }

    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    internal void OnPlayerTransBegin(PlayerTransBeginData data)
    {
        _ecsLoop.Scheduler.Schedule(static (_, self, data0) =>
        {
            if (self._playerState.GetMainCharacterById(data0.NetId) is not { } mainEntity)
                return;

            if (self._policyDir.MainCharacterEvent<PlayerTransBeginEvent>().ShouldEventPropagateToGame(mainEntity))
            {
                self._mappedEvent.PropagateToGame(new PlayerTransBeginEvent(
                    entity: mainEntity.Entity,
                    unitResId: data0.UnitResId,
                    unitBornSkillId: data0.UnitBornSkillId,
                    enableBlendViewTarget: data0.EnableBlendViewTarget,
                    transBeginType: data0.TransBeginType
                ));
            }
        }, this, data);
    }

    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    internal void OnPlayerTransEnd(PlayerTransEndData data)
    {
        _ecsLoop.Scheduler.Schedule(static (_, self, data0) =>
        {
            if (self._playerState.GetMainCharacterById(data0.NetId) is not { } mainEntity)
                return;

            if (self._policyDir.MainCharacterEvent<PlayerTransEndEvent>().ShouldEventPropagateToGame(mainEntity))
            {
                self._mappedEvent.PropagateToGame(new PlayerTransEndEvent(
                    entity: mainEntity.Entity,
                    unitResId: data0.UnitResId,
                    unitBornSkillId: data0.UnitBornSkillId,
                    enableBlendViewTarget: data0.EnableBlendViewTarget,
                    transEndType: data0.TransEndType
                ));
            }
        }, this, data);
    }

    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    internal void OnPlayMovieRequest(PlayerId __sender, PlayMovieRequestData requestData)
    {
        _ecsLoop.Scheduler.Schedule(static (_, self, data0, sender) =>
        {
            if (self._policyDir.ForEvent<PlayMovieRequestEvent, EmptyContext>().ShouldEventPropagateToGame(default))
            {
                self._mappedEvent.PropagateToGame(new PlayMovieRequestEvent(
                    sequenceId: data0.SequenceId,
                    disablePlayerControl: data0.DisablePlayerControl,
                    disableMovementInput: data0.DisableMovementInput,
                    disableLookAtInput: data0.DisableLookAtInput,
                    hidePlayer: data0.HidePlayer,
                    hideHud: data0.HideHud,
                    overlapBoxGuid: data0.OverlapBoxGuid,
                    matchType: data0.MatchType
                ));
            }
        }, this, requestData, __sender);
    }

    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    internal void OnSetTarget(SetTargetData data)
    {
        _ecsLoop.Scheduler.Schedule(static (_, self, data0) =>
        {
            if (!self._netEntity.TryGetEntityByNetworkId(data0.CharacterNetId, out var character))
            {
                self._logger.LogNull(nameof(data0.CharacterNetId));
                return;
            }

            if (data0.ClearTarget)
            {
                self._mappedEvent.PropagateToGame(new SetTargetEvent(
                    character: character.Value,
                    target: default,
                    clearTarget: true
                ));
                return;
            }

            if (!self._netEntity.TryGetEntityByNetworkId(data0.TargetNetId, out var target))
            {
                self._logger.LogNull(nameof(data0.TargetNetId));
                return;
            }

            if (self._policyDir.ForEvent<SetTargetEvent>().ShouldEventPropagateToGame(character.Value))
            {
                self._mappedEvent.PropagateToGame(new SetTargetEvent(
                    character: character.Value,
                    target: target.Value,
                    clearTarget: false
                ));
            }
        }, this, data);
    }

    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    internal void OnCastImmobilize(NetworkId caster)
    {
        _ecsLoop.Scheduler.Schedule(static (_, self, caster0) =>
        {
            // NOTE(api): This extra check emulates sending event to master only
            if (!self._areaState.IsMasterClient)
                return;

            if (!self._netEntity.TryGetEntityByNetworkId(caster0, out var casterEntity))
            {
                self._logger.LogNull(nameof(caster));
                return;
            }

            if (self._policyDir.ForEvent<CastImmobilizeEvent>().ShouldEventPropagateToGame(casterEntity.Value))
            {
                self._mappedEvent.PropagateToGame(new CastImmobilizeEvent(casterEntity.Value));
            }
        }, this, caster);
    }

    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    internal void OnTriggerImmobilize(TriggerImmobilizeData data)
    {
        _ecsLoop.Scheduler.Schedule(static (_, self, data0) =>
        {
            if (!self._netEntity.TryGetEntityByNetworkId(data0.NetId, out var caster))
            {
                Logging.LogError("Failed to cast immobilizedCharacter to BGUCharacterCS");
                return;
            }

            if (!self._netEntity.TryGetEntityByNetworkId(data0.TargetNetId, out var target))
            {
                Logging.LogError("Failed to cast castingCharacter to BGUCharacterCS");
                return;
            }

            if (self._policyDir.ForEvent<TriggerImmobilizeEvent, EmptyContext>().ShouldEventPropagateToGame(default))
            {
                self._mappedEvent.PropagateToGame(new TriggerImmobilizeEvent(
                    entity: caster.Value,
                    target: target.Value,
                    greatSageTalentActiveBuff: data0.GreatSageTalentActiveBuff
                ));
            }
        }, this, data);
    }

    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    internal void OnRelieveImmobilize(NetworkId affected)
    {
        _ecsLoop.Scheduler.Schedule(static (_, self, affected0) =>
        {
            if (!self._netEntity.TryGetEntityByNetworkId(affected0, out var affectedEntity))
            {
                self._logger.LogNullDebug(nameof(affected));
                return;
            }

            if (self._policyDir.ForEvent<TriggerImmobilizeEvent, EmptyContext>().ShouldEventPropagateToGame(default))
            {
                self._mappedEvent.PropagateToGame(new RelieveImmobilizeEvent(affectedEntity.Value));
            }
        }, this, affected);
    }

    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    internal void OnPhantomRush(NetworkId netId, ESkillDirection direction)
    {
        _ecsLoop.Scheduler.Schedule(static (_, self, netId0, direction0) =>
        {
            if (self._playerState.GetMainCharacterById(netId0) is not { } mainEntity)
                return;

            if (self._policyDir.MainCharacterEvent<PhantomRushEvent>().ShouldEventPropagateToGame(mainEntity))
            {
                self._mappedEvent.PropagateToGame(new PhantomRushEvent(mainEntity.Entity, direction0));
            }
        }, this, netId, direction);
    }

    // NOTE(api): Changed from AreaOfInterestAll
    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    public void OnBroadcastPlayerTransform(BroadcastPlayerTransformData data)
    {
        _ecsLoop.Scheduler.Schedule(static (_, self, data0) =>
        {
            if (self._playerState.GetMainCharacterById(data0.NetId) is not { } mainEntity)
                return;

            if (self._policyDir.ForEvent<BroadcastPlayerTransformEvent, EmptyContext>().ShouldEventPropagateToGame(default))
            {
                self._mappedEvent.PropagateToGame(new BroadcastPlayerTransformEvent(
                    entity: mainEntity.Entity,
                    location: data0.Location,
                    rotation: data0.Rotation
                ));
            }
        }, this, data);
    }

    // NOTE(api): Changed from AreaOfInterestAll
    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    internal void OnRebirthPlayer(NetworkId netId, bool isTeleport)
    {
        _ecsLoop.Scheduler.Schedule(static (_, self, netId0, isTeleport0) =>
        {
            self._logger.LogDebug("RebirthPlayer for main character {NetId} called", netId0);

            if (self._playerState.GetMainCharacterById(netId0) is not { } mainEntity)
                return;

            if (self._policyDir.ForEvent<RebirthPlayerEvent, EmptyContext>().ShouldEventPropagateToGame(default))
            {
                self._mappedEvent.PropagateToGame(new RebirthPlayerEvent(
                    entity: mainEntity.Entity,
                    teleport: isTeleport0
                ));
            }
        }, this, netId, isTeleport);
    }

    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    internal void OnDamageNum(DamageNumParam damageNum, NetworkId netId)
    {
        _ecsLoop.Scheduler.Schedule(static (_, self, damageNum0, netId0) =>
        {
            if (!self._netEntity.TryGetEntityByNetworkId(netId0, out var entity))
            {
                self._logger.LogError("Character not found: {NetId}", netId0);
                return;
            }

            if (self._policyDir.ForEvent<DamageNumEvent, Entity>().ShouldEventPropagateToGame(entity.Value))
            {
                self._mappedEvent.PropagateToGame(new DamageNumEvent(
                    entity: entity.Value,
                    damageType: damageNum0.DamageType,
                    damageNum: damageNum0.DamageNum,
                    amplitude: damageNum0.Amplitude,
                    realHitLocation: damageNum0.RealHitLocation,
                    realHitDir: damageNum0.RealHitDir,
                    attackerTeamType: damageNum0.AttackerTeamType
                ));
            }
        }, this, damageNum, netId);
    }

    // NOTE(api): Changed from AreaOfInterestAll
    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    internal void OnTeleportFinish(NetworkId netId)
    {
        _ecsLoop.Scheduler.Schedule(static (_, self, netId0) =>
        {
            if (self._playerState.GetMainCharacterById(netId0) is not { } mainEntity)
            {
                self._logger.LogError("Main character not found: {NetId}", netId0);
                return;
            }

            if (self._policyDir.ForEvent<TeleportFinishEvent, EmptyContext>().ShouldEventPropagateToGame(default))
            {
                self._mappedEvent.PropagateToGame(new TeleportFinishEvent(mainEntity.Entity));
            }
        }, this, netId);
    }

    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    public void OnMontageCallback(MontageCallbackData data)
    {
        _ecsLoop.Scheduler.Schedule(static (_, self, data0) =>
        {
            if (!self._netEntity.TryGetEntityByNetworkId(data0.NetId, out var entity))
            {
                self._logger.LogNullDebug(nameof(data0.NetId));
                return;
            }

            var fullMontagePath = string.IsNullOrEmpty(data0.MontagePath) ? ""
                : data0.Compressed ? Compressors.MontageNameCompressor.Decompress(data0.MontagePath) : data0.MontagePath;

            if (self._policyDir.ForEvent<MontageCallbackEvent>().ShouldEventPropagateToGame(entity.Value))
            {
                self._mappedEvent.PropagateToGame(new MontageCallbackEvent(
                    entity: entity.Value,
                    fullMontagePath: fullMontagePath,
                    position: data0.Position,
                    reset: data0.Reset
                ));
            }
        }, this, data);
    }

    // FIXME(api): Seems like this is unused
    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    public void OnAnimationSyncing(AnimationSyncingData data)
    {
        _ecsLoop.Scheduler.Schedule(static (_, self, data0) =>
        {
            /*
            self._logger.LogDebug("OnPreAnimationSyncing called for Host {Host} and Guest {Guest}", data0.HostNetId, data0.GuestNetId);

            if (!self._netEntity.TryGetEntityByNetworkId(data0.HostNetId, out var hostEntity))
            {
                self._logger.LogNullDebug(nameof(data0.HostNetId));
                return;
            }

            if (!self._netEntity.TryGetEntityByNetworkId(data0.GuestNetId, out var guestEntity))
            {
                self._logger.LogNullDebug(nameof(data0.GuestNetId));
                return;
            }

            if (self._policyDir.ForEvent<AnimationSyncingEvent>().ShouldEventPropagateToGame(hostEntity.Value))
            {
                var fullMontagePath = data0.Compressed ? Compressors.MontageNameCompressor.Decompress(data0.Montage) : data0.Montage;

                self._mappedEvent.PropagateToGame(new AnimationSyncingEvent(
                    host: hostEntity.Value,
                    guest: guestEntity.Value,
                    fullMontagePath: fullMontagePath
                ));
            }
            */
        }, this, data);
    }

    // FIXME(api): Seems like this is unused
    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    internal void OnBeginSyncAnimation(BeginSyncAnimationData data)
    {
        _ecsLoop.Scheduler.Schedule(static (_, self, data0) =>
        {
            /*
            self._logger.LogDebug("OnBeginSyncAnimation called for Host {NetId} with GuestMontage '{MontagePath}'", data0.HostNetId, data0.GuestMontage);

            if (!self._netEntity.TryGetEntityByNetworkId(data0.HostNetId, out var hostEntity))
            {
                self._logger.LogNullDebug(nameof(data0.HostNetId));
                return;
            }

            var fullMontagePath = data0.compressed ? Compressors.MontageNameCompressor.Decompress(data0.GuestMontage) : data0.GuestMontage;

            if (self._policyDir.ForEvent<BeginSyncAnimationEvent>().ShouldEventPropagateToGame(hostEntity.Value))
            {
                self._mappedEvent.PropagateToGame(new BeginSyncAnimationEvent(
                    host: hostEntity.Value,
                    fullGuestMontage: fullMontagePath,
                    foundHostSyncPointOnDummyMesh: data0.bFoundHostSyncPointOnDummyMesh,
                    selfSyncPointOnHost: data0.SelfSyncPointOnHost,
                    targetSyncPointOnHost: data0.TargetSyncPointOnHost,
                    selfSyncPointOnGuest: data0.SelfSyncPointOnGuest,
                    forceSyncDummyMeshAnimation: data0.bForceSyncDummyMeshAnimation,
                    enableDebugDraw: data0.bEnableDebugDraw,
                    notifyBeginTime: data0.NotifyBeginTime,
                    totalDuration: data0.TotalDuration,
                    animationSyncMontageInstanceId: data0.AnimationSyncMontageInstanceId
                ));
            }
            */
        }, this, data);
    }

    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    internal void OnUnitDead(UnitDeadData data)
    {
        _ecsLoop.Scheduler.Schedule(static (_, self, data0) =>
        {
            if (!self._netEntity.TryGetEntityByNetworkId(data0.NetId, out var entity))
            {
                self._logger.LogNullDebug(nameof(data0.NetId));
                return;
            }

            if (self._policyDir.ForEvent<UnitDeadEvent>().ShouldEventPropagateToGame(entity.Value))
            {
                self._mappedEvent.PropagateToGame(new UnitDeadEvent(
                    entity: entity.Value,
                    deadReason: data0.DeadReason,
                    dmgId: data0.DmgId,
                    stiffLevel: data0.StiffLevel,
                    isDotDmg: data0.IsDotDmg,
                    abnormalType: data0.AbnormalType
                ));
            }
        }, this, data);
    }

    [RpcEvent(RelayMode.GlobalOthers)]
    internal void OnWaitingForSequence(SequenceWaitingData data)
    {
        _ecsLoop.Scheduler.Schedule(static (_, self, data0) =>
        {
            if (self._policyDir.ForEvent<WaitingForSequenceEvent, EmptyContext>().ShouldEventPropagateToGame(default))
            {
                self._mappedEvent.PropagateToGame(new WaitingForSequenceEvent(
                    sequenceId: data0.SequenceID,
                    sequenceLocation: data0.SequenceLocation
                ));
            }
        }, this, data);
    }

    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    internal void OnIronBodyStart(NetworkId netId)
    {
        _ecsLoop.Scheduler.Schedule(static (_, self, netId0) =>
        {
            if (self._playerState.GetMainCharacterById(netId0) is not { } mainEntity)
            {
                self._logger.LogError("Main character not found: {NetId}", netId0);
                return;
            }

            if (self._policyDir.MainCharacterEvent<IronBodyStartEvent>().ShouldEventPropagateToGame(mainEntity))
            {
                self._mappedEvent.PropagateToGame(new IronBodyStartEvent(mainEntity.Entity));
            }
        }, this, netId);
    }

    // TODO: Find a better way to synchronize this event. If it's sent in entity owner mode, the server may not have the entity's data yet.
    // NOTE(api): Changed from AreaOfInterestAll
    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    private void OnUnitSpawned(PlayerId __sender, NetworkId netId)
    {
        _ecsLoop.Scheduler.Schedule(static (_, self, sender, netId0) =>
        {
            self._logger.LogDebug("OnUnitSpawned called for player {PlayerId} with entity: {NetId}", sender, netId0);

            if (!self._netEntity.TryGetEntityByNetworkId(netId0, out var entity))
                return;

            if (self._policyDir.ForEvent<UnitSpawnedEvent, EmptyContext>().ShouldEventPropagateToGame(default))
            {
                self._mappedEvent.PropagateToGame(new UnitSpawnedEvent(
                    entity: entity.Value,
                    playerId: sender
                ));
            }
        }, this, __sender, netId);
    }

    [RpcEvent(RelayMode.EntityOwner)]
    private void OnUnitDespawned(PlayerId __sender, NetworkId netId)
    {
        _ecsLoop.Scheduler.Schedule(static (_, self, sender, netId0) =>
        {
            // NOTE: There's an extra check so that we don't send this to ourselves
            if (sender == self._playerState.LocalPlayerId)
                return;

            self._logger.LogDebug("OnUnitDespawn called for player {PlayerId} with entity {NetId}", sender, netId0);

            if (!self._netEntity.TryGetEntityByNetworkId(netId0, out var entity))
                return;

            if (self._policyDir.ForEvent<UnitDespawnedEvent, EmptyContext>().ShouldEventPropagateToGame(default))
            {
                self._mappedEvent.PropagateToGame(new UnitDespawnedEvent(
                    entity: entity.Value,
                    playerId: sender
                ));
            }
        }, this, __sender, netId);
    }

    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    private void OnTamerSkillInteract(TamerSkillInteractData interactData)
    {
        _ecsLoop.Scheduler.Schedule(static (_, self, interactData0) =>
        {
            if (!self._netEntity.TryGetEntityByNetworkId(interactData0.NetId, out var entity))
                return;

            if (self._policyDir.ForEvent<TamerSkillInteractEvent>().ShouldEventPropagateToGame(entity.Value))
            {
                self._mappedEvent.PropagateToGame(new TamerSkillInteractEvent(
                    entity: entity.Value,
                    skillId: interactData0.SkillId
                ));
            }
        }, this, interactData);
    }

    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    private void OnTriggerMagicallyChange(MagicallyChangeData data)
    {
        _ecsLoop.Scheduler.Schedule(static (_, self, data0) =>
        {
            if (self._playerState.GetMainCharacterById(data0.NetId) is not { } mainEntity)
            {
                self._logger.LogError("Main character not found: {NetId}", data0.NetId);
                return;
            }

            if (self._policyDir.MainCharacterEvent<TriggerMagicallyChangeEvent>().ShouldEventPropagateToGame(mainEntity))
            {
                var fullPath = data0.Compressed ? Compressors.VigorNameCompressor.Decompress(data0.ConfigAssetName) : data0.ConfigAssetName;
                self._mappedEvent.PropagateToGame(new TriggerMagicallyChangeEvent(
                    entity: mainEntity.Entity,
                    configPathName: fullPath,
                    skillId: data0.SkillID,
                    recoverSkillId: data0.RecoverSkillID,
                    curVigorSkillId: data0.CurVigorSkillID,
                    castReason: data0.CastReason
                ));
            }
        }, this, data);
    }

    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    private void OnResetMagicallyChange(NetworkId netId, EResetReason_MagicallyChange reason)
    {
        _ecsLoop.Scheduler.Schedule(static (_, self, netId0, reason0) =>
        {
            if (self._playerState.GetMainCharacterById(netId0) is not { } mainEntity)
            {
                self._logger.LogError("Main character not found: {NetId}", netId0);
                return;
            }

            if (self._policyDir.MainCharacterEvent<ResetMagicallyChangeEvent>().ShouldEventPropagateToGame(mainEntity))
            {
                self._mappedEvent.PropagateToGame(new ResetMagicallyChangeEvent(
                    entity: mainEntity.Entity,
                    reason: reason0
                ));
            }
        }, this, netId, reason);
    }

    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    void OnProjectileTarget(ProjectileTargetData targetData)
    {
        _ecsLoop.Scheduler.Schedule(static (_, self, targetData0) =>
        {
            if (self._playerState.GetMainCharacterById(targetData0.CharacterNetId) is not { } mainEntity)
            {
                self._logger.LogError("Main character not found: {NetId}", targetData0.CharacterNetId);
                return;
            }

            if (!self._netEntity.TryGetEntityByNetworkId(targetData0.TargetNetId, out var target))
            {
                self._logger.LogNull(nameof(targetData0.TargetNetId));
                return;
            }

            if (self._policyDir.MainCharacterEvent<ProjectileTargetEvent>().ShouldEventPropagateToGame(mainEntity))
            {
                self._mappedEvent.PropagateToGame(new ProjectileTargetEvent(
                    character: mainEntity.Entity,
                    projectileName: targetData0.ProjectileName,
                    target: target.Value,
                    socketName: targetData0.SocketName
                ));
            }
        }, this, targetData);
    }

    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    void OnProjectileSwitch(ProjectileSwitchData switchData)
    {
        _ecsLoop.Scheduler.Schedule(static (_, self, switchData0) =>
        {
            if (self._playerState.GetMainCharacterById(switchData0.NetId) is not { } mainEntity)
            {
                self._logger.LogError("Main character not found: {NetId}", switchData0.NetId);
                return;
            }

            if (self._policyDir.MainCharacterEvent<ProjectileSwitchEvent>().ShouldEventPropagateToGame(mainEntity))
            {
                self._mappedEvent.PropagateToGame(new ProjectileSwitchEvent(
                    entity: mainEntity.Entity,
                    projectileClassName: switchData0.ProjectileClassName,
                    bulletSwitchId: switchData0.BulletSwitchID,
                    switchIdx: switchData0.SwitchIdx
                ));
            }
        }, this, switchData);
    }

    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    void OnProjectileDead(ProjectileDeadData data)
    {
        _ecsLoop.Scheduler.Schedule(static (_, self, data0) =>
        {
            if (self._playerState.GetMainCharacterById(data0.NetId) is not { } mainEntity)
            {
                self._logger.LogError("Main character not found: {NetId}", data0.NetId);
                return;
            }

            if (self._policyDir.MainCharacterEvent<ProjectileDeadEvent>().ShouldEventPropagateToGame(mainEntity))
            {
                self._mappedEvent.PropagateToGame(new ProjectileDeadEvent(
                    entity: mainEntity.Entity,
                    projectileClassName: data0.ProjectileClassName,
                    reason: data0.Reason
                ));
            }
        }, this, data);
    }

    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    void OnMagicFieldDead(string magicFieldClassName, EBGUBulletDestroyReason reason, NetworkId netId)
    {
        _ecsLoop.Scheduler.Schedule(static (_, self, magicFieldClassName0, reason0, netId0) =>
        {
            if (!self._netEntity.TryGetEntityByNetworkId(netId0, out var entity))
            {
                self._logger.LogError("Character not found: {NetId}", netId0);
                return;
            }

            if (self._policyDir.ForEvent<MagicFieldDeadEvent>().ShouldEventPropagateToGame(entity.Value))
            {
                self._mappedEvent.PropagateToGame(new MagicFieldDeadEvent(
                    entity: entity.Value,
                    className: magicFieldClassName0,
                    reason: reason0
                ));
            }
        }, this, magicFieldClassName, reason, netId);
    }

    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    void OnProjectileMoveMode(ProjectileMoveModeData data)
    {
        _ecsLoop.Scheduler.Schedule(static (_, self, data0) =>
        {
            if (self._playerState.GetMainCharacterById(data0.NetId) is not { } mainEntity)
            {
                self._logger.LogError("Main character not found: {NetId}", data0.NetId);
                return;
            }

            if (self._policyDir.MainCharacterEvent<ProjectileMoveModeEvent>().ShouldEventPropagateToGame(mainEntity))
            {
                self._mappedEvent.PropagateToGame(new ProjectileMoveModeEvent(
                    entity: mainEntity.Entity,
                    projectileClassName: data0.ProjectileClassName,
                    moveMode: data0.MoveMode
                ));
            }
        }, this, data);
    }

    // NOTE(api): Changed from AreaOfInterestAll
    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    private void OnPartyRespawn(int birthPointId, NetworkId netId)
    {
        _ecsLoop.Scheduler.Schedule(static (_, self, shrineId, netId0) =>
        {
            if (!self._netEntity.TryGetEntityByNetworkId(netId0, out var entity))
            {
                self._logger.LogError("Character not found: {NetId}", netId0);
                return;
            }

            if (self._policyDir.ForEvent<PartyRespawnEvent, EmptyContext>().ShouldEventPropagateToGame(default))
            {
                self._mappedEvent.PropagateToGame(new PartyRespawnEvent(
                    entity: entity.Value,
                    birthShrineId: shrineId
                ));
            }
        }, this, birthPointId, netId);
    }

    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    private void OnAfterRebirth(NetworkId netId)
    {
        _ecsLoop.Scheduler.Schedule(static (_, self, netId0) =>
        {
            if (self._playerState.GetMainCharacterById(netId0) is not { } mainEntity)
                return;

            if (self._policyDir.MainCharacterEvent<AfterRebirthEvent>().ShouldEventPropagateToGame(mainEntity))
            {
                self._mappedEvent.PropagateToGame(new AfterRebirthEvent(mainEntity.Entity));
            }
        }, this, netId);
    }

    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    private void OnRestAtShrine(int birthPointId, NetworkId netId)
    {
        _ecsLoop.Scheduler.Schedule(static (_, self, shrineId, netId0) =>
        {
            if (!self._netEntity.TryGetEntityByNetworkId(netId0, out var entity))
            {
                self._logger.LogError("Character not found: {NetId}", netId0);
                return;
            }

            if (self._policyDir.ForEvent<RestAtShrineEvent, Entity>().ShouldEventPropagateToGame(entity.Value))
            {
                self._mappedEvent.PropagateToGame(new RestAtShrineEvent(
                    entity: entity.Value,
                    rebirthPointId: shrineId
                ));
            }
        }, this, birthPointId, netId);
    }

    // NOTE(api): Changed from AreaOfInterestAll
    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    private void OnPartySoftlock(int birthPointId, NetworkId netId)
    {
        _ecsLoop.Scheduler.Schedule(static (_, self, shrineId, netId0) =>
        {
            if (!self._netEntity.TryGetEntityByNetworkId(netId0, out var entity))
            {
                self._logger.LogError("Character not found: {NetId}", netId0);
                return;
            }

            if (self._policyDir.ForEvent<PartySoftlockEvent, EmptyContext>().ShouldEventPropagateToGame(default))
            {
                self._mappedEvent.PropagateToGame(new PartySoftlockEvent(
                    entity: entity.Value,
                    birthPointId: shrineId
                ));
            }
        }, this, birthPointId, netId);
    }

    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    private void OnStartJump(StartJumpData jumpData)
    {
        _ecsLoop.Scheduler.Schedule(static (_, self, jumpData0) =>
        {
            if (self._playerState.GetMainCharacterById(jumpData0.NetId) is not { } mainEntity)
            {
                self._logger.LogError("Main character not found: {NetId}", jumpData0.NetId);
                return;
            }

            if (self._policyDir.MainCharacterEvent<StartJumpEvent>().ShouldEventPropagateToGame(mainEntity))
            {
                self._mappedEvent.PropagateToGame(new StartJumpEvent(
                    entity: mainEntity.Entity,
                    startJumpDir: jumpData0.StartJumpDir,
                    inputVector: jumpData0.InputVector
                ));
            }
        }, this, jumpData);
    }

    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    private void OnStopJump(NetworkId netId)
    {
        _ecsLoop.Scheduler.Schedule(static (_, self, netId0) =>
        {
            if (self._playerState.GetMainCharacterById(netId0) is not { } mainEntity)
            {
                self._logger.LogError("Main character not found: {NetId}", netId0);
                return;
            }

            if (self._policyDir.MainCharacterEvent<StopJumpEvent>().ShouldEventPropagateToGame(mainEntity))
            {
                self._mappedEvent.PropagateToGame(new StopJumpEvent(mainEntity.Entity));
            }
        }, this, netId);
    }

    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    private void OnMonsterWakeUp(NetworkId netId)
    {
        _ecsLoop.Scheduler.Schedule(static (_, self, netId0) =>
        {
            if (!self._netEntity.TryGetEntityByNetworkId(netId0, out var entity))
            {
                self._logger.LogNullDebug(nameof(netId0));
                return;
            }

            if (self._policyDir.ForEvent<MonsterWakeUpEvent>().ShouldEventPropagateToGame(entity.Value))
            {
                self._mappedEvent.PropagateToGame(new MonsterWakeUpEvent(entity.Value));
            }
        }, this, netId);
    }

    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    private void OnPlayBaneEffect(PlayBaneEffectData data)
    {
        _ecsLoop.Scheduler.Schedule(static (_, self, data0) =>
        {
            if (!self._netEntity.TryGetEntityByNetworkId(data0.NetId, out var entity))
            {
                self._logger.LogNullDebug(nameof(data0.NetId));
                return;
            }

            if (self._policyDir.ForEvent<PlayBaneEffectEvent>().ShouldEventPropagateToGame(entity.Value))
            {
                self._mappedEvent.PropagateToGame(new PlayBaneEffectEvent(
                    entity: entity.Value,
                    stateType: data0.StateType,
                    actionType: data0.ActionType
                ));
            }
        }, this, data);
    }

    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    private void OnStopBaneEffect(StopBaneEffectData data)
    {
        _ecsLoop.Scheduler.Schedule(static (_, self, data0) =>
        {
            if (!self._netEntity.TryGetEntityByNetworkId(data0.NetId, out var entity))
            {
                self._logger.LogNullDebug(nameof(data0.NetId));
                return;
            }

            if (self._policyDir.ForEvent<StopBaneEffectEvent>().ShouldEventPropagateToGame(entity.Value))
            {
                self._mappedEvent.PropagateToGame(new StopBaneEffectEvent(
                    entity: entity.Value,
                    stateType: data0.StateType
                ));
            }
        }, this, data);
    }

    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    internal void OnCastSkill(NetworkId casterNetId, int skillId, ECastSkillSourceType skillType)
    {
        _ecsLoop.Scheduler.Schedule(static (_, self, casterNetId0, skillId0, skillType0) =>
        {
            if (!self._netEntity.TryGetEntityByNetworkId(casterNetId0, out var casterEntity))
            {
                self._logger.LogError("Caster pawn not found: {NetId}", casterNetId0);
                return;
            }

            if (self._policyDir.ForEvent<CastSkillEvent>().ShouldEventPropagateToGame(casterEntity.Value))
            {
                self._mappedEvent.PropagateToGame(new CastSkillEvent(
                    entity: casterEntity.Value,
                    skillId: skillId0,
                    skillType: skillType0
                ));
            }
        }, this, casterNetId, skillId, skillType);
    }

    [Obsolete("To be removed once per-file RPC is implemented")]
    public event Action<ChatMessage>? OnGetChatMessage;

    [RpcEvent(RelayMode.AreaOfInterestAll)]
    internal void OnChatMessage(ChatMessage message)
    {
        _ecsLoop.Scheduler.Schedule(static (_, self, message0) => { self.OnGetChatMessage?.Invoke(message0); }, this, message);
    }

    #region PvpRPC

    [Obsolete("To be removed once per-project RPC is implemented")]
    public event Action<PlayerId, int[]>? OnPvpEventReceived;

    // NOTE(api): Changed from AreaOfInterestAll
    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    internal void OnPvpEvent(PlayerId __sender, int[] data)
    {
        OnPvpEventReceived?.Invoke(__sender, data);
    }

    [RpcEvent(RelayMode.AreaOfInterestAll)]
    internal void OnShowAntiStallWarning(int warningTime)
    {
        _ecsLoop.Scheduler.Schedule(static (_, self, warningTime0) =>
        {
            if (self._playerState.LocalMainCharacter is not { } mainEntity)
                return;

            if (mainEntity.GetState().IsDead)
                return;

            self._widgetManager.ShowInfoMessage(Texts.AntiStallWarning);
            self._timerController.SetTimer(0, warningTime0);
            self._timerController.StartTimer();
            Logging.LogDebug("OnShowAntiStallWarning received");
        }, this, warningTime);
    }

    [RpcEvent(RelayMode.AreaOfInterestAll)]
    internal void OnShowAntiStallAction()
    {
        _ecsLoop.Scheduler.Schedule(static (_, self) =>
        {
            if (self._playerState.LocalMainCharacter is not { } mainEntity)
                return;

            if (mainEntity.GetState().IsDead)
                return;

            self._widgetManager.ShowInfoMessage(Texts.StallingMessage);
            Logging.LogDebug("OnShowAntiStallAction received");
        }, this);
    }

    [RpcEvent(RelayMode.AreaOfInterestAll)]
    internal void OnHideAntiStall()
    {
        _ecsLoop.Scheduler.Schedule(static (_, self) =>
        {
            if (self._playerState.LocalMainCharacter is not { } mainEntity)
                return;

            self._widgetManager.HideInfoMessage();
            self._timerController.StopTimer();
            self._widgetManager.SetTimerVisibility(false);
            Logging.LogDebug("OnHideAntiStallWarning received");
        }, this);
    }

    [RpcEvent(RelayMode.AreaOfInterestAll)]
    internal void OnStallDamage(NetworkId netId, float value)
    {
        _ecsLoop.Scheduler.Schedule(static (_, self, netId0, value0) =>
        {
            if (self._playerState.LocalMainCharacter is not { } mainEntity)
                return;

            if (mainEntity.GetState().IsDead)
                return;

            if (self._netEntity.TryGetEntityByNetworkId(netId0, out var entity) && entity == mainEntity.Entity)
            {
                // TODO: Move to player utils
                Logging.LogDebug("Applying stall damage: {Damage}%", value0);
                var pawn = mainEntity.Pawn;
                if (pawn == null)
                    return;

                var container = BGU_DataUtil.GetReadOnlyData<IBUC_AttrContainer, BUC_AttrContainer>(pawn);
                var maxStamina = container?.GetFloatValue(EBGUAttrFloat.StaminaMax) ?? 1f;

                FSkillDamageConfig SkillDamageConfig = new()
                {
                    DamageCalcType = EDamageCalcType.HPMaxRatioAbs,
                    HPMaxINV10000Damage_Abs = value0 * 100,
                    DamageImmueLevel = 2,
                    DmgReason = EDamageReason.FallDmg
                };

                var events = BUS_EventCollectionCS.Get(pawn);
                events?.Evt_IncreaseAttrFloat.Invoke(EBGUAttrFloat.Stamina, -(maxStamina * value0 / 100 * 3));
                events?.Evt_TriggerNormalDamageEffect.Invoke(null, in SkillDamageConfig, default, new FBattleAttrSnapShot(null));
            }
        }, this, netId, value);
    }

    #endregion
}