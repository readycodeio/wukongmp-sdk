using b1;
using BtlShare;
using Microsoft.Extensions.Logging;
using UnrealEngine.Engine;
using UnrealEngine.Runtime;
using WukongMp.Api;
using WukongMp.Api.Configuration;
using WukongMp.Api.ECS.Entities;
using WukongMp.Sdk;
using WukongMp.Sdk.Api;

namespace WukongMp.Coop.Systems;

public class ReEnableCollidersSystem : PluginSystemBase
{
    private const float TickIntervalSeconds = 1; // Check every second
    
    private float _elapsedTime;

    private readonly Dictionary<AActor, float> _colliderDisableTimes = [];

    public ReEnableCollidersSystem(WukongLocalApi localApi, WukongClientApi clientApi, ILogger logger)
        : base(localApi, clientApi, logger)
    {
        LocalApi.OnObstacleCollision += OnObstacleCollision;
        LocalApi.OnDisableObstacle += OnDisableObstacle;
    }

    public override void Dispose()
    {
        base.Dispose();
        
        LocalApi.OnDisableObstacle -= OnDisableObstacle;
        LocalApi.OnObstacleCollision -= OnObstacleCollision;
    }

    private void OnDisableObstacle(AActor obstacle)
    {
        PermanentlyDisableCollider(obstacle);
    }

    private void OnObstacleCollision(MainCharacterEntity mainEntity, AActor obstacle, out bool shouldBlock)
    {
        shouldBlock = false;

        // NOTE(api): It is supposed to affect only the controlled character since it's arena collision-related
        if (mainEntity.GetState().PlayerId == Mod.Instance.ClientApi.LocalPlayerId) // TODO: We shouldn't expose MainCharacterEntity in API here
        {
            var owner = mainEntity.UnsyncedPawn;
            if (owner == null)
                return;
            
            var bossActor = GetClosestBossActor(owner, owner.GetActorLocation());
            if (bossActor == null)
                return;

            var bossLocation = bossActor.GetActorLocation();
            if (UBGUSelectUtil.MultiSphereTraceForObjects(owner, owner.GetActorLocation(), bossLocation, 10, [EObjectTypeQuery.ObjectTypeQuery15], false, out var hitResult) > 0 && hitResult.Any(x => x.HitActor == obstacle))
            {
                Logging.LogDebug("Hit dynamic obstacle wall is between boss and player, disabling collision temporarily");
                DisableCollider(obstacle, Constants.ColliderDisableTime);
                shouldBlock = true;
            }
        }
    }
    
    private static AActor? GetClosestBossActor(UObject context, FVector position)
    {
        AActor? closestBoss = null;
        var closestDistanceSquared = double.MaxValue;
        var monsters = UGameplayStatics.GetAllActorsOfClass<BGU_CharacterAI?>(context);
        foreach (var monster in monsters)
        {
            if (USharpExtensions.IsNullOrDestroyed(monster))
                continue;

            var info = BGW_GameDB.GetUnitBattleInfoExtendDesc(monster.GetFinalBattleInfoExtendID());

            if (info == null)
                continue;

            if (!(monster.bBossRoomMonster || info.QualityType is EUnitQualityType.NormalBoss or EUnitQualityType.FinalBoss || info.BloodBarType == EBGUBloodBarType.BossBar))
                continue;

            var distanceSquared = FVector.DistSquared2D(monster.GetActorLocation(), position);
            if (distanceSquared < closestDistanceSquared)
            {
                closestDistanceSquared = distanceSquared;
                closestBoss = monster;
            }
        }

        return closestBoss;
    }
    
    private void PermanentlyDisableCollider(AActor actor)
    {
        if (_colliderDisableTimes.ContainsKey(actor))
        {
            _colliderDisableTimes.Remove(actor);
            Logger.LogDebug("Permanently disabled collider for actor: {Actor}", BGU_DataUtil.GetActorGuid(actor));
        }
    }

    private void DisableCollider(AActor actor, float disableDuration)
    {
        _colliderDisableTimes[actor] = disableDuration;
        actor.SetActorEnableCollision(false);
    }

    private void TryReEnableColliders(float deltaTime)
    {
        var collidersToEnable = new List<AActor>();
        foreach (var collider in _colliderDisableTimes.Keys.ToList())
        {
            var remainingTime = _colliderDisableTimes[collider] - deltaTime;
            if (remainingTime <= 0f)
            {
                collidersToEnable.Add(collider);
            }
            else
            {
                _colliderDisableTimes[collider] = remainingTime;
            }
        }
        foreach (var collider in collidersToEnable)
        {
            collider.SetActorEnableCollision(true);

            if (ClientApi.LocalMainCharacter.HasValue && ClientApi.LocalMainCharacter.Value.Pawn != null)
            {
                var player = ClientApi.LocalMainCharacter.Value.Pawn!;
                var traceLength = player.CapsuleComponent.GetScaledCapsuleRadius() + 20f;
                var lineTraceDir = GetLineTraceDir_SafeNormal2D(player);
                var playerLocation = BGUFuncLibActorTransformCS.BGUGetActorLocation(player);
                var startTrace = playerLocation - lineTraceDir * traceLength;
                var endTrace = playerLocation + lineTraceDir * traceLength;
                if (UBGUSelectUtil.MultiSphereTraceForObjects(player, startTrace, endTrace, traceLength, [EObjectTypeQuery.ObjectTypeQuery15], false, out var HitResult) > 0)
                {
                    if (HitResult.Any(x => x.HitActor == collider))
                    {
                        Logger.LogDebug("Re-disabled collider for actor: {Actor} due to player proximity", BGU_DataUtil.GetActorGuid(collider));
                        DisableCollider(collider, Constants.ColliderDisableTime);
                        continue;
                    }
                }
            }

            _colliderDisableTimes.Remove(collider);
            Logger.LogDebug("Re-enabled collider for actor: {Actor}", BGU_DataUtil.GetActorGuid(collider));
        }
    }

    private FVector GetLineTraceDir_SafeNormal2D(BGUCharacterCS playerCharacter)
    {
        if (playerCharacter.CharacterMovement.IsFalling())
        {
            return playerCharacter.GetVelocity().GetSafeNormal2D();
        }

        return playerCharacter.CharacterMovement.GetCurrentAcceleration().GetSafeNormal2D();
    }

    protected override void OnUpdate(PluginTick tick)
    {
        if (!LocalApi.IsGameplayLevel)
            return;

        _elapsedTime += tick.DeltaTime;

        if (_elapsedTime < TickIntervalSeconds)
            return;

        TryReEnableColliders(_elapsedTime);
        _elapsedTime = 0f;
    }
}
