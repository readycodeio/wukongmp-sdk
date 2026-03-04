using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using b1;
using b1.BGW;
using BtlShare;
using ReadyM.Api.ECS.Worlds;
using ReadyM.Wukong.Common.ECS.Components;
using UnrealEngine.Engine;
using UnrealEngine.Runtime;
using WukongMp.Api.Configuration;
using WukongMp.Api.ECS.Components;
using WukongMp.Api.ECS.Entities;
using WukongMp.Api.ECS.GameEvents;
using WukongMp.Api.FreeCamera;
using WukongMp.Api.Mapping;
using WukongMp.Api.State;

namespace WukongMp.Api.WukongUtils;

public static class SpawningUtils
{
    public static BGUCharacterCS? SpawnCloneForPlayer(FreeCameraManager freeCameraManager, WukongPlayerState playerState, in MainCharacterEntity mainEntity)
    {
        ref var mainComp = ref mainEntity.GetState();
        var pvpComp = mainEntity.GetPvP();
        ref readonly var teamComp = ref mainEntity.GetTeam();

        var playerId = mainComp.PlayerId;

        if (mainEntity.HasUnsyncedPawn)
        {
            Logging.LogDebug("Player already exists: {Id}", playerId); // reconnection
            return null;
        }

        var localPlayerPawn = playerState.LocalMainCharacter?.Pawn;

        if (localPlayerPawn == null)
        {
            Logging.LogError("Local player pawn is null");
            return null;
        }

        var mapId = BGUFuncLibMap.GetCurLevelId(localPlayerPawn);
        var areaId = BGUFuncLibMap.GetAreaId(localPlayerPawn);
        bool isDaShengInPrologue = mapId == 13 && areaId == 0;

        var playerPawnClass = BGW_PreloadAssetMgr.Get(GameUtils.GetWorld()).TryGetCachedResourceObj<UClass>(isDaShengInPrologue ? Constants.WukongDashengClassPath : Constants.WukongClassPath, ELoadResourceType.SyncLoadAndCache);

        if (playerPawnClass == null)
        {
            Logging.LogError("Player pawn class is null");
            return null;
        }

        var oldPawn = GameUtils.GetControlledPawn();

        if (oldPawn == null)
        {
            Logging.LogError("Old pawn is null");
            return null;
        }

        var loc = mainComp.Location.ToFVector();
        var rot = mainComp.Rotation.ToFRotator();

        var @class = UClass.GetClass("BGP_AIPlayerControllerB1"); // "BGPPlayerController" works for sure

        if (@class == null)
        {
            Logging.LogError("Class is null");
            return null;
        }

        var oldController = GameUtils.GetPlayerController();
        var controllerCameraRotation = oldController!.GetControlRotation();
        var newPawn = SpawnWukong(oldController, playerPawnClass, new FTransform(rot, loc), oldPawn);

        if (newPawn == null)
        {
            Logging.LogError("Failed to spawn new pawn");
            return null;
        }

        GameUtils.PossesPawnWithViewTarget(freeCameraManager, oldController, oldPawn, newPawn, controllerCameraRotation);

        Logging.LogDebug("Assigned player {PlayerId} clone {CloneHash}", playerId, newPawn.GetEntityHash());

        var newControllerActor = GameUtils.GetWorld()?.SpawnActor(@class, ref loc, ref rot);
        if (newControllerActor != null && newControllerActor is BGP_AIPlayerControllerCS newController)
        {
            Logging.LogDebug("Spawned new controller");
            newController.Possess(newPawn);
        }

        // Reset falling timer.
        var events = BUS_EventCollectionCS.Get(newPawn);
        events.Evt_OnLeaveFalling.Invoke();
        events = BUS_EventCollectionCS.Get(oldPawn);
        events.Evt_OnLeaveFalling.Invoke();

        // get teamId
        var teamId = teamComp.TeamId;

        // get initial Hp and HpMax
        var initialHp = mainComp.Hp;
        Logging.LogDebug("Setting initial HP to {Hp}", initialHp);

        var initialHpMaxBase = mainComp.HpMaxBase;
        Logging.LogDebug("Setting initial HPMax to {HpMax}", initialHpMaxBase);

        mainEntity.SetPawn(newPawn, false);

        var attrContainer = (BUC_AttrContainer?)BGU_DataUtil.GetReadOnlyData<IBUC_AttrContainer, BUC_AttrContainer>(newPawn);
        if (attrContainer != null)
        {
            var setHpMaxBase = attrContainer.SetFloatValue(EBGUAttrFloat.HpMaxBase, initialHpMaxBase);
            var setHp = attrContainer.SetFloatValue(EBGUAttrFloat.Hp, initialHp);
            Logging.LogDebug("Set actual Hp / HpMax: {Hp} {HpMax}", setHp, setHpMaxBase);

            foreach (var attr in Constants.SyncedAttributes)
            {
                if (mainComp.Attributes.TryGetAttribute((byte)attr, out var value))
                {
                    attrContainer.SetFloatValue(attr, value);
                }
            }
        }
        else
        {
            Logging.LogError("Failed to get attribute container from player");
        }

        Logging.LogDebug("Assigning team ID {TeamId} to player", teamId);
        ClientUtils.RegisterAndSetPlayerTeam(newPawn, teamId);

        // NOTE: Nickname already set in ECS. Therefore, the following can be removed
        Logging.LogDebug("Setting initial Nickname to {Nickname}", mainComp.CharacterNickName);

        // NOTE: Player properties already set in ECS. Therefore the following can be removed
        Logging.LogDebug("Setting initial IsReadyForPvP to {IsReady}", pvpComp.IsReadyForPvP);
        Logging.LogDebug("Setting initial IsSpectator to {IsSpectator}", pvpComp.IsSpectator);

        // FIXME: (refactor) Equipment should be synced on the actor here

        // set lock distance
        FUStUnitCommDesc unitCommDesc = BGW_GameDB.GetUnitCommDesc(newPawn.GetResID());
        if (unitCommDesc != null)
        {
            unitCommDesc.CameraLockDist = 10000;
        }

        return newPawn;
    }

