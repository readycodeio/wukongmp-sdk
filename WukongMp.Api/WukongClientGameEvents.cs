using System;
using System.Reflection;
using b1;
using b1.BGW;
using HarmonyLib;
using Microsoft.Extensions.Logging;
using ReadyM.Api.Mapping.Events;
using ReadyM.Relay.Client.State;
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

public class WukongClientGameEvents : IDisposable
{
    // ReSharper disable once InconsistentNaming
    private static readonly MethodInfo PlayDBC_ByType = AccessTools.Method(typeof(BGU_AbnormalStateHandlerBase), "PlayDBC_ByType");

    // ReSharper disable once InconsistentNaming
    private static readonly MethodInfo EndAllDBC = AccessTools.Method(typeof(BGU_AbnormalStateHandlerBase), "EndAllDBC");

    private readonly IMappedEventManager _mappedEvent;
    private readonly WukongMappingPolicyDirectory _policyDir;
    private readonly ClientState _state;
    private readonly WukongPawnState _pawnState;
    private readonly WukongPlayerState _playerState;
    private readonly WukongWidgetManager _widgetManager;
    private readonly GameplayEventRouter _eventRouter;
    private readonly ILogger _logger;

    public WukongClientGameEvents(
        IMappedEventManager mappedEvent,
        WukongMappingPolicyDirectory policyDir,
        ClientState state,
        WukongPawnState pawnState,
        WukongPlayerState playerState,
        WukongWidgetManager widgetManager,
        GameplayEventRouter eventRouter,
        ILogger logger)
    {
        _mappedEvent = mappedEvent;
        _policyDir = policyDir;
        _state = state;
        _pawnState = pawnState;
        _playerState = playerState;
        _widgetManager = widgetManager;
        _eventRouter = eventRouter;
        _logger = logger;

        _mappedEvent.RegisterGameEventHandler<ExitPhantomRushEvent, WukongClientGameEvents>(static (ev, self) =>
        {
            var mainEntity = new MainCharacterEntity(ev.Entity);
            ref var mainComp = ref mainEntity.GetState();
            self._logger.LogDebug("Received exit phantom rush for main character {Entity} and {Nickname}", mainEntity, mainComp.CharacterNickName);
            var events = BUS_EventCollectionCS.Get(mainEntity.Pawn);
            events?.Evt_RelievePhantomRush.Invoke();
        }, this);

        _mappedEvent.RegisterGameEventHandler<AddBuffEvent, WukongClientGameEvents>(static (ev, self) =>
        {
            var pawn = self._pawnState.GetPawnByEntity(ev.Entity);

            if (pawn == null)
                return;

            BuffUtils.AddBuff(pawn, ev.BuffId, ev.Duration);
        }, this);

        _mappedEvent.RegisterGameEventHandler<RemoveBuffEvent, WukongClientGameEvents>(static (ev, self) =>
        {
            var pawn = self._pawnState.GetPawnByEntity(ev.Entity);

            if (pawn == null)
                return;

            BuffUtils.RemoveBuff(pawn, ev.BuffId, ev.TriggerType, ev.Layer, ev.WithTriggerRemoveEffect);
        }, this);

        _mappedEvent.RegisterGameEventHandler<RemoveAllBuffsEvent, WukongClientGameEvents>(static (ev, self) =>
        {
            var pawn = self._pawnState.GetPawnByEntity(ev.Entity);

            if (pawn == null)
                return;

            BuffUtils.RemoveAllBuffs(pawn, ev.TriggerType, ev.WithTriggerRemoveEffect);
        }, this);

        _mappedEvent.RegisterGameEventHandler<UnitStateTriggerEvent, WukongClientGameEvents>(static (ev, self) =>
        {
            var character = self._pawnState.GetPawnByEntity(ev.Entity);
            NpcLocomotionUtils.SetStateTrigger(character, ev.Trigger, ev.Time, ev.NeedForceUpdate);
        }, this);

        _mappedEvent.RegisterGameEventHandler<UnitSimpleStateEvent, WukongClientGameEvents>(static (ev, self) =>
        {
            var character = self._pawnState.GetPawnByEntity(ev.Entity);
            NpcLocomotionUtils.SetSimpleState(character, ev.SimpleState, ev.IsRemove);
        }, this);

        _mappedEvent.RegisterGameEventHandler<TriggerFsmStateEvent, WukongClientGameEvents>(static (ev, self) =>
        {
            var character = self._pawnState.GetPawnByEntity(ev.Entity);
            NpcLocomotionUtils.SetFsmState(character, ev.FsmStateName);
        }, this);

        _mappedEvent.RegisterGameEventHandler<MotionMatchingStateEvent, WukongClientGameEvents>(static (ev, self) =>
        {
            var character = self._pawnState.GetPawnByEntity(ev.Entity);
            NpcLocomotionUtils.SetMotionMatchingState(character, ev.State);
        }, this);

        _mappedEvent.RegisterGameEventHandler<SpawnSummonEvent, WukongClientGameEvents>(static (ev, self) =>
        {
            self._logger.LogDebug("Received OnSpawnSummon for summoner {Summoner} with guid {Guid} for tamer path {Path}", ev.Summoner, ev.SummonGuid, ev.SummonClassPath);
            SpawningUtils.SpawnSummonedUnitWithGuid(ev.ToGame(self._pawnState));
        }, this);

        _mappedEvent.RegisterGameEventHandler<RequestSpawnUnitsEvent, WukongClientGameEvents>(static (ev, self) => { SpawningUtils.SpawnUnitsAsOwner(self._playerState, self._pawnState, self._policyDir, new TamerKind(ev.UnitName), ev.Count, ev.TeamId, ev.Location); }, this);

        _mappedEvent.RegisterGameEventHandler<BroadcastUnitSpawnEvent>(static ev => { SpawningUtils.SpawnUnitLocallyByName(ev.Guid, new TamerKind(ev.UnitName), ev.Location); });

        _mappedEvent.RegisterGameEventHandler<PlayerTransBeginEvent, WukongClientGameEvents>(static (ev, self) =>
        {
            var mainEntity = new MainCharacterEntity(ev.Entity);
            TransformationUtils.TransformPlayer(mainEntity, ev.UnitResId, ev.UnitBornSkillId, ev.EnableBlendViewTarget, ev.TransBeginType);
        }, this);

        _mappedEvent.RegisterGameEventHandler<PlayerTransEndEvent, WukongClientGameEvents>(static (ev, self) =>
        {
            var mainEntity = new MainCharacterEntity(ev.Entity);
            TransformationUtils.TransformPlayerBack(mainEntity, ev.UnitResId, ev.UnitBornSkillId, ev.EnableBlendViewTarget, ev.TransEndType);
        }, this);

        _mappedEvent.RegisterGameEventHandler<PlayMovieRequestEvent>(CutsceneUtils.PlayCutscene);

        _mappedEvent.RegisterGameEventHandler<SetTargetEvent, WukongClientGameEvents>(static (ev, self) =>
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

        _mappedEvent.RegisterGameEventHandler<CastImmobilizeEvent, WukongClientGameEvents>(static (ev, self) =>
        {
            var character = self._pawnState.GetPawnByEntity(ev.Caster);
            if (character == null)
            {
                self._logger.LogNull(nameof(ev.Caster));
                return;
            }

            ImmobilizeUtils.CastImmobilize(character);
        }, this);

        _mappedEvent.RegisterGameEventHandler<TriggerImmobilizeEvent, WukongClientGameEvents>(static (ev, self) =>
        {
            var caster = self._pawnState.GetPawnByEntity(ev.Entity);
            var target = self._pawnState.GetPawnByEntity(ev.Target);
            ImmobilizeUtils.TriggerImmobilize(caster, target, ev.GreatSageTalentActiveBuff);
        }, this);

        _mappedEvent.RegisterGameEventHandler<RelieveImmobilizeEvent, WukongClientGameEvents>(static (ev, self) =>
        {
            var character = self._pawnState.GetPawnByEntity(ev.Affected);
            if (character == null)
            {
                self._logger.LogNullDebug(nameof(ev.Affected));
                return;
            }

            ImmobilizeUtils.RelieveImmobilize(self._pawnState, character);
        }, this);

        _mappedEvent.RegisterGameEventHandler<PhantomRushEvent, WukongClientGameEvents>(static (ev, self) =>
        {
            var mainEntity = new MainCharacterEntity(ev.Entity);

            if (mainEntity.Pawn == null)
            {
                self._logger.LogError("Main character not found: {Entity}", ev.Entity);
                return;
            }

            ref var mainComp = ref mainEntity.GetState();
            self._logger.LogDebug("Received phantom rush for main character {Entity} and {Nickname} in direction {Direction}", ev.Entity, mainComp.CharacterNickName, ev.Direction);
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

        _mappedEvent.RegisterGameEventHandler<BroadcastPlayerTransformEvent, WukongClientGameEvents>(static (ev, self) =>
        {
            var mainEntity = new MainCharacterEntity(ev.Entity);

            PlayerUtils.TeleportLocalPlayer(mainEntity, ev.Location, ev.Rotation, true);
        }, this);

        _mappedEvent.RegisterGameEventHandler<RebirthPlayerEvent, WukongClientGameEvents>(static (ev, self) =>
        {
            self._logger.LogDebug("RebirthPlayer for main character {Entity} called", ev.Entity);

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

        _mappedEvent.RegisterGameEventHandler<DamageNumEvent>(static ev =>
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

        _mappedEvent.RegisterGameEventHandler<TeleportFinishEvent>(static ev =>
        {
            var mainEntity = new MainCharacterEntity(ev.Entity);
            var events = BUS_EventCollectionCS.Get(mainEntity.Pawn);
            events?.Evt_UnitStateTrigger.Invoke(EBUStateTrigger.TeleportEnd, -1f);
            events?.Evt_TeleportFinish.Invoke();
        });

        _mappedEvent.RegisterGameEventHandler<MontageCallbackEvent, WukongClientGameEvents>(static (ev, self) =>
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
                    self._logger.LogDebug("Received montage cancel at {Time} for entity {Entity} - {Montage}", time, ev.Entity, current.PathName);
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

        _mappedEvent.RegisterGameEventHandler<AnimationSyncingEvent, WukongClientGameEvents>((ev, self) =>
        {
            self._logger.LogDebug("OnPreAnimationSyncing called for Host {Host} and Guest {Guest}", ev.Host, ev.Guest);

            var hostPawn = self._pawnState.GetPawnByEntity(ev.Host);
            if (hostPawn == null)
            {
                self._logger.LogNullDebug(nameof(ev.Host));
                return;
            }

            var guestPawn = self._pawnState.GetPawnByEntity(ev.Guest);
            if (guestPawn == null)
            {
                self._logger.LogNullDebug(nameof(ev.Guest));
                return;
            }

            BUS_EventCollectionCS.Get(hostPawn)?.Evt_NotifyEnterPreAnimationSyncingStateOnHost?.Invoke(guestPawn, []);
            BUS_EventCollectionCS.Get(guestPawn)?.Evt_NotifyEnterPreAnimationSyncingStateOnGuest?.Invoke(hostPawn, []);

            var montage = string.IsNullOrEmpty(ev.FullMontagePath) ? null : BGW_PreloadAssetMgr.Get(GameUtils.GetWorld()).TryGetCachedResourceObj<UAnimMontage>(ev.FullMontagePath, ELoadResourceType.SyncLoadAndCache);

            BUS_EventCollectionCS.Get(hostPawn)?.Evt_NotifyEnterAnimationSyncingStateOnHost?.Invoke([], montage);
            BUS_EventCollectionCS.Get(guestPawn)?.Evt_NotifyEnterAnimationSyncingStateOnGuest?.Invoke([]);

            var data = BGU_DataUtil.GetReadOnlyData<BGC_AnimationSyncData>(UGameplayStatics.GetGameState(GameUtils.GetWorld()));
            data?.AddParticipants(hostPawn, guestPawn);
        }, this);

        _mappedEvent.RegisterGameEventHandler<BeginSyncAnimationEvent, WukongClientGameEvents>(static (ev, self) =>
        {
            self._logger.LogDebug("OnBeginSyncAnimation called for Host {Entity} with GuestMontage '{MontagePath}'", ev.Host, ev.FullGuestMontage);

            var hostPawn = self._pawnState.GetPawnByEntity(ev.Host);
            if (hostPawn == null)
            {
                self._logger.LogNullDebug(nameof(ev.Host));
                return;
            }

            var montage = string.IsNullOrEmpty(ev.FullGuestMontage) ? null : BGW_PreloadAssetMgr.Get(GameUtils.GetWorld()).TryGetCachedResourceObj<UAnimMontage>(ev.FullGuestMontage, ELoadResourceType.SyncLoadAndCache);

            var events = BGS_GSEventCollection.Get(hostPawn);
            if (events == null)
            {
                self._logger.LogError("Failed to get event collection for unit {Unit}", hostPawn.GetName());
                return;
            }

            events.Evt_BGS_BeginSyncAnimation?.Invoke(hostPawn, montage, ev.FoundHostSyncPointOnDummyMesh, new FName(ev.SelfSyncPointOnHost), new FName(ev.TargetSyncPointOnHost), new FName(ev.SelfSyncPointOnGuest), ev.ForceSyncDummyMeshAnimation, ev.EnableDebugDraw, ev.NotifyBeginTime, ev.TotalDuration, ev.AnimationSyncMontageInstanceId);
        }, this);

        _mappedEvent.RegisterGameEventHandler<UnitDeadEvent, WukongClientGameEvents>(static (ev, self) =>
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

        _mappedEvent.RegisterGameEventHandler<WaitingForSequenceEvent, WukongClientGameEvents>(static (ev, self) =>
        {
            if (self._playerState.LocalMainCharacter is not { } mainEntity)
            {
                Logging.LogError("Local player not found");
                return;
            }

            CutsceneUtils.SetJoiningCutsceneStatus(mainEntity, self._widgetManager, ev);

            if (mainEntity.Pawn == null)
                return;

            if (mainEntity.GetState().IsDead)
            {
                PlayerUtils.DisableSpectator(mainEntity);
                PlayerUtils.RebirthPlayerInPlace(mainEntity.Pawn);
                CutsceneUtils.TeleportLocalPlayerToCutsceneLocation(mainEntity);
            }
        }, this);

        _mappedEvent.RegisterGameEventHandler<IronBodyStartEvent, WukongClientGameEvents>(static (ev, self) =>
        {
            var mainEntity = new MainCharacterEntity(ev.Entity);

            if (mainEntity.Pawn == null)
            {
                self._logger.LogError("Pawn is null for main character: {Entity}", ev.Entity);
                return;
            }

            IronBodyUtils.TriggerIronBody(mainEntity.Pawn);
        }, this);

        _mappedEvent.RegisterGameEventHandler<UnitSpawnedEvent, WukongClientGameEvents>(static (ev, self) =>
        {
            self._logger.LogDebug("OnUnitSpawned called for player {PlayerId} with entity: {Entity}", ev.PlayerId, ev.Entity);

            var playerId = ev.PlayerId != default ? ev.PlayerId : self._playerState.LocalPlayerId;

            if (playerId == null || self._playerState.GetMainCharacterByPlayerId(playerId.Value) == null)
            {
                self._logger.LogError("Player not found: {PlayerId}", playerId);
                return;
            }

            TamerUtils.AddSpawnedUnitRefCount(new TamerEntity(ev.Entity), playerId.Value);
        }, this);

        _mappedEvent.RegisterGameEventHandler<UnitDespawnedEvent, WukongClientGameEvents>(static (ev, self) =>
        {
            self._logger.LogDebug("OnUnitDespawn called for player {PlayerId} with entity {Entity}", ev.PlayerId, ev.Entity);

            var playerId = ev.PlayerId != default ? ev.PlayerId : self._playerState.LocalPlayerId;

            if (playerId == null || self._playerState.GetMainCharacterByPlayerId(playerId.Value) == null)
            {
                self._logger.LogError("Player not found: {PlayerId}", playerId);
                return;
            }

            TamerUtils.SubtractSpawnedUnitRefCount(new TamerEntity(ev.Entity), playerId.Value);
        }, this);

        _mappedEvent.RegisterGameEventHandler<TamerSkillInteractEvent, WukongClientGameEvents>(static (ev, self) => { TamerUtils.TriggerSkillInteract(ev.Entity, ev.SkillId); }, this);

        _mappedEvent.RegisterGameEventHandler<TriggerMagicallyChangeEvent, WukongClientGameEvents>(static (ev, self) =>
        {
            var mainEntity = new MainCharacterEntity(ev.Entity);
            ref var mainComp = ref mainEntity.GetState();

            if (mainEntity.Pawn == null)
            {
                self._logger.LogError("Pawn is null for main character {Entity}", ev.Entity);
                return;
            }

            self._logger.LogDebug("Received trigger magically change for character {Nickname} with config {ConfigAssetPath}, skillID {SkillID}, recoverSkillID {RecoverSkillID}, curVigorSkillID {CurVigorSkillID}", mainComp.CharacterNickName, ev.ConfigPathName, ev.SkillId, ev.RecoverSkillId, ev.CurVigorSkillId);
            MagicallyChangeUtils.TriggerMagicallyChange(mainEntity.Pawn, ev.ConfigPathName, ev.SkillId, ev.RecoverSkillId, ev.CurVigorSkillId, ev.CastReason);
        }, this);

        _mappedEvent.RegisterGameEventHandler<ResetMagicallyChangeEvent, WukongClientGameEvents>(static (ev, self) =>
        {
            var mainEntity = new MainCharacterEntity(ev.Entity);
            ref var mainComp = ref mainEntity.GetState();

            if (mainEntity.Pawn == null)
            {
                self._logger.LogError("Pawn is null for main character: {Entity}", ev.Entity);
                return;
            }

            self._logger.LogDebug("Received reset magically change for character {Nickname} with reason {Reason}", mainComp.CharacterNickName, ev.Reason);
            MagicallyChangeUtils.ResetMagicallyChange(mainEntity.Pawn, ev.Reason);
        }, this);

        _mappedEvent.RegisterGameEventHandler<ProjectileTargetEvent, WukongClientGameEvents>(static (ev, self) =>
        {
            var mainEntity = new MainCharacterEntity(ev.Character);

            if (mainEntity.Pawn == null)
            {
                self._logger.LogError("Pawn is null for character main character: {Entity}", ev.Character);
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

        _mappedEvent.RegisterGameEventHandler<ProjectileSwitchEvent, WukongClientGameEvents>(static (ev, self) =>
        {
            var mainEntity = new MainCharacterEntity(ev.Entity);

            if (mainEntity.Pawn == null)
            {
                self._logger.LogError("Pawn is null for main character: {Entity}", ev.Entity);
                return;
            }

            ProjectileUtils.SwitchProjectileInfo(mainEntity.Pawn, ev.ProjectileClassName, ev.BulletSwitchId, ev.SwitchIdx);
        }, this);

        _mappedEvent.RegisterGameEventHandler<ProjectileDeadEvent, WukongClientGameEvents>(static (ev, self) =>
        {
            var mainEntity = new MainCharacterEntity(ev.Entity);

            if (mainEntity.Pawn == null)
            {
                self._logger.LogError("Pawn is null for main character: {Entity}", ev.Entity);
                return;
            }

            ProjectileUtils.DestroyProjectile(mainEntity.Pawn, ev.ProjectileClassName, ev.Reason);
        }, this);

        _mappedEvent.RegisterGameEventHandler<MagicFieldDeadEvent, WukongClientGameEvents>(static (ev, self) => { MagicFieldUtils.DestroyMagicField(ev.ClassName, ev.Reason); }, this);

        _mappedEvent.RegisterGameEventHandler<ProjectileMoveModeEvent, WukongClientGameEvents>(static (ev, self) =>
        {
            var mainEntity = new MainCharacterEntity(ev.Entity);

            if (mainEntity.Pawn == null)
            {
                self._logger.LogError("Pawn is null for main character: {Entity}", ev.Entity);
                return;
            }

            ProjectileUtils.SetProjectileMode(mainEntity.Pawn, ev.ProjectileClassName, ev.MoveMode);
        }, this);

        _mappedEvent.RegisterGameEventHandler<PartyRespawnEvent, WukongClientGameEvents>(static (ev, self) =>
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

        _mappedEvent.RegisterGameEventHandler<AfterRebirthEvent>(static ev =>
        {
            var mainEntity = new MainCharacterEntity(ev.Entity);

            var playerPawn = mainEntity.Pawn;
            var events = BUS_EventCollectionCS.Get(playerPawn);
            if (events != null)
            {
                events.Evt_AfterUnitRebirth.Invoke(ERebirthType.RebirthPoint);
            }
        });

        _mappedEvent.RegisterGameEventHandler<RestAtShrineEvent, WukongClientGameEvents>(static (ev, self) =>
        {
            foreach (var player in self._state.AreaPlayers)
            {
                var playerEntity = self._playerState.GetMainCharacterByPlayerId(player);

                if (playerEntity is not { } mainEntity)
                    continue;

                ref var localMainComp = ref mainEntity.GetLocalState();
                if (mainEntity.Pawn == null)
                    continue;

                if (mainEntity.GetState().IsDead)
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

        _mappedEvent.RegisterGameEventHandler<PartySoftlockEvent, WukongClientGameEvents>(static (ev, self) =>
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

        _mappedEvent.RegisterGameEventHandler<StartJumpEvent, WukongClientGameEvents>(static (ev, self) =>
        {
            var mainEntity = new MainCharacterEntity(ev.Entity);

            if (mainEntity.Pawn == null)
            {
                self._logger.LogError("Pawn is null for main character: {Entity}", ev.Entity);
                return;
            }

            PlayerUtils.StartJump(mainEntity.Pawn, ev.StartJumpDir, ev.InputVector);
        }, this);

        _mappedEvent.RegisterGameEventHandler<StopJumpEvent, WukongClientGameEvents>(static (ev, self) =>
        {
            var mainEntity = new MainCharacterEntity(ev.Entity);

            if (mainEntity.Pawn == null)
            {
                self._logger.LogError("Pawn is null for main character: {Entity}", ev.Entity);
                return;
            }

            PlayerUtils.StopJump(mainEntity.Pawn);
        }, this);

        _mappedEvent.RegisterGameEventHandler<MonsterWakeUpEvent, WukongClientGameEvents>(static (ev, self) =>
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

        _mappedEvent.RegisterGameEventHandler<PlayBaneEffectEvent, WukongClientGameEvents>(static (ev, self) =>
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

        _mappedEvent.RegisterGameEventHandler<StopBaneEffectEvent, WukongClientGameEvents>(static (ev, self) =>
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

        _mappedEvent.RegisterGameEventHandler<CastSkillEvent, WukongClientGameEvents>(static (ev, self) =>
        {
            if (self._pawnState.GetPawnByEntity(ev.Entity) is not { } casterPawn)
            {
                self._logger.LogError("Caster pawn not found: {Entity}", ev.Entity);
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