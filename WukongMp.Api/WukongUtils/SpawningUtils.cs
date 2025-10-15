using b1;
using b1.BGW;
using BtlShare;
using Friflo.Engine.ECS;
using ReadyM.Relay.Common.Wukong.ECS.Components;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using UnrealEngine.Engine;
using UnrealEngine.Runtime;
using WukongMp.Api.Configuration;
using WukongMp.Api.ECS.Components;
using WukongMp.Api.ECS.Entities;

namespace WukongMp.Api.WukongUtils;

public static class SpawningUtils
{
    public static BGUCharacterCS? SpawnCloneForPlayer(in PlayerEntity playerEntity, in MainCharacterEntity mainEntity)
    {
        ref var mainComp = ref mainEntity.GetState();
        ref var localMainComp = ref mainEntity.GetLocalState();
        ref readonly var teamComp = ref mainEntity.GetTeam();

        var playerId = mainComp.PlayerId;
        
        ref var player = ref playerEntity.GetState();
        
        if (localMainComp.HasPawn)
        {
            Logging.LogDebug("Player already exists: {Id}", playerId); // reconnection
            return null;
        }

        var playerPawnClass = BGW_PreloadAssetMgr.Get(GameUtils.GetWorld()).TryGetCachedResourceObj<UClass>(Constants.WukongClassPath, ELoadResourceType.SyncLoadAndCache);

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
        var controllerCameraRotation = oldController.GetControlRotation();
        var newPawn = SpawnWukong(oldController, playerPawnClass, new FTransform(rot, loc), oldPawn);

        if (newPawn == null)
        {
            Logging.LogError("Failed to spawn new pawn");
            return null;
        }

        GameUtils.PossesPawnWithViewTarget(oldController, oldPawn, newPawn, controllerCameraRotation);

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

        localMainComp.Pawn = newPawn;
        
        // NOTE: The actor needs to be synchronized to have the right equipment. ECS equipment shouldn't be set to 
        // actor's equipment. Therefore, the following can be removed
        // Equipment = EquipmentHelpers.GetCurrentEquipmentStateForActor(pawn);
        // Attributes = new ConcurrentDictionary<EBGUAttrFloat, float>();

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
        Logging.LogDebug("Setting initial IsReadyForPvP to {IsReady}", player.IsReadyForPvP);
        Logging.LogDebug("Setting initial IsSpectator to {IsSpectator}", player.IsSpectator);

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

        BGW_EventCollection.Get(GameUtils.GetWorld())?.Evt_RemoveActorGuid2Entity(newCharacter, BGU_DataUtil.GetActorGuid(newCharacter), newCharacter.GetResID());

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

    public static void SpawnUnitsAsOwner(string unitName, int count, int teamId, FVector spawnLocation)
    {
        // Spawn in a grid around center point, separated by 200 units.
        var cols = (int)Math.Ceiling(Math.Sqrt(count));
        var rows = (int)Math.Ceiling((float)count / cols);

        var startX = -((cols - 1) * Constants.MonsterSpawnSpread) / 2f;
        var startY = -((rows - 1) * Constants.MonsterSpawnSpread) / 2f;

        //var placed = 0;
        for (var row = 0; row < rows; row++)
        {
            for (var col = 0; col < cols; col++)
            {
                var x = startX + col * Constants.MonsterSpawnSpread;
                var y = startY + row * Constants.MonsterSpawnSpread;
                var loc = spawnLocation + new FVector(x, y, 0);

                //var localI = placed;
                //Task.Run(async () =>
                //{
                //    // wait for i * 200ms
                //    await Task.Delay(localI * Constants.MonsterSpawnDelayMs);
                //});
                SpawnUnitAsOwner(unitName, loc, teamId);
                //placed++;
            }
        }
    }

    public static void SpawnUnitAsOwner(string unitName, FVector locaction, int teamId)
    {
        var guid = Guid.NewGuid().ToString();
        var tamerActor = SpawnUnitLocally(guid, unitName, teamId, locaction);
        if (tamerActor != null)
        {
            var unitPath = UnitPathsConfig.GetUnitPath(unitName);
            var characterEntity = CreateMonsterInEcs(guid, tamerActor, teamId, unitPath);

            ref var transComp = ref characterEntity.GetTranslation();
            transComp.Position = locaction.ToVector3();
            transComp.Rotation = Vector3.Zero;

            ref var nameComp = ref characterEntity.GetNickname();
            nameComp.Nickname = "Bot";

            Logging.LogDebug("Sending spawn unit {Name} at {Location}", unitName, locaction.ToString());
            DI.Instance.Rpc.SendSpawnUnit(new DTO.UnitSpawnRequestData(unitName, guid, teamId, locaction));
        }
    }

    public static BUTamerActor? SpawnUnitLocally(string guid, string unitName, int teamId, FVector location)
    {
        if (!UnitPathsConfig.IsValidUnitName(unitName))
        {
            Logging.LogError("Invalid unit name in SpawnUnitLocally: {UnitName}", unitName);
            return null;
        }

        Logging.LogDebug("Spawn unit called for {UnitName}", unitName);
        var unitPath = UnitPathsConfig.GetUnitPath(unitName);

        if (string.IsNullOrEmpty(unitPath))
            return null;

        var world = GameUtils.GetWorld();

        var unitClass = BGW_PreloadAssetMgr.Get(world).TryGetCachedResourceObj<UClass>(unitPath, ELoadResourceType.SyncLoadAndCache);
        var transform = new FTransform(FRotator.ZeroRotator, location);
        var tamerActor = UBGUFunctionLibrary.BGUBeginDeferredActorSpawnFromClass(world, (TSubclassOf<AActor>)unitClass, transform, ESpawnActorCollisionHandlingMethod.AdjustIfPossibleButAlwaysSpawn, null) as BUTamerActor;
        if (tamerActor == null)
        {
            Logging.LogError("Could not spawn unit: {UnitPath}", unitPath);
            return null;
        }

        tamerActor.MarkAsSpawnedTamer(null);
        tamerActor.ExtendConfigComp.ActorResetType = EBGUResetType.Destroy;

        tamerActor.SpawnedTamerGuid = guid;
        // Update final guid
        tamerActor.GetFinalGuid(true);

        UBGUFunctionLibrary.BGUFinishSpawningActor(tamerActor, transform);
        Logging.LogDebug("Spawned enemy: {TamerName}, with Guid {Guid}", tamerActor.GetName(), guid);

       
        //BGS_GSEventCollection.Get(tamerActor)?.Evt_TamerBlockingSpawnImmediately.Invoke(guid);


        return tamerActor;
    }

    public static void SpawnBots(int teamId)
    {
        for (var i = 0; i < Constants.BotCount; i++)
        {
            var angle = i / (float)Constants.BotCount * 2f * FMath.PI;
            var x = FMath.Cos(angle) * Constants.PvpMonsterRadius;
            var y = FMath.Sin(angle) * Constants.PvpMonsterRadius;

            var levelData = LevelSpawnConfig.GetCurrentLevelSpawnData();
            var spawnPosition = levelData.PvpStartingLocation + new FVector(x, y, 0f);
            SpawnUnitAsOwner(CharacterKind.Monkey, spawnPosition, teamId);
        }
    }

    public static TamerEntity CreateMonsterInEcs(string guid, BUTamerActor tamer, int teamId, string unitName)
    {
        Logging.LogDebug("Created monster state with team ID: {TeamId} (assigned)", teamId);

        var entity = DI.Instance.PawnState.CreateNetworkedMonster(
            new LocalTamerComponent(tamer),
            new TamerComponent
            {
                Guid = guid,
                UnitPath = unitName
            },
            new TeamComponent
            {
                TeamId = teamId
            });

        return new TamerEntity(entity);
    }

    public static FVector AdjustSpawnLocation(ABGUCharacter? CharacterCS, FVector InTargetLocation)
    {
        // TODO: For Heart of Birthstone map adjustment resulted in falling - invisible collision. So it is disabled for now.
        if (LaunchParameters.Instance.LevelId == 0)
        {
            return InTargetLocation;
        }

        FVector result = InTargetLocation;
        if (CharacterCS == null)
        {
            return result;
        }

        UCapsuleComponent? uCapsuleComponent = CharacterCS.GetRootComponent() as UCapsuleComponent;
        if (uCapsuleComponent == null)
        {
            return result;
        }

        float scaledCapsuleHalfHeight = uCapsuleComponent.GetScaledCapsuleHalfHeight();
        float scaledCapsuleHalfHeight2 = uCapsuleComponent.GetScaledCapsuleHalfHeight();
        float num = 2.4f;
        FVector start = InTargetLocation + FVector.UpVector * scaledCapsuleHalfHeight * 2.0;
        FVector end = InTargetLocation - FVector.UpVector * scaledCapsuleHalfHeight * 2.0;
        if (UGSE_TraceFuncLib.CharacterCapsuleTraceSingleByProfile(GameUtils.GetWorld(), start, end, scaledCapsuleHalfHeight2, scaledCapsuleHalfHeight, B1GlobalFNames.Pawn, bTraceComplex: false, CharacterCS, out var OutHitLocation))
        {
            result = OutHitLocation + num + FVector.UpVector * scaledCapsuleHalfHeight;
        }

        return result;
    }

    public static BUTamerActor? BeginDeferredSummonSpawn(UWorld? world, TSubclassOf<BUTamerActor> tamerClass, FTransform transform, int summonId, bool safeClampToLand = false)
    {
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

    public static bool CanSummon(AActor summoner, FVector summonLocation)
    {
        var localCharacter = DI.Instance.PlayerState.LocalMainCharacter;
        if (localCharacter == null)
        {
            return false;
        }
        var summonerEntity = DI.Instance.PawnState.GetByEntityByPlayerPawn(summoner);
        if (summonerEntity.HasValue && summoner == localCharacter.Value.GetLocalState().Pawn)
        {
            return true; // Local player summons.
        }
        else if (summonerEntity.HasValue)
        {
            return false; // Other player summons.
        }
        else // Summoner is not a player e.g. spawn point
        {
            if (DI.Instance.PlayerState.LocalPlayerId == null)
                return false;

            if (DI.Instance.AreaState.IsMasterClient)
                return true;

            var localPlayerId = DI.Instance.PlayerState.LocalPlayerId.Value;
            var localPosition = localCharacter.Value.GetState().Location;
            var squaredDistanceToSummon = FVector.DistSquared(localPosition.ToFVector(), summonLocation);
            var squaredSpawnOwnershipRadius = Constants.SpawnOwnershipRadius * Constants.SpawnOwnershipRadius;
            if (squaredDistanceToSummon > squaredSpawnOwnershipRadius)
            {
                return false; // Distant summon -> master as owner
            }

            // Check if master or another player with lower id is nearby
            bool canSummon = true;
            DI.Instance.World.Query<MainCharacterComponent>().ForEachEntity((
            ref MainCharacterComponent playerComp, Entity entity) =>
            {
                if (entity == localCharacter.Value.Entity)
                    return;

                var squaredDistance = Vector3.DistanceSquared(localPosition, playerComp.Location);
                if (squaredDistance < squaredSpawnOwnershipRadius && (DI.Instance.AreaState.MasterClientId == playerComp.PlayerId || playerComp.PlayerId.RawValue < localPlayerId.RawValue))
                {
                    canSummon = false;
                }
            });
            return canSummon;
        }
    }

    public static void SetMonkeyBotConfig(BGUCharacterCS bGUCharacter)
    {
        var events = BUS_EventCollectionCS.Get(bGUCharacter);
        if (events != null)
        {
            foreach (var attr in MonkeyBotConfig.Attribues)
            {
                events.Evt_SetAttrFloat.Invoke(attr.Key, attr.Value);
            }

            foreach (var eq in MonkeyBotConfig.Equipment)
            {
                events.Evt_InitDaShenEquipData.Invoke(eq.Key, eq.Value);
            }
        }
    }
}