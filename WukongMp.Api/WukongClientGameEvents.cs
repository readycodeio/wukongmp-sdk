using System.Reflection;
using b1;
using b1.BGW;
using HarmonyLib;
using Microsoft.Extensions.Logging;
using ReadyM.Api.Multiplayer.Mapping.Events;
using ReadyM.Relay.Client;
using ReadyM.Relay.Client.State;
using ReadyM.Wukong.Common.ECS.Components;
using UnrealEngine.Engine;
using UnrealEngine.Runtime;
using WukongMp.Api.Configuration;
using WukongMp.Api.ECS.Entities;
using WukongMp.Api.ECS.GameEvents;
using WukongMp.Api.Mapping;
using WukongMp.Api.State;
using WukongMp.Api.UI;
using WukongMp.Api.WukongUtils;

namespace WukongMp.Api;

internal class WukongClientGameEvents(
    IMappedEventManager mappedEvent,
    WukongMappingPolicyDirectory policyDir,
    ClientState state,
    WukongPawnState pawnState,
    WukongPlayerState playerState,
    WukongWidgetManager widgetManager,
    GameplayEventRouter eventRouter,
    ILogger logger
) : IScopedLifetime
{
    // ReSharper disable once InconsistentNaming
    private static readonly MethodInfo PlayDBC_ByType = AccessTools.Method(typeof(BGU_AbnormalStateHandlerBase), "PlayDBC_ByType");

    // ReSharper disable once InconsistentNaming
    private static readonly MethodInfo EndAllDBC = AccessTools.Method(typeof(BGU_AbnormalStateHandlerBase), "EndAllDBC");

    private readonly WukongMappingPolicyDirectory _policyDir = policyDir;
    private readonly ClientState _state = state;
    private readonly WukongPawnState _pawnState = pawnState;
    private readonly WukongPlayerState _playerState = playerState;
    private readonly WukongWidgetManager _widgetManager = widgetManager;
    private readonly GameplayEventRouter _eventRouter = eventRouter;

    private readonly ILogger _logger = logger;

    public void OnScopeStart()
    {
        mappedEvent.RegisterGameEventHandler<ExitPhantomRushEvent, WukongClientGameEvents>(static (ev, self) =>
        {
            var mainEntity = new MainCharacterEntity(ev.Entity);
            ref var mainComp = ref mainEntity.GetState();
            self._logger.LogDebug("Received exit phantom rush for main character {Entity} and {Nickname}", mainEntity.GetNetId(), mainComp.CharacterNickname);
            var events = BUS_EventCollectionCS.Get(mainEntity.Pawn);
            events?.Evt_RelievePhantomRush.Invoke();
        }, this);

        mappedEvent.RegisterGameEventHandler<AddBuffEvent, WukongClientGameEvents>(static (ev, self) =>
        {
            var pawn = self._pawnState.GetPawnByEntity(ev.Entity);

            if (pawn == null)
                return;

            BuffUtils.AddBuff(pawn, ev.BuffId, ev.Duration);
        }, this);

        mappedEvent.RegisterGameEventHandler<RemoveBuffEvent, WukongClientGameEvents>(static (ev, self) =>
        {
            var pawn = self._pawnState.GetPawnByEntity(ev.Entity);

            if (pawn == null)
                return;

            BuffUtils.RemoveBuff(pawn, ev.BuffId, ev.TriggerType, ev.Layer, ev.WithTriggerRemoveEffect);
        }, this);

        mappedEvent.RegisterGameEventHandler<RemoveAllBuffsEvent, WukongClientGameEvents>(static (ev, self) =>
        {
            var pawn = self._pawnState.GetPawnByEntity(ev.Entity);

            if (pawn == null)
                return;

            BuffUtils.RemoveAllBuffs(pawn, ev.TriggerType, ev.WithTriggerRemoveEffect);
        }, this);

        mappedEvent.RegisterGameEventHandler<UnitStateTriggerEvent, WukongClientGameEvents>(static (ev, self) =>
        {
            var character = self._pawnState.GetPawnByEntity(ev.Entity);
            NpcLocomotionUtils.SetStateTrigger(character, ev.Trigger, ev.Time, ev.NeedForceUpdate);
        }, this);

        mappedEvent.RegisterGameEventHandler<UnitSimpleStateEvent, WukongClientGameEvents>(static (ev, self) =>
        {
            var character = self._pawnState.GetPawnByEntity(ev.Entity);
            NpcLocomotionUtils.SetSimpleState(character, ev.SimpleState, ev.IsRemove);
        }, this);

        mappedEvent.RegisterGameEventHandler<TriggerFsmStateEvent, WukongClientGameEvents>(static (ev, self) =>
        {
            var character = self._pawnState.GetPawnByEntity(ev.Entity);
            NpcLocomotionUtils.SetFsmState(character, ev.FsmStateName);
        }, this);

        mappedEvent.RegisterGameEventHandler<MotionMatchingStateEvent, WukongClientGameEvents>(static (ev, self) =>
        {
            var character = self._pawnState.GetPawnByEntity(ev.Entity);
            NpcLocomotionUtils.SetMotionMatchingState(character, ev.State);
        }, this);

        mappedEvent.RegisterGameEventHandler<SpawnSummonEvent, WukongClientGameEvents>(static (ev, self) =>
        {
            self._logger.LogDebug("Received OnSpawnSummon with guid {Guid} for tamer path {Path}", ev.SummonGuid, ev.SummonClassPath);
            SpawningUtils.SpawnSummonedUnitWithGuid(ev.ToGame(self._pawnState));
        }, this);

        mappedEvent.RegisterGameEventHandler<RequestSpawnUnitsEvent, WukongClientGameEvents>(static (ev, self) => { SpawningUtils.SpawnUnitsAsOwner(self._playerState, self._pawnState, self._policyDir, new TamerKind(ev.UnitName), ev.Count, ev.TeamId, ev.Location); }, this);

        mappedEvent.RegisterGameEventHandler<BroadcastUnitSpawnEvent>(static ev => { SpawningUtils.SpawnUnitLocallyByName(ev.Guid, new TamerKind(ev.UnitName), ev.Location); });

        mappedEvent.RegisterGameEventHandler<PlayerTransBeginEvent, WukongClientGameEvents>(static (ev, self) =>
        {
            var mainEntity = new MainCharacterEntity(ev.Entity);
            TransformationUtils.TransformPlayer(mainEntity, ev.UnitResId, ev.UnitBornSkillId, ev.EnableBlendViewTarget, ev.TransBeginType);
        }, this);

        mappedEvent.RegisterGameEventHandler<PlayerTransEndEvent, WukongClientGameEvents>(static (ev, self) =>
        {
            var mainEntity = new MainCharacterEntity(ev.Entity);
            TransformationUtils.TransformPlayerBack(mainEntity, ev.UnitResId, ev.UnitBornSkillId, ev.EnableBlendViewTarget, ev.TransEndType);
        }, this);

        mappedEvent.RegisterGameEventHandler<PlayMovieRequestEvent>(CutsceneUtils.PlayCutscene);

        mappedEvent.RegisterGameEventHandler<SetTargetEvent, WukongClientGameEvents>(static (ev, self) =>
        {
            var pawn = self._pawnState.GetPawnByEntity(ev.Character);
            if (pawn == null)
            {
                self._logger.LogNullDebug(nameof(ev.Character));
                return;
            }

            if (ev.ClearTarget)
            {
                TargetingUtils.ClearTarget(pawn);
                return;
            }

            var target = self._pawnState.GetPawnByEntity(ev.Target);

            if (target == null)
            {
                self._logger.LogNull(nameof(ev.Target));
                return;
            }

            TargetingUtils.SetTarget(pawn, target);
        }, this);

        mappedEvent.RegisterGameEventHandler<CastImmobilizeEvent, WukongClientGameEvents>(static (ev, self) =>
        {
            var character = self._pawnState.GetPawnByEntity(ev.Caster);
            if (character == null)
            {
                self._logger.LogNull(nameof(ev.Caster));
                return;
            }

            ImmobilizeUtils.CastImmobilize(character);
        }, this);

        mappedEvent.RegisterGameEventHandler<TriggerImmobilizeEvent, WukongClientGameEvents>(static (ev, self) =>
        {
            var target = self._pawnState.GetPawnByEntity(ev.Target);
            var caster = self._pawnState.GetPawnByEntity(ev.Caster);
            ImmobilizeUtils.TriggerImmobilize(target, caster, ev.GreatSageTalentActiveBuff);
        }, this);

        mappedEvent.RegisterGameEventHandler<RelieveImmobilizeEvent, WukongClientGameEvents>(static (ev, self) =>
        {
            var character = self._pawnState.GetPawnByEntity(ev.Affected);
            if (character == null)
            {
                self._logger.LogNullDebug(nameof(ev.Affected));
                return;
            }

            ImmobilizeUtils.RelieveImmobilize(character);
        }, this);

        mappedEvent.RegisterGameEventHandler<PhantomRushEvent, WukongClientGameEvents>(static (ev, self) =>
        {
            var mainEntity = new MainCharacterEntity(ev.Entity);

            if (mainEntity.Pawn == null)
            {
                self._logger.LogError("Main character not found: {Entity}", ev.Entity.GetNetId());
                return;
            }

            ref var mainComp = ref mainEntity.GetState();
            self._logger.LogDebug("Received phantom rush for main character {Entity} and {Nickname} in direction {Direction}", ev.Entity.GetNetId(), mainComp.CharacterNickname, ev.Direction);
            var events = BUS_EventCollectionCS.Get(mainEntity.Pawn);
            events?.Evt_TriggerPhantomRush.Invoke(ev.Direction);

            // reset mana and cooldowns of the sender's pawn, since it's a remote player who needs to keep track of them
            PlayerUtils.ResetCooldown(mainEntity.Pawn);
            PlayerUtils.ResetMana(mainEntity.Pawn);

            // NOTE(api): This is handling a special case where the target is the event entity 
            // unattach tracking camera if target was the sender
            if (self._playerState.LocalMainCharacter.HasValue)
            {
                var localPawn = self._playerState.LocalMainCharacter.Value.Pawn;
                if (localPawn != null)
                {
                    var localTargetData = BGU_DataUtil.GetReadOnlyData<IBUC_TargetInfoData, BUC_TargetInfoData>(localPawn);
                    if (localTargetData?.GetTargetInfo()?.LockTargetActor == mainEntity.Pawn)
                    {
                        var localEvents = BUS_EventCollectionCS.Get(localPawn);
                        localEvents.Evt_ClearCameraLock?.Invoke();
                    }
                }
            }
        }, this);

        mappedEvent.RegisterGameEventHandler<RequestTeleportEvent, WukongClientGameEvents>(static (ev, self) =>
        {
            if (ev.Entity.HasComponent<MainCharacterComponent>())
            {
                var mainEntity = new MainCharacterEntity(ev.Entity);
                PlayerUtils.TeleportLocalPlayer(mainEntity, ev.Location, ev.Rotation, true);
            }
            else if (ev.Entity.HasComponent<TamerComponent>())
            {
                var tamerEntity = new TamerEntity(ev.Entity);
                TamerUtils.TeleportTamer(tamerEntity, ev.Location, ev.Rotation);
            }
            else
            {
                self._logger.LogError("Received RequestTeleportEvent for unsupported entity {Entity}", ev.Entity.GetNetId());
            }
        }, this);

        mappedEvent.RegisterGameEventHandler<RebirthPlayerEvent, WukongClientGameEvents>(static (ev, self) =>
        {
            self._logger.LogDebug("RebirthPlayer for main character {Entity} called", ev.Entity.GetNetId());

            var mainEntity = new MainCharacterEntity(ev.Entity);

            if (ev.Teleport)
            {
                // NOTE(api): Moved from WukongChatter
                PlayerUtils.TeleportLocalPlayerToCurrentRebirthPoint(mainEntity);
            }

            if (mainEntity == self._playerState.LocalMainCharacter)
            {
                PlayerUtils.DisableSpectator(mainEntity);
            }

            if (mainEntity.Pawn != null)
            {
                PlayerUtils.RebirthPlayerInPlace(mainEntity.Pawn);
            }
        }, this);

        mappedEvent.RegisterGameEventHandler<DamageNumEvent>(static ev =>
        {
            var uiEvt = BGW_UIEventCollection.Get(GameUtils.GetWorld());
            var param = new DamageNumParam(
                InDamageType: ev.DamageType,
                InDamageNum: ev.DamageNum,
                InAmplitude: ev.Amplitude,
                InRealHitLocation: ev.RealHitLocation,
                InRealHitDir: ev.RealHitDir,
                _AttackerTeamType: ev.AttackerTeamType);
            uiEvt.Evt_UI_ShowHPChangeNum(param);
        });

        mappedEvent.RegisterGameEventHandler<TeleportFinishEvent>(static ev =>
        {
            var mainEntity = new MainCharacterEntity(ev.Entity);
            var events = BUS_EventCollectionCS.Get(mainEntity.Pawn);
            events?.Evt_UnitStateTrigger.Invoke(EBUStateTrigger.TeleportEnd, -1f);
            events?.Evt_TeleportFinish.Invoke();
        });

        mappedEvent.RegisterGameEventHandler<MontageCallbackEvent, WukongClientGameEvents>(static (ev, self) =>
        {
            // FIXME(api): Move this logic to utils

            var pawn = self._pawnState.GetPawnByEntity(ev.Entity);
            if (pawn == null)
            {
                self._logger.LogNullDebug(nameof(ev.Entity));
                return;
            }

            if (string.IsNullOrEmpty(ev.FullMontagePath))
            {
                var current = pawn.GetCurrentMontage();
                if (current != null)
                {
                    var time = pawn.Mesh.GetAnimInstance().Montage_GetPosition(current);
                    self._logger.LogDebug("Received montage cancel at {Time} for entity {Entity} - {Montage}", time, ev.Entity.GetNetId(), current.PathName);
                }

                pawn.StopAnimMontage(null);
                return;
            }

            var fullMontagePath = ev.FullMontagePath;

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
            if (currentMontage != null && currentMontage.PathName == fullMontagePath && !ev.Reset)
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

            if (ev.FullMontagePath == "LYS/LYS_KJLDragon/new/Montage/AM_LYS_KJLDragon_Atk_14_monster")
            {
                self._logger.LogDebug("Received host attack with offset {Offset} (reset: {Reset})", ev.Position, ev.Reset);
            }

            animInstance.Montage_Play(montage, 1f, EMontagePlayReturnType.MontageLength, ev.Position);
            events.Evt_PlayMontageCallback.Invoke(EMontageBindReason.Default, montage, EMontageCallbackState.OnStarted);
        }, this);

        mappedEvent.RegisterGameEventHandler<UnitDeadEvent, WukongClientGameEvents>(static (ev, self) =>
        {
            var pawn = self._pawnState.GetPawnByEntity(ev.Entity);
            if (pawn == null)
            {
                self._logger.LogNullDebug(nameof(ev.Entity));
                return;
            }

            var events = BUS_EventCollectionCS.Get(pawn);
            if (events == null)
            {
                self._logger.LogError("Failed to get event collection for unit {Unit}", pawn.GetName());
                return;
            }

            self._logger.LogDebug("OnUnitDead for unit {Unit}", pawn.GetName());
            events.Evt_UnitDead.Invoke(GameUtils.GetControlledPawn(), ev.DeadReason, ev.DmgId, ev.StiffLevel, null, default, ev.IsDotDmg, ev.AbnormalType);
        }, this);

        mappedEvent.RegisterGameEventHandler<WaitingForSequenceEvent, WukongClientGameEvents>(static (ev, self) =>
        {
            if (self._playerState.LocalMainCharacter is not { } mainEntity)
            {
                Logging.LogError("Local player not found");
                return;
            }

            CutsceneUtils.SetJoiningCutsceneStatus(mainEntity, self._widgetManager, ev);

            if (mainEntity.Pawn == null)
                return;

            if (mainEntity.GetHp().IsDead)
            {
                PlayerUtils.DisableSpectator(mainEntity);
                PlayerUtils.RebirthPlayerInPlace(mainEntity.Pawn);
                CutsceneUtils.TeleportLocalPlayerToCutsceneLocation(mainEntity);
            }
        }, this);

        mappedEvent.RegisterGameEventHandler<IronBodyStartEvent, WukongClientGameEvents>(static (ev, self) =>
        {
            var mainEntity = new MainCharacterEntity(ev.Entity);

            if (mainEntity.Pawn == null)
            {
                self._logger.LogError("Pawn is null for main character: {Entity}", ev.Entity.GetNetId());
                return;
            }

            IronBodyUtils.TriggerIronBody(mainEntity.Pawn);
        }, this);

        mappedEvent.RegisterGameEventHandler<UnitSpawnedEvent, WukongClientGameEvents>(static (ev, self) =>
        {
            self._logger.LogDebug("OnUnitSpawned called for player {PlayerId} with entity: {Entity}", ev.PlayerId, ev.Entity.GetNetId());

            var playerId = ev.PlayerId != default ? ev.PlayerId : self._playerState.LocalPlayerId;

            if (playerId == null || self._playerState.GetMainCharacterByPlayerId(playerId.Value) == null)
            {
                self._logger.LogError("Player not found: {PlayerId}", playerId);
                return;
            }

            TamerUtils.AddSpawnedUnitRefCount(new TamerEntity(ev.Entity), playerId.Value);
        }, this);

        mappedEvent.RegisterGameEventHandler<UnitDespawnedEvent, WukongClientGameEvents>(static (ev, self) =>
        {
            self._logger.LogDebug("OnUnitDespawn called for player {PlayerId} with entity {Entity}", ev.PlayerId, ev.Entity.GetNetId());

            var playerId = ev.PlayerId != default ? ev.PlayerId : self._playerState.LocalPlayerId;

            if (playerId == null || self._playerState.GetMainCharacterByPlayerId(playerId.Value) == null)
            {
                self._logger.LogError("Player not found: {PlayerId}", playerId);
                return;
            }

            TamerUtils.SubtractSpawnedUnitRefCount(new TamerEntity(ev.Entity), playerId.Value);
        }, this);

        mappedEvent.RegisterGameEventHandler<TamerSkillInteractEvent, WukongClientGameEvents>(static (ev, self) => { TamerUtils.TriggerSkillInteract(ev.Entity, ev.SkillId); }, this);

        mappedEvent.RegisterGameEventHandler<TriggerMagicallyChangeEvent, WukongClientGameEvents>(static (ev, self) =>
        {
            var mainEntity = new MainCharacterEntity(ev.Entity);
            ref var mainComp = ref mainEntity.GetState();

            if (mainEntity.Pawn == null)
            {
                self._logger.LogError("Pawn is null for main character {Entity}", ev.Entity.GetNetId());
                return;
            }

            self._logger.LogDebug("Received trigger magically change for character {Nickname} with config {ConfigAssetPath}, skillID {SkillID}, recoverSkillID {RecoverSkillID}, curVigorSkillID {CurVigorSkillID}", mainComp.CharacterNickname, ev.ConfigPathName, ev.SkillId, ev.RecoverSkillId, ev.CurVigorSkillId);
            MagicallyChangeUtils.TriggerMagicallyChange(mainEntity.Pawn, ev.ConfigPathName, ev.SkillId, ev.RecoverSkillId, ev.CurVigorSkillId, ev.CastReason);
        }, this);

        mappedEvent.RegisterGameEventHandler<ResetMagicallyChangeEvent, WukongClientGameEvents>(static (ev, self) =>
        {
            var mainEntity = new MainCharacterEntity(ev.Entity);
            ref var mainComp = ref mainEntity.GetState();

            if (mainEntity.Pawn == null)
            {
                self._logger.LogError("Pawn is null for main character: {Entity}", ev.Entity.GetNetId());
                return;
            }

            self._logger.LogDebug("Received reset magically change for character {Nickname} with reason {Reason}", mainComp.CharacterNickname, ev.Reason);
            MagicallyChangeUtils.ResetMagicallyChange(mainEntity.Pawn, ev.Reason);
        }, this);

        mappedEvent.RegisterGameEventHandler<ProjectileTargetEvent, WukongClientGameEvents>(static (ev, self) =>
        {
            var mainEntity = new MainCharacterEntity(ev.Character);

            if (mainEntity.Pawn == null)
            {
                self._logger.LogError("Pawn is null for character main character: {Entity}", ev.Character.GetNetId());
                return;
            }

            var target = self._pawnState.GetPawnByEntity(ev.Target);
            if (target == null)
            {
                self._logger.LogNull(nameof(ev.Target));
                return;
            }

            ProjectileUtils.SetProjectileTarget(mainEntity.Pawn, ev.ProjectileName, target, ev.SocketName);
        }, this);

        mappedEvent.RegisterGameEventHandler<ProjectileSwitchEvent, WukongClientGameEvents>(static (ev, self) =>
        {
            var mainEntity = new MainCharacterEntity(ev.Entity);

            if (mainEntity.Pawn == null)
            {
                self._logger.LogError("Pawn is null for main character: {Entity}", ev.Entity.GetNetId());
                return;
            }

            ProjectileUtils.SwitchProjectileInfo(mainEntity.Pawn, ev.ProjectileClassName, ev.BulletSwitchId, ev.SwitchIdx);
        }, this);

        mappedEvent.RegisterGameEventHandler<ProjectileDeadEvent, WukongClientGameEvents>(static (ev, self) =>
        {
            var mainEntity = new MainCharacterEntity(ev.Entity);

            if (mainEntity.Pawn == null)
            {
                self._logger.LogError("Pawn is null for main character: {Entity}", ev.Entity.GetNetId());
                return;
            }

            ProjectileUtils.DestroyProjectile(mainEntity.Pawn, ev.ProjectileClassName, ev.Reason);
        }, this);

        mappedEvent.RegisterGameEventHandler<MagicFieldDeadEvent, WukongClientGameEvents>(static (ev, self) => { MagicFieldUtils.DestroyMagicField(ev.ClassName, ev.Reason); }, this);

        mappedEvent.RegisterGameEventHandler<ProjectileMoveModeEvent, WukongClientGameEvents>(static (ev, self) =>
        {
            var mainEntity = new MainCharacterEntity(ev.Entity);

            if (mainEntity.Pawn == null)
            {
                self._logger.LogError("Pawn is null for main character: {Entity}", ev.Entity.GetNetId());
                return;
            }

            ProjectileUtils.SetProjectileMode(mainEntity.Pawn, ev.ProjectileClassName, ev.MoveMode);
        }, this);

        mappedEvent.RegisterGameEventHandler<PartyRespawnEvent, WukongClientGameEvents>(static (ev, self) =>
        {
            if (self._playerState.LocalMainCharacter is not { } mainEntity)
                return;

            ref var localMainComp = ref mainEntity.GetLocalState();
            if (mainEntity.Pawn == null)
                return;

            localMainComp.IsRespawning = true;
            PlayerUtils.DisableSpectator(mainEntity);
            CutsceneUtils.ClearLocalJoiningCutsceneStatus(mainEntity);
            self._eventRouter.RaiseOnLocalPlayerBeforeRebirth();
            PlayerUtils.RebirthDeadPlayer(mainEntity.Pawn, ev.BirthShrineId);
        }, this);

        mappedEvent.RegisterGameEventHandler<AfterRebirthEvent>(static ev =>
        {
            var mainEntity = new MainCharacterEntity(ev.Entity);

            var playerPawn = mainEntity.Pawn;
            var events = BUS_EventCollectionCS.Get(playerPawn);
            if (events != null)
            {
                events.Evt_AfterUnitRebirth.Invoke(ERebirthType.RebirthPoint);
            }
        });

        mappedEvent.RegisterGameEventHandler<RestAtShrineEvent, WukongClientGameEvents>(static (ev, self) =>
        {
            foreach (var player in self._state.AreaPlayers)
            {
                var playerEntity = self._playerState.GetMainCharacterByPlayerId(player);

                if (playerEntity is not { } mainEntity)
                    continue;

                ref var localMainComp = ref mainEntity.GetLocalState();
                if (mainEntity.Pawn == null)
                    continue;

                if (mainEntity.GetHp().IsDead)
                {
                    localMainComp.IsRespawning = true;
                    PlayerUtils.DisableSpectator(mainEntity);
                    PlayerUtils.RebirthDeadPlayer(mainEntity.Pawn, ev.RebirthPointId);
                }
                else
                {
                    PlayerUtils.RestPlayer(mainEntity.Pawn);
                }
            }
        }, this);

        mappedEvent.RegisterGameEventHandler<PartySoftlockEvent, WukongClientGameEvents>(static (ev, self) =>
        {
            if (self._playerState.LocalMainCharacter is not { } mainEntity)
                return;

            ref var localMainComp = ref mainEntity.GetLocalState();
            if (mainEntity.Pawn == null)
                return;

            localMainComp.IsRespawning = true;
            PlayerUtils.DisableSpectator(mainEntity);
            CutsceneUtils.ClearLocalJoiningCutsceneStatus(mainEntity);
            self._eventRouter.RaiseOnLocalPlayerBeforeRebirth();
            PlayerUtils.RebirthAlivePlayer(mainEntity.Pawn, ev.BirthPointId);
        }, this);

        mappedEvent.RegisterGameEventHandler<StartJumpEvent, WukongClientGameEvents>(static (ev, self) =>
        {
            var mainEntity = new MainCharacterEntity(ev.Entity);

            if (mainEntity.Pawn == null)
            {
                self._logger.LogError("Pawn is null for main character: {Entity}", ev.Entity.GetNetId());
                return;
            }

            PlayerUtils.StartJump(mainEntity.Pawn, ev.StartJumpDir, ev.InputVector);
        }, this);

        mappedEvent.RegisterGameEventHandler<StopJumpEvent, WukongClientGameEvents>(static (ev, self) =>
        {
            var mainEntity = new MainCharacterEntity(ev.Entity);

            if (mainEntity.Pawn == null)
            {
                self._logger.LogError("Pawn is null for main character: {Entity}", ev.Entity.GetNetId());
                return;
            }

            PlayerUtils.StopJump(mainEntity.Pawn);
        }, this);

        mappedEvent.RegisterGameEventHandler<MonsterWakeUpEvent, WukongClientGameEvents>(static (ev, self) =>
        {
            var pawn = self._pawnState.GetPawnByEntity(ev.Entity);
            if (pawn == null)
            {
                self._logger.LogNullDebug(nameof(ev.Entity));
                return;
            }

            var guid = BGU_DataUtil.GetActorGuid(pawn);
            Logging.LogDebug("OnMonsterWakeup called for monster {Guid}", guid);

            TamerUtils.TriggerWakeUp(pawn);
        }, this);

        mappedEvent.RegisterGameEventHandler<PlayBaneEffectEvent, WukongClientGameEvents>(static (ev, self) =>
        {
            var pawn = self._pawnState.GetPawnByEntity(ev.Entity);
            if (pawn == null)
            {
                self._logger.LogNullDebug(nameof(ev.Entity));
                return;
            }

            var handlers = BGU_DataUtil.GetUnPersistentReadOnlyData<BUC_AbnormalStateHandlers>(pawn);

            if (handlers == null)
                return;

            var handler = handlers.GetAbnormalHanddler(ev.StateType);
            PlayDBC_ByType.Invoke(handler, [ev.ActionType, default(FTransform), -1]);
        }, this);

        mappedEvent.RegisterGameEventHandler<StopBaneEffectEvent, WukongClientGameEvents>(static (ev, self) =>
        {
            var pawn = self._pawnState.GetPawnByEntity(ev.Entity);
            if (pawn == null)
            {
                self._logger.LogNullDebug(nameof(ev.Entity));
                return;
            }

            var handlers = BGU_DataUtil.GetUnPersistentReadOnlyData<BUC_AbnormalStateHandlers>(pawn);
            if (handlers == null)
                return;

            var handler = handlers.GetAbnormalHanddler(ev.StateType);
            EndAllDBC.Invoke(handler, []);
        }, this);

        mappedEvent.RegisterGameEventHandler<CastSkillEvent, WukongClientGameEvents>(static (ev, self) =>
        {
            if (self._pawnState.GetPawnByEntity(ev.Entity) is not { } casterPawn)
            {
                self._logger.LogError("Caster pawn not found: {Entity}", ev.Entity.GetNetId());
                return;
            }

            Logging.LogDebug("OnCastSkill called for caster {Caster} with skillId {SkillId} and skillType {SkillType}", BGU_DataUtil.GetActorGuid(casterPawn), ev.SkillId, ev.SkillType);
            BUS_EventCollectionCS.Get(casterPawn)?.Evt_UnitCastSkillTry.Invoke(new FCastSkillInfo(ev.SkillId, ev.SkillType));
        }, this);
    }

    public void Dispose()
    {
        // FIXME(api): Unregister game events
    }
}