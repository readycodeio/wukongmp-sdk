using System;
using System.Linq;
using System.Threading.Tasks;
using b1;
using b1.BGW;
using BtlShare;
using Friflo.Engine.ECS;
using ReadyM.Relay.Common;
using ReadyM.Relay.Common.ECS;
using ReadyM.Relay.Common.Wukong.Components;
using UnrealEngine.Engine;
using UnrealEngine.Runtime;
using WukongMp.Api.Configuration;
using WukongMp.Api.ECS;
using WukongMp.Api.Old;
using WukongMp.Api.Old.Api;
using WukongMp.Api.Old.State;
using WukongMp.Api.Patches;

namespace WukongMp.Api.WukongUtils;

public static class SpawningUtils
{
    public static PlayerState? SpawnCloneForPlayer(PlayerId playerId)
    {
        if (DI.Instance.Players.ConnectedPlayers.ContainsKey(playerId))
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

        FVector loc = default;
        FRotator rot = default;

        var initialProps = DI.Instance.RelayClient.GetPlayerState(playerId)?.Properties;

        if (initialProps == null)
        {
            Logging.LogError("Player properties are null at player joining");
            return null;
        }

        if (initialProps.TryGetValue(nameof(PlayerState.Location), out var playerLoc))
        {
            loc = (FVector)playerLoc;
        }

        if (initialProps.TryGetValue(nameof(PlayerState.Rotation), out var playerRot))
        {
            rot = (FRotator)playerRot;
        }

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
        var teamId = newPawn.GetTeamIDInCS();
        if (initialProps.TryGetValue(nameof(PlayerState.TeamId), out var assignedTeamId))
        {
            teamId = (int)assignedTeamId;
        }

        // get initial Hp and HpMax
        if (!initialProps.TryGetValue(nameof(PlayerState.Hp), out var initialHpObj) || initialHpObj is not float initialHp)
        {
            Logging.LogWarning("Joining player did not set initial HP");
            initialHp = 1000f;
        }
        else
        {
            Logging.LogDebug("Setting initial HP to {Hp}", initialHp);
        }

        if (!initialProps.TryGetValue($"{Constants.AttributePrefix}{EBGUAttrFloat.HpMaxBase}", out var initialHpMaxObj) || initialHpMaxObj is not float initialHpMaxBase)
        {
            Logging.LogWarning("Joining player did not set initial HPMax");
            initialHpMaxBase = 1000f;
        }
        else
        {
            Logging.LogDebug("Setting initial HPMax to {HpMax}", initialHpMaxBase);
        }

        var playerState = new PlayerState(playerId, newPawn, teamId, initialHp, initialHpMaxBase)
        {
            Location = loc,
            Rotation = rot
        };

        // set nickname
        if (initialProps.TryGetValue(nameof(PlayerState.NickName), out var nickName))
        {
            playerState.NickName = (string)nickName;
            Logging.LogDebug("Setting initial Nickname to {Nickname}", playerState.NickName);
        }
        else
        {
            Logging.LogWarning("Initial nickname not provided");
        }

        // set IsReadyForPvP and IsSpectator
        if (initialProps.TryGetValue(nameof(PlayerState.IsReadyForPvP), out var isReady))
        {
            playerState.IsReadyForPvP = (bool)isReady;
            Logging.LogDebug("Setting initial IsReadyForPvP to {IsReady}", playerState.IsReadyForPvP);
        }

        if (initialProps.TryGetValue(nameof(PlayerState.IsSpectator), out var isSpectator))
        {
            playerState.IsSpectator = (bool)isSpectator;
            Logging.LogDebug("Setting initial IsSpectator to {IsSpectator}", playerState.IsSpectator);
        }

        // set attributes
        foreach (var attr in Constants.SyncedAttributes)
        {
            if (initialProps.TryGetValue($"{Constants.AttributePrefix}{attr}", out var value))
            {
                Logging.LogTrace("Setting remote player initial attribute {Attribute} = {Value}", attr, value);
                playerState.Attributes[attr] = (float)value;
            }
        }

        // update equipment
        if (initialProps.TryGetValue(nameof(PlayerState.Equipment), out var eq))
        {
            playerState.Equipment = (EquipmentState)eq;
            EquipmentHelpers.SetRemoteActorEquipment(newPawn, playerState.Equipment);
        }

        // set lock distance
        FUStUnitCommDesc unitCommDesc = BGW_GameDB.GetUnitCommDesc(newPawn.GetResID());
        if (unitCommDesc != null)
        {
            unitCommDesc.CameraLockDist = 10000;
        }

        return playerState;
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

    public static void SpawnUnitsMaster(PlayerId playerId, string unitName, int count, int teamId)
    {
        var playerState = DI.Instance.Players.GetPlayerById(playerId);
        if (playerState == null || playerState.Pawn == null)
        {
            Logging.LogError("Player not found: {PlayerId}", playerId);
            return;
        }

        var spawnLoc = playerState.Pawn.GetActorLocation() + playerState.Pawn.GetActorForwardVector() * Constants.MonsterSpawnDistance;
        var startLoc = spawnLoc + FVector.UpVector * Constants.MonsterSpawnTraceHeight / 2;
        var endLoc = spawnLoc - FVector.UpVector * Constants.MonsterSpawnTraceHeight / 2;

        // trace vertically for spawn height
        var hit = BGUFuncLibSelectTargetsCS.LineTraceForHitWorldItem(GameUtils.GetWorld(), startLoc, endLoc, out var hitResultSimple);
        if (hit)
        {
            spawnLoc = hitResultSimple.HitLocation + FVector.UpVector * Constants.MonsterHalfHeight;
        }

        // spawn in a grid around center point, separated by 200 units
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
                var loc = spawnLoc + new FVector(x, y, 0);

                var localI = placed;
                Task.Run(async () =>
                {
                    // wait for i * 200ms
                    await Task.Delay(localI * Constants.MonsterSpawnDelayMs);
                    SpawnUnitMaster(unitName, loc, teamId);
                });
                placed++;
                if (placed == count)
                    goto Notify;
            }
        }