    public static BGUCharacterCS? SpawnWukong(ABGPPlayerController oldController, UClass pawnClass, FTransform spawnTransform, APawn oldPawn)
    {
        var newPawn = BGU_UnrealActorUtil.BGUBeginDeferredActorSpawnFromClass(oldController.World, pawnClass, spawnTransform, ESpawnActorCollisionHandlingMethod.AdjustIfPossibleButAlwaysSpawn, null) as APawn;
        oldController.Possess(newPawn);

        if (newPawn is not BGUCharacterCS newCharacter)
        {
            Logging.LogError("Failed to cast pawn to ACharacter");
            return null;
        }

        newCharacter.CapsuleComponent.SetGenerateOverlapEvents(bInGenerateOverlapEvents: false);
        newCharacter.CapsuleComponent.SetGenerateOverlapEvents(bInGenerateOverlapEvents: false);
        BGU_UnrealActorUtil.BGUFinishSpawningActorAndECSBeginPlay(oldController, newCharacter, spawnTransform);
        BPS_GSEventCollection.Get(oldController).Evt_BPS_OnControlledPawnChange.Invoke(newCharacter);
        BGS_EventCollectionCS.Get(oldController)?.Evt_NotifyPossessEntityChanged.Invoke(oldPawn.ToEntity(), newCharacter.ToEntity());
        newCharacter.CapsuleComponent.SetGenerateOverlapEvents(bInGenerateOverlapEvents: true);
        newCharacter.CapsuleComponent.SetGenerateOverlapEvents(bInGenerateOverlapEvents: true);
        UGSE_ActorFuncLib.UpdateActorOverlaps(newCharacter);
        return newCharacter;
    }


