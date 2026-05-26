using System.Diagnostics;
using b1;
using Microsoft.Extensions.Logging;
using ReadyM.Api.Idents;
using ReadyM.Api.Mapping.Events;
using ReadyM.Api.Mapping.Tags;
using ReadyM.Api.Multiplayer.Client;
using ReadyM.Api.Multiplayer.ECS.Systems;
using ReadyM.Api.Multiplayer.ECS.Values;
using ReadyM.Api.Multiplayer.Generators;
using ReadyM.Api.Multiplayer.Protocol.Enums;
using ReadyM.Api.Multiplayer.RPC;
using ReadyM.Api.Multiplayer.Serialization;
using UnrealEngine.Engine;
using WukongMp.Api.DTO;
using WukongMp.Api.ECS.Entities;
using WukongMp.Api.ECS.GameEvents;
using WukongMp.Api.Mapping.Policies.Event;
using WukongMp.Api.NameCompressors;
using WukongMp.Api.State;
using INetworkedEntityManager = ReadyM.Api.Multiplayer.ECS.Managers.INetworkedEntityManager;

// ReSharper disable InconsistentNaming

namespace WukongMp.Api;

internal partial class WukongClientRpcCallbacks(
    ReceiveSystem scheduleSystem,
    WukongPlayerState playerState,
    WukongAreaState areaState,
    IMappedEventManager mappedEvent,
    IRelaySerializer serializer,
    IRelayClient relayClient,
    INetworkedEntityManager netEntity,
    ILogger logger
)
    : RpcClassBase(relayClient, serializer)
{
    private readonly IMappedEventManager _mappedEvent = mappedEvent;
    private readonly WukongPlayerState _playerState = playerState;
    private readonly WukongAreaState _areaState = areaState;
    private readonly INetworkedEntityManager _netEntity = netEntity;
    private readonly ILogger _logger = logger;

    public override void OnScopeStart()
    {
        base.OnScopeStart();

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
                summonerNetId: ev.Summoner?.GetNetId() ?? default,
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
                catchTargetNetId: ev.CatchTarget?.GetNetId() ?? default,
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
                requesterId: ev.Requester.GetNetId(),
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
                netId: ev.Target.GetNetId(),
                targetNetId: ev.Caster.GetNetId(),
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

        _mappedEvent.RegisterEcsEventHandler<RequestTeleportEvent, WukongClientRpcCallbacks>(static (ev, self) =>
        {
            self.SendRequestTeleport(new RequestTeleportData(
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
            Debug.Assert(ev.PlayerId == self._playerState.LocalPlayerId, "ev.PlayerId == self._playerState.LocalPlayerId");

            self.SendUnitSpawned(ev.Entity.GetNetId());
        }, this);

        _mappedEvent.RegisterEcsEventHandler<UnitDespawnedEvent, WukongClientRpcCallbacks>(static (ev, self) =>
        {
            Debug.Assert(ev.PlayerId == self._playerState.LocalPlayerId, "ev.PlayerId == self._playerState.LocalPlayerId");

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
                ev.Reason
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
        SendMontageCallback(evData);
    }

    public void SendMontageCancel(NetworkId netId)
    {
        var evData = new MontageCallbackData(netId, false, "", 0f, false);
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
    private void OnExitPhantomRush(NetworkId netId)
    {
        scheduleSystem.Scheduler.Schedule(static (_, self, netId0) =>
        {
            if (self._playerState.GetMainCharacterById(netId0) is not { } mainEntity)
                return;

            self._mappedEvent.InvokeInGameIfApplicable(new ExitPhantomRushEvent(mainEntity.Entity), mainEntity.Entity);
        }, this, netId);
    }

    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    private void OnAddBuff(BuffAddData data)
    {
        scheduleSystem.Scheduler.Schedule(static (_, self, data0) =>
        {
            if (self._playerState.GetMainCharacterById(data0.NetId) is not { } mainEntity)
                return;

            self._mappedEvent.InvokeInGameIfApplicable(new AddBuffEvent(
                entity: mainEntity.Entity,
                buffId: data0.BuffId,
                duration: data0.Duration
            ), mainEntity.Entity);
        }, this, data);
    }

    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    private void OnRemoveBuff(BuffRemoveData data)
    {
        scheduleSystem.Scheduler.Schedule(static (_, self, data0) =>
        {
            if (!self._netEntity.TryGetEntityByNetworkId(data0.NetId, out var entity))
                return;

            self._mappedEvent.InvokeInGameIfApplicable(new RemoveBuffEvent(
                entity: entity.Value,
                buffId: data0.BuffId,
                triggerType: data0.TriggerType,
                layer: data0.Layer,
                withTriggerRemoveEffect: data0.WithTriggerRemoveEffect
            ), entity.Value);
        }, this, data);
    }

    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    private void OnRemoveAllBuffs(BuffRemoveAllData data)
    {
        scheduleSystem.Scheduler.Schedule(static (_, self, data0) =>
        {
            if (!self._netEntity.TryGetEntityByNetworkId(data0.NetId, out var entity))
                return;

            self._mappedEvent.InvokeInGameIfApplicable(new RemoveAllBuffsEvent(
                entity: entity.Value,
                triggerType: data0.TriggerType,
                withTriggerRemoveEffect: data0.WithTriggerRemoveEffect
            ), entity.Value);
        }, this, data);
    }

    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    private void OnUnitStateTrigger(StateTriggerData data)
    {
        scheduleSystem.Scheduler.Schedule(static (_, self, data0) =>
        {
            if (!self._netEntity.TryGetEntityByNetworkId(data0.NetId, out var entity))
                return;

            self._mappedEvent.InvokeInGameIfApplicable(new UnitStateTriggerEvent(
                entity: entity.Value,
                trigger: data0.Trigger,
                time: data0.Time,
                needForceUpdate: data0.NeedForceUpdate
            ), entity.Value);
        }, this, data);
    }

    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    private void OnUnitSimpleState(SimpleStateData data)
    {
        scheduleSystem.Scheduler.Schedule(static (_, self, data0) =>
        {
            if (!self._netEntity.TryGetEntityByNetworkId(data0.NetId, out var entity))
                return;

            self._mappedEvent.InvokeInGameIfApplicable(new UnitSimpleStateEvent(
                entity: entity.Value,
                simpleState: data0.SimpleState,
                isRemove: data0.IsRemove
            ), entity.Value);
        }, this, data);
    }

    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    private void OnTriggerFsmState(FsmStateData data)
    {
        scheduleSystem.Scheduler.Schedule(static (_, self, data0) =>
        {
            if (!self._netEntity.TryGetEntityByNetworkId(data0.NetId, out var entity))
                return;

            self._mappedEvent.InvokeInGameIfApplicable(new TriggerFsmStateEvent(
                entity: entity.Value,
                fsmStateName: data0.FsmStateName
            ), entity.Value);
        }, this, data);
    }

    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    private void OnMotionMatchingState(MotionMatchingStateData data)
    {
        scheduleSystem.Scheduler.Schedule(static (_, self, data0) =>
        {
            if (!self._netEntity.TryGetEntityByNetworkId(data0.NetId, out var entity))
                return;

            self._mappedEvent.InvokeInGameIfApplicable(new MotionMatchingStateEvent(
                entity: entity.Value,
                state: data0.State
            ), entity.Value);
        }, this, data);
    }

    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    private void OnSpawnSummon(SpawnSummonData data)
    {
        scheduleSystem.Scheduler.Schedule(static (_, self, data0) =>
        {
            self._netEntity.TryGetEntityByNetworkId(data0.SummonerNetId, out var summoner);
            self._netEntity.TryGetEntityByNetworkId(data0.CatchTargetNetId, out var catchTarget);

            var context = new SpawnSummonContext(summoner, data0.Location);
            self._mappedEvent.InvokeInGameIfApplicable(new SpawnSummonEvent(
                summoner: summoner,
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
                catchTarget: catchTarget,
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
            ), context);
        }, this, data);
    }

    [RpcEvent(RelayMode.AreaOfInterestAll)]
    private void OnRequestSpawnUnits(RequestSpawnUnitsData data)
    {
        scheduleSystem.Scheduler.Schedule(static (_, self, data0) =>
        {
            if (!self._netEntity.TryGetEntityByNetworkId(data0.RequesterId, out var entity))
            {
                self._logger.LogError("Entity not found: {NetId}", data0.RequesterId);
                return;
            }

            self._mappedEvent.InvokeInGameIfApplicable(new RequestSpawnUnitsEvent(
                requester: entity.Value,
                unitName: data0.UnitName,
                count: data0.Count,
                teamId: data0.TeamId,
                location: data0.Location
            ), entity.Value);
        }, this, data);
    }

    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    private void OnBroadcastUnitSpawn(BroadcastUnitSpawnData data)
    {
        scheduleSystem.Scheduler.Schedule(static (_, self, data0) =>
        {
            if (!self._netEntity.TryGetEntityByNetworkId(data0.NetId, out var entity))
            {
                self._logger.LogError("Entity not found: {NetId}", data0.NetId);
                return;
            }

            self._mappedEvent.InvokeInGameIfApplicable(new BroadcastUnitSpawnEvent(
                entity: entity.Value,
                unitName: data0.UnitName,
                guid: data0.Guid,
                location: data0.Location
            ), entity.Value);
        }, this, data);
    }

    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    private void OnPlayerTransBegin(PlayerTransBeginData data)
    {
        scheduleSystem.Scheduler.Schedule(static (_, self, data0) =>
        {
            if (self._playerState.GetMainCharacterById(data0.NetId) is not { } mainEntity)
                return;

            self._mappedEvent.InvokeInGameIfApplicable(new PlayerTransBeginEvent(
                entity: mainEntity.Entity,
                unitResId: data0.UnitResId,
                unitBornSkillId: data0.UnitBornSkillId,
                enableBlendViewTarget: data0.EnableBlendViewTarget,
                transBeginType: data0.TransBeginType
            ), mainEntity.Entity);
        }, this, data);
    }

    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    private void OnPlayerTransEnd(PlayerTransEndData data)
    {
        scheduleSystem.Scheduler.Schedule(static (_, self, data0) =>
        {
            if (self._playerState.GetMainCharacterById(data0.NetId) is not { } mainEntity)
                return;

            self._mappedEvent.InvokeInGameIfApplicable(new PlayerTransEndEvent(
                entity: mainEntity.Entity,
                unitResId: data0.UnitResId,
                unitBornSkillId: data0.UnitBornSkillId,
                enableBlendViewTarget: data0.EnableBlendViewTarget,
                transEndType: data0.TransEndType
            ), mainEntity.Entity);
        }, this, data);
    }

    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    private void OnPlayMovieRequest(PlayerId __sender, PlayMovieRequestData requestData)
    {
        scheduleSystem.Scheduler.Schedule(static (_, self, data0, _) =>
        {
            self._mappedEvent.InvokeInGameIfApplicable(new PlayMovieRequestEvent(
                sequenceId: data0.SequenceId,
                disablePlayerControl: data0.DisablePlayerControl,
                disableMovementInput: data0.DisableMovementInput,
                disableLookAtInput: data0.DisableLookAtInput,
                hidePlayer: data0.HidePlayer,
                hideHud: data0.HideHud,
                overlapBoxGuid: data0.OverlapBoxGuid,
                matchType: data0.MatchType
            ), default(EmptyContext));
        }, this, requestData, __sender);
    }

    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    private void OnSetTarget(SetTargetData data)
    {
        scheduleSystem.Scheduler.Schedule(static (_, self, data0) =>
        {
            if (!self._netEntity.TryGetEntityByNetworkId(data0.CharacterNetId, out var character))
            {
                self._logger.LogNull(nameof(data0.CharacterNetId));
                return;
            }

            if (data0.ClearTarget)
            {
                self._mappedEvent.InvokeInGameIfApplicable(new SetTargetEvent(
                    character: character.Value,
                    target: default,
                    clearTarget: true
                ), character.Value);
                return;
            }

            if (!self._netEntity.TryGetEntityByNetworkId(data0.TargetNetId, out var target))
            {
                self._logger.LogNull(nameof(data0.TargetNetId));
                return;
            }

            self._mappedEvent.InvokeInGameIfApplicable(new SetTargetEvent(
                character: character.Value,
                target: target.Value,
                clearTarget: false
            ), character.Value);
        }, this, data);
    }

    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    private void OnCastImmobilize(NetworkId caster)
    {
        scheduleSystem.Scheduler.Schedule(static (_, self, caster0) =>
        {
            // NOTE(api): This extra check emulates sending event to master only
            if (!self._areaState.IsMasterClient)
                return;

            if (!self._netEntity.TryGetEntityByNetworkId(caster0, out var casterEntity))
            {
                self._logger.LogNull(nameof(caster));
                return;
            }

            self._mappedEvent.InvokeInGameIfApplicable(new CastImmobilizeEvent(casterEntity.Value), casterEntity.Value);
        }, this, caster);
    }

    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    private void OnTriggerImmobilize(TriggerImmobilizeData data)
    {
        scheduleSystem.Scheduler.Schedule(static (_, self, data0) =>
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

            self._mappedEvent.InvokeInGameIfApplicable(new TriggerImmobilizeEvent(
                target: caster.Value,
                caster: target.Value,
                greatSageTalentActiveBuff: data0.GreatSageTalentActiveBuff
            ), default(EmptyContext));
        }, this, data);
    }

    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    private void OnRelieveImmobilize(NetworkId affected)
    {
        scheduleSystem.Scheduler.Schedule(static (_, self, affected0) =>
        {
            if (!self._netEntity.TryGetEntityByNetworkId(affected0, out var affectedEntity))
            {
                self._logger.LogNullDebug(nameof(affected));
                return;
            }

            self._mappedEvent.InvokeInGameIfApplicable(new RelieveImmobilizeEvent(affectedEntity.Value), default(EmptyContext));
        }, this, affected);
    }

    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    private void OnPhantomRush(NetworkId netId, ESkillDirection direction)
    {
        scheduleSystem.Scheduler.Schedule(static (_, self, netId0, direction0) =>
        {
            if (self._playerState.GetMainCharacterById(netId0) is not { } mainEntity)
                return;

            self._mappedEvent.InvokeInGameIfApplicable(new PhantomRushEvent(mainEntity.Entity, direction0), mainEntity.Entity);
        }, this, netId, direction);
    }

    // NOTE(api): Changed from AreaOfInterestAll
    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    private void OnRequestTeleport(RequestTeleportData data)
    {
        scheduleSystem.Scheduler.Schedule(static (_, self, data) =>
        {
            if (!self._netEntity.TryGetEntityByNetworkId(data.NetId, out var entity))
            {
                self._logger.LogNullDebug(nameof(entity));
                return;
            }

            self._mappedEvent.InvokeInGameIfApplicable(new RequestTeleportEvent(
                entity: entity.Value,
                location: data.Location,
                rotation: data.Rotation
            ), default(EmptyContext));
        }, this, data);
    }

    // NOTE(api): Changed from AreaOfInterestAll
    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    private void OnRebirthPlayer(NetworkId netId, bool isTeleport)
    {
        scheduleSystem.Scheduler.Schedule(static (_, self, netId0, isTeleport0) =>
        {
            self._logger.LogDebug("RebirthPlayer for main character {NetId} called", netId0);

            if (self._playerState.GetMainCharacterById(netId0) is not { } mainEntity)
                return;

            self._mappedEvent.InvokeInGameIfApplicable(new RebirthPlayerEvent(
                entity: mainEntity.Entity,
                teleport: isTeleport0
            ), default(EmptyContext));
        }, this, netId, isTeleport);
    }

    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    private void OnDamageNum(DamageNumParam damageNum, NetworkId netId)
    {
        scheduleSystem.Scheduler.Schedule(static (_, self, damageNum0, netId0) =>
        {
            if (!self._netEntity.TryGetEntityByNetworkId(netId0, out var entity))
            {
                self._logger.LogError("Character not found: {NetId}", netId0);
                return;
            }

            self._mappedEvent.InvokeInGameIfApplicable(new DamageNumEvent(
                entity: entity.Value,
                damageType: damageNum0.DamageType,
                damageNum: damageNum0.DamageNum,
                amplitude: damageNum0.Amplitude,
                realHitLocation: damageNum0.RealHitLocation,
                realHitDir: damageNum0.RealHitDir,
                attackerTeamType: damageNum0.AttackerTeamType
            ), entity.Value);
        }, this, damageNum, netId);
    }

    // NOTE(api): Changed from AreaOfInterestAll
    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    private void OnTeleportFinish(NetworkId netId)
    {
        scheduleSystem.Scheduler.Schedule(static (_, self, netId0) =>
        {
            if (self._playerState.GetMainCharacterById(netId0) is not { } mainEntity)
            {
                self._logger.LogError("Main character not found: {NetId}", netId0);
                return;
            }

            self._mappedEvent.InvokeInGameIfApplicable(new TeleportFinishEvent(mainEntity.Entity), default(EmptyContext));
        }, this, netId);
    }

    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    public void OnMontageCallback(MontageCallbackData data)
    {
        scheduleSystem.Scheduler.Schedule(static (_, self, data0) =>
        {
            if (!self._netEntity.TryGetEntityByNetworkId(data0.NetId, out var entity))
            {
                self._logger.LogNullDebug(nameof(data0.NetId));
                return;
            }

            var fullMontagePath = string.IsNullOrEmpty(data0.MontagePath) ? ""
                : data0.Compressed ? Compressors.MontageNameCompressor.Decompress(data0.MontagePath) : data0.MontagePath;

            self._mappedEvent.InvokeInGameIfApplicable(new MontageCallbackEvent(
                entity: entity.Value,
                fullMontagePath: fullMontagePath,
                position: data0.Position,
                reset: data0.Reset
            ), entity.Value);
        }, this, data);
    }

    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    private void OnUnitDead(UnitDeadData data)
    {
        scheduleSystem.Scheduler.Schedule(static (_, self, data0) =>
        {
            if (!self._netEntity.TryGetEntityByNetworkId(data0.NetId, out var entity))
            {
                self._logger.LogNullDebug(nameof(data0.NetId));
                return;
            }

            self._mappedEvent.InvokeInGameIfApplicable(new UnitDeadEvent(
                entity: entity.Value,
                deadReason: data0.DeadReason,
                dmgId: data0.DmgId,
                stiffLevel: data0.StiffLevel,
                isDotDmg: data0.IsDotDmg,
                abnormalType: data0.AbnormalType
            ), entity.Value);
        }, this, data);
    }

    [RpcEvent(RelayMode.GlobalOthers)]
    private void OnWaitingForSequence(SequenceWaitingData data)
    {
        scheduleSystem.Scheduler.Schedule(static (_, self, data0) =>
        {
            self._mappedEvent.InvokeInGameIfApplicable(new WaitingForSequenceEvent(
                sequenceId: data0.SequenceID,
                sequenceLocation: data0.SequenceLocation
            ), default(EmptyContext));
        }, this, data);
    }

    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    private void OnIronBodyStart(NetworkId netId)
    {
        scheduleSystem.Scheduler.Schedule(static (_, self, netId0) =>
        {
            if (self._playerState.GetMainCharacterById(netId0) is not { } mainEntity)
            {
                self._logger.LogError("Main character not found: {NetId}", netId0);
                return;
            }

            self._mappedEvent.InvokeInGameIfApplicable(new IronBodyStartEvent(mainEntity.Entity), mainEntity.Entity);
        }, this, netId);
    }

    // TODO: Find a better way to synchronize this event. If it's sent in entity owner mode, the server may not have the entity's data yet.
    // NOTE(api): Changed from AreaOfInterestAll
    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    private void OnUnitSpawned(PlayerId __sender, NetworkId netId)
    {
        scheduleSystem.Scheduler.Schedule(static (_, self, sender, netId0) =>
        {
            self._logger.LogDebug("OnUnitSpawned called for player {PlayerId} with entity: {NetId}", sender, netId0);

            if (!self._netEntity.TryGetEntityByNetworkId(netId0, out var entity))
                return;

            self._mappedEvent.InvokeInGameIfApplicable(new UnitSpawnedEvent(
                entity: entity.Value,
                playerId: sender
            ), default(EmptyContext));
        }, this, __sender, netId);
    }

    [RpcEvent(RelayMode.EntityOwner)]
    private void OnUnitDespawned(PlayerId __sender, NetworkId netId)
    {
        scheduleSystem.Scheduler.Schedule(static (_, self, sender, netId0) =>
        {
            // NOTE: There's an extra check so that we don't send this to ourselves
            if (sender == self._playerState.LocalPlayerId)
                return;

            self._logger.LogDebug("OnUnitDespawn called for player {PlayerId} with entity {NetId}", sender, netId0);

            if (!self._netEntity.TryGetEntityByNetworkId(netId0, out var entity))
                return;

            self._mappedEvent.InvokeInGameIfApplicable(new UnitDespawnedEvent(
                entity: entity.Value,
                playerId: sender
            ), default(EmptyContext));
        }, this, __sender, netId);
    }

    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    private void OnTamerSkillInteract(TamerSkillInteractData interactData)
    {
        scheduleSystem.Scheduler.Schedule(static (_, self, interactData0) =>
        {
            if (!self._netEntity.TryGetEntityByNetworkId(interactData0.NetId, out var entity))
                return;

            self._mappedEvent.InvokeInGameIfApplicable(new TamerSkillInteractEvent(
                entity: entity.Value,
                skillId: interactData0.SkillId
            ), default(EmptyContext));
        }, this, interactData);
    }

    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    private void OnTriggerMagicallyChange(MagicallyChangeData data)
    {
        scheduleSystem.Scheduler.Schedule(static (_, self, data0) =>
        {
            if (self._playerState.GetMainCharacterById(data0.NetId) is not { } mainEntity)
            {
                self._logger.LogError("Main character not found: {NetId}", data0.NetId);
                return;
            }

            var fullPath = data0.Compressed ? Compressors.VigorNameCompressor.Decompress(data0.ConfigAssetName) : data0.ConfigAssetName;
            self._mappedEvent.InvokeInGameIfApplicable(new TriggerMagicallyChangeEvent(
                entity: mainEntity.Entity,
                configPathName: fullPath,
                skillId: data0.SkillID,
                recoverSkillId: data0.RecoverSkillID,
                curVigorSkillId: data0.CurVigorSkillID,
                castReason: data0.CastReason
            ), mainEntity.Entity);
        }, this, data);
    }

    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    private void OnResetMagicallyChange(NetworkId netId, EResetReason_MagicallyChange reason)
    {
        scheduleSystem.Scheduler.Schedule(static (_, self, netId0, reason0) =>
        {
            if (self._playerState.GetMainCharacterById(netId0) is not { } mainEntity)
            {
                self._logger.LogError("Main character not found: {NetId}", netId0);
                return;
            }

            self._mappedEvent.InvokeInGameIfApplicable(new ResetMagicallyChangeEvent(
                entity: mainEntity.Entity,
                reason: reason0
            ), mainEntity.Entity);
        }, this, netId, reason);
    }

    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    void OnProjectileTarget(ProjectileTargetData targetData)
    {
        scheduleSystem.Scheduler.Schedule(static (_, self, targetData0) =>
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

            self._mappedEvent.InvokeInGameIfApplicable(new ProjectileTargetEvent(
                character: mainEntity.Entity,
                projectileName: targetData0.ProjectileName,
                target: target.Value,
                socketName: targetData0.SocketName
            ), mainEntity.Entity);
        }, this, targetData);
    }

    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    void OnProjectileSwitch(ProjectileSwitchData switchData)
    {
        scheduleSystem.Scheduler.Schedule(static (_, self, switchData0) =>
        {
            if (self._playerState.GetMainCharacterById(switchData0.NetId) is not { } mainEntity)
            {
                self._logger.LogError("Main character not found: {NetId}", switchData0.NetId);
                return;
            }

            self._mappedEvent.InvokeInGameIfApplicable(new ProjectileSwitchEvent(
                entity: mainEntity.Entity,
                projectileClassName: switchData0.ProjectileClassName,
                bulletSwitchId: switchData0.BulletSwitchID,
                switchIdx: switchData0.SwitchIdx
            ), mainEntity.Entity);
        }, this, switchData);
    }

    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    void OnProjectileDead(ProjectileDeadData data)
    {
        scheduleSystem.Scheduler.Schedule(static (_, self, data0) =>
        {
            if (self._playerState.GetMainCharacterById(data0.NetId) is not { } mainEntity)
            {
                self._logger.LogError("Main character not found: {NetId}", data0.NetId);
                return;
            }

            self._mappedEvent.InvokeInGameIfApplicable(new ProjectileDeadEvent(
                entity: mainEntity.Entity,
                projectileClassName: data0.ProjectileClassName,
                reason: data0.Reason
            ), mainEntity.Entity);
        }, this, data);
    }

    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    void OnMagicFieldDead(string magicFieldClassName, EBGUBulletDestroyReason reason)
    {
        scheduleSystem.Scheduler.Schedule(static (_, self, magicFieldClassName0, reason0) =>
        {
            self._mappedEvent.InvokeInGameIfApplicable(new MagicFieldDeadEvent(
                className: magicFieldClassName0,
                reason: reason0
            ), default(EmptyContext));
        }, this, magicFieldClassName, reason);
    }

    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    void OnProjectileMoveMode(ProjectileMoveModeData data)
    {
        scheduleSystem.Scheduler.Schedule(static (_, self, data0) =>
        {
            if (self._playerState.GetMainCharacterById(data0.NetId) is not { } mainEntity)
            {
                self._logger.LogError("Main character not found: {NetId}", data0.NetId);
                return;
            }

            self._mappedEvent.InvokeInGameIfApplicable(new ProjectileMoveModeEvent(
                entity: mainEntity.Entity,
                projectileClassName: data0.ProjectileClassName,
                moveMode: data0.MoveMode
            ), mainEntity.Entity);
        }, this, data);
    }

    // NOTE(api): Changed from AreaOfInterestAll
    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    private void OnPartyRespawn(int birthPointId, NetworkId netId)
    {
        scheduleSystem.Scheduler.Schedule(static (_, self, shrineId, netId0) =>
        {
            if (!self._netEntity.TryGetEntityByNetworkId(netId0, out var entity))
            {
                self._logger.LogError("Character not found: {NetId}", netId0);
                return;
            }

            self._mappedEvent.InvokeInGameIfApplicable(new PartyRespawnEvent(
                entity: entity.Value,
                birthShrineId: shrineId
            ), default(EmptyContext));
        }, this, birthPointId, netId);
    }

    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    private void OnAfterRebirth(NetworkId netId)
    {
        scheduleSystem.Scheduler.Schedule(static (_, self, netId0) =>
        {
            if (self._playerState.GetMainCharacterById(netId0) is not { } mainEntity)
                return;

            self._mappedEvent.InvokeInGameIfApplicable(new AfterRebirthEvent(mainEntity.Entity), mainEntity.Entity);
        }, this, netId);
    }

    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    private void OnRestAtShrine(int birthPointId, NetworkId netId)
    {
        scheduleSystem.Scheduler.Schedule(static (_, self, shrineId, netId0) =>
        {
            if (!self._netEntity.TryGetEntityByNetworkId(netId0, out var entity))
            {
                self._logger.LogError("Character not found: {NetId}", netId0);
                return;
            }

            self._mappedEvent.InvokeInGameIfApplicable(new RestAtShrineEvent(
                entity: entity.Value,
                rebirthPointId: shrineId
            ), entity.Value);
        }, this, birthPointId, netId);
    }

    // NOTE(api): Changed from AreaOfInterestAll
    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    private void OnPartySoftlock(int birthPointId, NetworkId netId)
    {
        scheduleSystem.Scheduler.Schedule(static (_, self, shrineId, netId0) =>
        {
            if (!self._netEntity.TryGetEntityByNetworkId(netId0, out var entity))
            {
                self._logger.LogError("Character not found: {NetId}", netId0);
                return;
            }

            self._mappedEvent.InvokeInGameIfApplicable(new PartySoftlockEvent(
                entity: entity.Value,
                birthPointId: shrineId
            ), default(EmptyContext));
        }, this, birthPointId, netId);
    }

    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    private void OnStartJump(StartJumpData jumpData)
    {
        scheduleSystem.Scheduler.Schedule(static (_, self, jumpData0) =>
        {
            if (self._playerState.GetMainCharacterById(jumpData0.NetId) is not { } mainEntity)
            {
                self._logger.LogError("Main character not found: {NetId}", jumpData0.NetId);
                return;
            }

            self._mappedEvent.InvokeInGameIfApplicable(new StartJumpEvent(
                entity: mainEntity.Entity,
                startJumpDir: jumpData0.StartJumpDir,
                inputVector: jumpData0.InputVector
            ), mainEntity.Entity);
        }, this, jumpData);
    }

    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    private void OnStopJump(NetworkId netId)
    {
        scheduleSystem.Scheduler.Schedule(static (_, self, netId0) =>
        {
            if (self._playerState.GetMainCharacterById(netId0) is not { } mainEntity)
            {
                self._logger.LogError("Main character not found: {NetId}", netId0);
                return;
            }

            self._mappedEvent.InvokeInGameIfApplicable(new StopJumpEvent(mainEntity.Entity), mainEntity.Entity);
        }, this, netId);
    }

    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    private void OnMonsterWakeUp(NetworkId netId)
    {
        scheduleSystem.Scheduler.Schedule(static (_, self, netId0) =>
        {
            if (!self._netEntity.TryGetEntityByNetworkId(netId0, out var entity))
            {
                self._logger.LogNullDebug(nameof(netId0));
                return;
            }

            self._mappedEvent.InvokeInGameIfApplicable(new MonsterWakeUpEvent(entity.Value), entity.Value);
        }, this, netId);
    }

    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    private void OnPlayBaneEffect(PlayBaneEffectData data)
    {
        scheduleSystem.Scheduler.Schedule(static (_, self, data0) =>
        {
            if (!self._netEntity.TryGetEntityByNetworkId(data0.NetId, out var entity))
            {
                self._logger.LogNullDebug(nameof(data0.NetId));
                return;
            }

            self._mappedEvent.InvokeInGameIfApplicable(new PlayBaneEffectEvent(
                entity: entity.Value,
                stateType: data0.StateType,
                actionType: data0.ActionType
            ), entity.Value);
        }, this, data);
    }

    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    private void OnStopBaneEffect(StopBaneEffectData data)
    {
        scheduleSystem.Scheduler.Schedule(static (_, self, data0) =>
        {
            if (!self._netEntity.TryGetEntityByNetworkId(data0.NetId, out var entity))
            {
                self._logger.LogNullDebug(nameof(data0.NetId));
                return;
            }

            self._mappedEvent.InvokeInGameIfApplicable(new StopBaneEffectEvent(
                entity: entity.Value,
                stateType: data0.StateType
            ), entity.Value);
        }, this, data);
    }

    [RpcEvent(RelayMode.AreaOfInterestOthers)]
    private void OnCastSkill(NetworkId casterNetId, int skillId, ECastSkillSourceType skillType)
    {
        scheduleSystem.Scheduler.Schedule(static (_, self, casterNetId0, skillId0, skillType0) =>
        {
            if (!self._netEntity.TryGetEntityByNetworkId(casterNetId0, out var casterEntity))
            {
                self._logger.LogError("Caster pawn not found: {NetId}", casterNetId0);
                return;
            }

            self._mappedEvent.InvokeInGameIfApplicable(new CastSkillEvent(
                entity: casterEntity.Value,
                skillId: skillId0,
                skillType: skillType0
            ), casterEntity.Value);
        }, this, casterNetId, skillId, skillType);
    }
}