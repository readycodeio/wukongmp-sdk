using System;
using System.Threading.Tasks;
using b1;
using b1.BGW;
using BtlShare;
using Friflo.Engine.ECS;
using ReadyM.Relay.Common.ECS;
using ReadyM.Relay.Common.Wukong.Components;
using UnrealEngine.Engine;
using UnrealEngine.Runtime;
using WukongMp.Api.Configuration;
using WukongMp.Api.ECS;
using WukongMp.Api.GameApi;
using WukongMp.Api.Old;
using WukongMp.Api.Old.Api;
using WukongMp.Api.Patches;

namespace WukongMp.Api.WukongUtils;

public static class SpawningUtils
{
    public static void SpawnUnitsMaster(short peerId, string unitName, int count, int teamId)
    {
        var playerState = WukongMpModBase.Client.GetPlayerById(peerId);
        if (playerState == null || playerState.Pawn == null)
        {
            Logging.LogError("Player not found: {PlayerId}", peerId);
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
        WukongMpModBase.Client.WukongChat.SendServerMessage("PlayerSpawned", WukongMpModBase.Client.LocalPlayerState.NickName, count.ToString(), unitName);
    }

    public static void SpawnUnitMaster(string unitName, FVector loc, int teamId)
    {
        var unitPath = UnitPathsConfig.GetUnitPath(unitName);

        var guid = Guid.NewGuid().ToString();

        Logging.LogDebug("Sending spawn unit {Name} at {Location}", unitName, loc.ToCompactString());
        SpawnUnitLocally(null, guid, unitPath, teamId, loc.X, loc.Y, loc.Z);
    }

    public static void SpawnUnitLocally(NetworkIdComponent? providedNetId, string guid, string unitPath, int teamId, float x, float y, float z)
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
            var entity = providedNetId.HasValue ? AddRemoteMonsterToEcs(providedNetId.Value, guid, tamerActor, teamId, unitPath) : CreateMonsterInEcs(guid, tamerActor, teamId, unitPath);

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
            SpawnUnitMaster(CharacterKind.Monkey, spawnPosition, GameUtils.GetOppositeTeam(WukongMpModBase.Client.LocalPlayerState.TeamId));
        }
    }

    public static Entity AddRemoteMonsterToEcs(NetworkIdComponent netId, string guid, BUTamerActor tamer, int teamId, string unitName)
    {
        var id = WukongMpMod.Instance.CreateNetworkedMonster(netId);

        id.AddComponent(new LocalTamerComponent(tamer));

        ref var tamerComp = ref id.GetComponent<TamerComponent>();
        tamerComp.Guid = guid;
        tamerComp.UnitPath = unitName;

        ref var teamComp = ref id.GetComponent<TeamComponent>();
        teamComp.TeamId = teamId;

        Logging.LogDebug("Created monster state with team ID: {TeamId} (assigned)", teamId);
        return id;
    }

    public static Entity CreateMonsterInEcs(string guid, BUTamerActor tamer, int teamId, string unitName)
    {
        var id = WukongMpMod.Instance.CreateNetworkedMonster();
        id.AddComponent(new LocalTamerComponent(tamer));

        ref var tamerComp = ref id.GetComponent<TamerComponent>();
        tamerComp.Guid = guid;
        tamerComp.UnitPath = unitName;

        ref var teamComp = ref id.GetComponent<TeamComponent>();
        teamComp.TeamId = teamId;

        Logging.LogDebug("Created monster state with team ID: {TeamId} (assigned)", teamId);
        return id;
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