    public static FVector CalculateSpawnLocation(FVector playerLocation, FVector playerForwardVector)
    {
        var spawnLoc = playerLocation + playerForwardVector * Constants.MonsterSpawnDistance;

        var startLoc = spawnLoc + FVector.UpVector * Constants.MonsterSpawnTraceHeight / 2;
        var endLoc = spawnLoc - FVector.UpVector * Constants.MonsterSpawnTraceHeight / 2;

        // Trace vertically for spawn height.
        var hitResultSimple = new FHitResultSimple();
        var hit = BGUFuncLibSelectTargetsCS.LineTraceForHitWorldItem(GameUtils.GetWorld(), startLoc, endLoc, ref hitResultSimple);
        if (hit)
        {
            spawnLoc = hitResultSimple.HitLocation + FVector.UpVector * Constants.MonsterHalfHeight;
        }

        return spawnLoc;
    }

    public static void SpawnUnitsAsOwner(WukongPlayerState playerState, WukongPawnState pawnState, WukongMappingPolicyDirectory policyDir, TamerKind tamerKind, int count, int teamId, FVector spawnLocation)
    {
        // Spawn in a grid around center point, separated by 200 units.
        var cols = (int)Math.Ceiling(Math.Sqrt(count));
        var rows = (int)Math.Ceiling((float)count / cols);

        var startX = -((cols - 1) * Constants.MonsterSpawnSpread) / 2f;
        var startY = -((rows - 1) * Constants.MonsterSpawnSpread) / 2f;

        var placed = 0;
        for (var row = 0; row < rows; row++)
        {
            for (var col = 0; col < cols; col++)
            {
                var x = startX + col * Constants.MonsterSpawnSpread;
                var y = startY + row * Constants.MonsterSpawnSpread;
                var loc = spawnLocation + new FVector(x, y, 0);

                SpawnUnitAsOwner(playerState, pawnState, policyDir, tamerKind, loc, teamId);
                placed++;
                if (placed == count)
                    return;
            }
        }
    }

    public static void SpawnUnitAsOwner(WukongPlayerState playerState, WukongPawnState pawnState, WukongMappingPolicyDirectory policyDir, TamerKind tamerKind, FVector location, int teamId)
    {
        var guid = Guid.NewGuid().ToString();
        var tamerActor = SpawnUnitLocallyByName(guid, tamerKind, location);
        if (tamerActor != null && playerState.LocalPlayerId != null)
        {
            var unitPath = UnitPathUtils.GetUnitPathName(tamerKind);
            var tamerEntity = CreateMonsterInEcs(pawnState, guid, tamerActor, teamId, unitPath);

            ref var transComp = ref tamerEntity.GetTransform();
            transComp.Position = location.ToVector3();
            transComp.Rotation = Vector3.Zero;

            ref var nameComp = ref tamerEntity.GetNickname();
            nameComp.Nickname = "Bot";

            Logging.LogDebug("Sending spawn unit {Name} at {Location}", tamerKind, location.ToString());

            // NOTE(api): PolicyDir check always true because newly created entity is owned locally.
            if (policyDir.TamerEvent<BroadcastUnitSpawnEvent>().CanGameEventNotifyEcs(tamerEntity))
            {
                policyDir.MappedEvent.NotifyEcs(new BroadcastUnitSpawnEvent(
                    entity: tamerEntity.Entity,
                    unitName: tamerKind.Name, 
                    guid: guid, 
                    location: location 
                ));
            }
            else
            {
                Debug.Assert(false);
            }
        }
    }

    public static BUTamerActor? SpawnUnitLocallyByName(string guid, TamerKind tamerKind, FVector location)
    {
        if (!UnitPathUtils.IsValidUnitName(tamerKind))
        {
            Logging.LogError("Invalid unit name in SpawnUnitLocallyByName: {UnitName}", tamerKind.Name);
            return null;
        }

        Logging.LogDebug("Spawn unit called for {UnitName}", tamerKind.Name);
        var unitPath = UnitPathUtils.GetUnitPathName(tamerKind);

        if (string.IsNullOrEmpty(unitPath))
            return null;

        return SpawnUnitLocallyByPath(guid, unitPath, location);
    }