        Notify:
        DI.Instance.Chatter.SendServerMessage("PlayerSpawned", DI.Instance.Players.LocalPlayerState.NickName, count.ToString(), unitName);
    }

    public static void SpawnUnitMaster(string unitName, FVector loc, int teamId)
    {
        var unitPath = UnitPathsConfig.GetUnitPath(unitName);

        var guid = Guid.NewGuid().ToString();

        Logging.LogDebug("Sending spawn unit {Name} at {Location}", unitName, loc.ToCompactString());
        SpawnUnitLocally(guid, unitPath, teamId, loc.X, loc.Y, loc.Z);
    }

    public static void SpawnUnitLocally(string guid, string unitPath, int teamId, float x, float y, float z)
    {
        GameLoopPatch.QueueOnGameThread(() =>
        {
            Logging.LogDebug("Spawn unit called for {UnitPath}", unitPath);

            if (string.IsNullOrEmpty(unitPath))
                return;

            var loc = new FVector(x, y, z);
            var rot = new FRotator();

            var world = GameUtils.GetWorld();

            var unitClass = BGW_PreloadAssetMgr.Get(world).TryGetCachedResourceObj<UClass>(unitPath, ELoadResourceType.SyncLoadAndCache);
            var transform = new FTransform(rot, loc);
            var tamerActor = UBGUFunctionLibrary.BGUBeginDeferredActorSpawnFromClass(world, (TSubclassOf<AActor>)unitClass, transform, ESpawnActorCollisionHandlingMethod.AdjustIfPossibleButAlwaysSpawn, null) as BUTamerActor;
            if (tamerActor == null)
            {
                Logging.LogError("Could not spawn unit: {UnitPath}", unitPath);
                return;
            }

            tamerActor.MarkAsSpawnedTamer(null);
            tamerActor.ExtendConfigComp.ActorResetType = EBGUResetType.Destroy;

            tamerActor.SpawnedTamerGuid = guid;
            // Update final guid
            tamerActor.GetFinalGuid(true);

            Logging.LogDebug("Spawned enemy: {TamerName}, with Guid {Guid}", tamerActor.GetName(), guid);
            var entity = CreateMonsterInEcs(guid, tamerActor, teamId, unitPath);

            ref var trans = ref entity.GetComponent<TranslationComponent>();
            trans.Position = loc.ToVector3();
            trans.Rotation = rot.ToVector3();

            UBGUFunctionLibrary.BGUFinishSpawningActor(tamerActor, transform);
            BGS_GSEventCollection.Get(tamerActor)?.Evt_TamerBlockingSpawnImmediately.Invoke(guid);

            ref var nameComp = ref entity.GetComponent<NicknameComponent>();
            nameComp.Nickname = "Bot";

            MarkerUtils.CreateMarkerForCharacter(entity); // 3D marker above monster
            if (unitPath == UnitPathsConfig.GetUnitPath(CharacterKind.Monkey))
            {
                SetMonkeyBotConfig(tamerActor.GetMonster());
            }
        }, nameof(SpawnUnitLocally));
    }

    public static void SpawnBots()
    {
        for (var i = 0; i < Constants.BotCount; i++)
        {
            var angle = i / (float)Constants.BotCount * 2f * FMath.PI;
            var x = FMath.Cos(angle) * Constants.PvpMonsterRadius;
            var y = FMath.Sin(angle) * Constants.PvpMonsterRadius;

            var levelData = LevelSpawnConfig.GetCurrentLevelSpawnData();
            var spawnPosition = levelData.PvpStartingLocation + new FVector(x, y, 0f);
            SpawnUnitMaster(CharacterKind.Monkey, spawnPosition, PvPUtils.GetOppositeTeam(DI.Instance.Players.LocalPlayerState.TeamId));
        }
    }

    public static Entity CreateMonsterInEcs(string guid, BUTamerActor tamer, int teamId, string unitName)
    {
        Logging.LogDebug("Created monster state with team ID: {TeamId} (assigned)", teamId);

        return DI.Instance.PawnRegistry.CreateNetworkedMonster(
            new LocalTamerComponent(tamer),
            new TamerComponent
            {
                Guid = guid,
                UnitPath = unitName
            }, new TeamComponent
            {
                TeamId = teamId
            });
    }

    public static FVector AdjustSpawnLocation(ABGUCharacter? CharacterCS, FVector InTargetLocation)
    {
        // TODO: For Heart of Birthstone map adjustment resulted in falling - invisible collision. So it is disabled for now.
        if (CmdLineParams.Instance.LevelId == 0)
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

    private static void SetMonkeyBotConfig(BGUCharacterCS bGUCharacter)
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