    public static BUTamerActor? SpawnUnitLocallyByPath(string guid, string unitPath, FVector location)
    {
        var world = GameUtils.GetWorld();

        var unitClass = BGW_PreloadAssetMgr.Get(world).TryGetCachedResourceObj<UClass>(unitPath, ELoadResourceType.SyncLoadAndCache);
        var transform = new FTransform(FRotator.ZeroRotator, location);
        var tamerActor = UBGUFunctionLibrary.BGUBeginDeferredActorSpawnFromClass(world, (TSubclassOf<AActor>)unitClass, transform, ESpawnActorCollisionHandlingMethod.AdjustIfPossibleButAlwaysSpawn, null) as BUTamerActor;
        if (tamerActor == null)
        {
            if (!unitPath.Contains("PersistentLevel"))
            {
                Logging.LogError("Could not spawn unit: {UnitPath}", unitPath);
            }

            return null;
        }

        tamerActor.MarkAsSpawnedTamer(null);
        tamerActor.ExtendConfigComp.ActorResetType = EBGUResetType.Destroy;

        tamerActor.SpawnedTamerGuid = guid;
        // Update final guid
        tamerActor.GetFinalGuid(true);

        UBGUFunctionLibrary.BGUFinishSpawningActor(tamerActor, transform);
        Logging.LogDebug("Spawned enemy: {TamerName}, with Guid {Guid}", tamerActor.GetName(), guid);

        return tamerActor;
    }

    public static TamerEntity CreateMonsterInEcs(WukongPawnState pawnState, string guid, BUTamerActor tamer, int teamId, string unitName)
    {
        Logging.LogDebug("Created monster state with team ID: {TeamId} (assigned)", teamId);

        var entity = pawnState.CreateNetworkedTamer(
            new LocalTamerComponent(),
            new TamerComponent
            {
                Guid = guid,
                UnitPath = unitName
            },
            new TeamComponent
            {
                TeamId = teamId
            },
            tamer);

        return new TamerEntity(entity);
    }

    public static BUTamerActor? BeginDeferredSummonSpawn(UWorld? world, TSubclassOf<BUTamerActor> tamerClass, FTransform transform, int summonId, bool safeClampToLand = false)
    {
        #region InlineOriginalCode
        if (world == null || tamerClass.Value == null)
        {
            return null;
        }

        BUTamerActor? tamerActor = UBGUFunctionLibrary.BGUBeginDeferredActorSpawnFromClass(world, tamerClass.Value, transform, ESpawnActorCollisionHandlingMethod.AlwaysSpawn, null) as BUTamerActor;
        if (tamerActor == null)
        {
            return null;
        }

        if (safeClampToLand)
        {
            FVector fVector = tamerActor.BGUGetActorLocation();
            float scaledCapsuleHalfHeight = tamerActor.CapsuleComponent.GetScaledCapsuleHalfHeight();
            float scaledCapsuleRadius = tamerActor.CapsuleComponent.GetScaledCapsuleRadius();
            FVector start = fVector + FVector.UpVector * scaledCapsuleHalfHeight * 2.0;
            FVector end = fVector - FVector.UpVector * scaledCapsuleHalfHeight * 2.0;
            List<AActor> list = [tamerActor];
            if (USystemLibrary.CapsuleTraceSingleByProfile(world, start, end, scaledCapsuleRadius, scaledCapsuleHalfHeight, B1GlobalFNames.Pawn, bTraceComplex: false, list, EDrawDebugTrace.None, out var OutHit, bIgnoreSelf: true, FLinearColor.Red, FLinearColor.Blue, 3f))
            {
                FVector newLocation = BGUFunctionLibraryCS.BGUGetVectorFromNetQuantizeVector(in OutHit.ImpactPoint) + FVector.UpVector * scaledCapsuleHalfHeight;
                tamerActor.BGUSetActorLocation(newLocation, bSweep: false, bTeleport: false);
            }
        }

        if (B1Global.GIsBossRushMode)
        {
            IBIC_BossRushBattleData gameInstanceReadonlyData = BGU_DataUtil.GetGameInstanceReadonlyData<IBIC_BossRushBattleData, BIC_BossRushBattleData>(world);
            if (gameInstanceReadonlyData != null && gameInstanceReadonlyData.ServantPropertyOverrideList.TryGetValue(summonId, out var value))
            {
                tamerActor.ApplyServantPropertyOverride(value);
            }
        }
        #endregion

        return tamerActor;
    }

    public static void SpawnSummonedUnitWithGuid(FServantReq servantReq)
    {
        var world = GameUtils.GetWorld();

        var tamerActor = BeginDeferredSummonSpawn(world, servantReq.TamerTemplate, servantReq.BornTransform, servantReq.SummonID, servantReq.SafeClampToLand);
        if (tamerActor == null)
        {
            Logging.LogDebug("Cannot spawn tamer {Name}", servantReq.TamerTemplate.GetName());
            return;
        }

        Logging.LogDebug("Spawned tamer {Name} with type {Type}", servantReq.TamerTemplate.GetName(), servantReq.ServantType);
        tamerActor.SpawnedTamerGuid = servantReq.ServantTamerGuid;
        tamerActor.MarkAsServant();
        BPS_EventCollectionCS.GetLocal(world).Evt_SendServantReq.Invoke(servantReq);
        UBGUFunctionLibrary.BGUFinishSpawningActor(tamerActor, servantReq.BornTransform);
    }

    [Obsolete]
    public static bool CanSummon(WukongPlayerState playerState, WukongAreaState areaState, WukongPawnState pawnState, Store world, AActor? summoner, FVector summonLocation)
    {
        var localMainEntity = playerState.LocalMainCharacter;
        if (localMainEntity == null)
        {
            return false;
        }

        var summonerEntity = pawnState.GetEntityByPlayerActor(summoner);
        if (summonerEntity.HasValue && summoner == localMainEntity.Value.Pawn)
        {
            return true; // Local player summons.
        }
        else if (summonerEntity.HasValue)
        {
            return false; // Other player summons.
        }
        else // Summoner is not a player e.g. spawn point
        {
            if (playerState.LocalPlayerId == null)
                return false;

            if (areaState.IsMasterClient)
                return true;

            var localPlayerId = playerState.LocalPlayerId.Value;
            var localPosition = localMainEntity.Value.GetState().Location;
            var squaredDistanceToSummon = FVector.DistSquared(localPosition.ToFVector(), summonLocation);
            var squaredSpawnOwnershipRadius = Constants.SpawnOwnershipRadius * Constants.SpawnOwnershipRadius;
            if (squaredDistanceToSummon > squaredSpawnOwnershipRadius)
            {
                return false; // Distant summon -> master as owner
            }

            // Check if master or another player with lower id is nearby
            bool canSummon = true;
            world.Query<MainCharacterComponent>().ForEachEntity((ref mainComp, entity) =>
            {
                if (entity == localMainEntity.Value.Entity)
                    return;

                var squaredDistance = Vector3.DistanceSquared(localPosition, mainComp.Location);
                if (squaredDistance < squaredSpawnOwnershipRadius && (areaState.MasterClientId == mainComp.PlayerId || mainComp.PlayerId.RawValue < localPlayerId.RawValue))
                {
                    canSummon = false;
                }
            });
            return canSummon;
        }
    }

    public static FVector GetCorrectedSpawnLocation(ACharacter character, FVector targetLocation)
    {
        FVector location = targetLocation;
        UCapsuleComponent capsuleComponent = character.CapsuleComponent;
        float scaledCapsuleHalfHeight = capsuleComponent.GetScaledCapsuleHalfHeight();
        float scaledCapsuleRadius = capsuleComponent.GetScaledCapsuleRadius();
        FVector start = targetLocation + FVector.UpVector * scaledCapsuleHalfHeight * 2.0;
        FVector end = targetLocation - FVector.UpVector * scaledCapsuleHalfHeight * 2.0;
        if (UGSE_TraceFuncLib.CharacterCapsuleTraceSingleByProfile(character, start, end, scaledCapsuleRadius, scaledCapsuleHalfHeight, B1GlobalFNames.Pawn, bTraceComplex: false, character, out var OutHitLocation))
        {
            location = OutHitLocation;
            location.Z += 2.4f;
        }
        return location;
    }
}