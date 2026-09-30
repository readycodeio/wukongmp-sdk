using System;
using System.Collections.Generic;
using System.Numerics;
using b1;
using Friflo.Engine.ECS;
using ReadyM.Api.Mapping.Events;
using ReadyM.Api.Multiplayer.GameEvents;
using ReadyM.Wukong.Common.ECS.Components;
using ReadyM.Wukong.Common.ECS.Values;
using UnrealEngine.Runtime;
using WukongMp.Api.Configuration;
using WukongMp.Api.ECS.Entities;
using WukongMp.Api.GameEvents;

namespace WukongMp.Api.ECS.GameEvents;

/// <summary>
/// The policy is written by hand. A summon by a player runs on the machine that owns that player. A summon by
/// anything else (a quest actor, another unmapped spawn point) runs on the master client, and on a player near the
/// summon when neither the master nor a player with a lower id is near it too.
/// </summary>
internal readonly partial struct SpawnSummonEvent(Entity? summoner, string summonGuid, string summonClassPath)
    : IEquatable<SpawnSummonEvent>, IGameEvent
{
    public readonly Entity? Summoner = summoner;
    public readonly string SummonGuid = summonGuid;
    public readonly string SummonClassPath = summonClassPath;

    public readonly FVector Location;
    public readonly FRotator Rotation;
    public readonly bool SafeClampToLand;
    public readonly int SummonId;
    public readonly Guid SummonInstanceId;
    public readonly EServantType ServantType;
    public readonly EServantSearchTargetType SearchTargetType;
    public readonly string CooperativeSCGuid = "";
    public readonly float AliveTime;
    public readonly Entity? CatchTarget;

    public readonly float DelayBornTime;
    public readonly string BornMontagePath = "";
    public readonly int BornSkill;
    public readonly float DelayEffectTime;
    public readonly float DelaySummonTime;
    public readonly bool IsSummonerAsMaster;
    public readonly EquipmentState EquipmentState;
    public readonly float InitSpeed;
    public readonly string BornEffectPath = "";
    public readonly List<string> DisappearMontagePathList = [];
    public readonly float DestroyDelayTime;

    /// <summary>Sets only the summoner and the location, the fields the policy reads, so a patch can ask the policy
    /// before the game has spawned the summon.</summary>
    public SpawnSummonEvent(Entity? summoner, FVector location)
        : this(summoner, "", "")
    {
        Location = location;
    }

    public SpawnSummonEvent(
        Entity? summoner,
        string summonGuid, 
        string summonClassPath, 
        FVector location, 
        FRotator rotation, 
        bool safeClampToLand, 
        int summonId,
        Guid summonInstanceId, 
        EServantType servantType, 
        EServantSearchTargetType searchTargetType, 
        string cooperativeSCGuid, 
        float aliveTime,
        Entity? catchTarget,
        float delayBornTime,
        string bornMontagePath, 
        int bornSkill, 
        float delayEffectTime, 
        float delaySummonTime, 
        bool isSummonerAsMaster,
        EquipmentState equipmentState, 
        float initSpeed, 
        string bornEffectPath, 
        List<string> disappearMontagePathList,
        float destroyDelayTime
        ) : this(summoner, summonGuid, summonClassPath)
    {
        Location = location;
        Rotation = rotation;
        SafeClampToLand = safeClampToLand;
        SummonId = summonId;
        SummonInstanceId = summonInstanceId;
        ServantType = servantType;
        SearchTargetType = searchTargetType;
        CooperativeSCGuid = cooperativeSCGuid;
        AliveTime = aliveTime;
        CatchTarget = catchTarget;
        DelayBornTime = delayBornTime;
        BornMontagePath = bornMontagePath;
        BornSkill = bornSkill;
        DelayEffectTime = delayEffectTime;
        DelaySummonTime = delaySummonTime;
        IsSummonerAsMaster = isSummonerAsMaster;
        EquipmentState = equipmentState;
        InitSpeed = initSpeed;
        BornEffectPath = bornEffectPath;
        DisappearMontagePathList = disappearMontagePathList;
        DestroyDelayTime = destroyDelayTime;
    }

    public GameEventNotifyResult CanGameEventNotifyEcs(GameEventContextRegistry contexts)
        => CanSummon(contexts) ? GameEventNotifyResult.Notify : GameEventNotifyResult.DontNotify;

    public GameEventResult CanGameEventRunLocally(GameEventContextRegistry contexts)
        => CanSummon(contexts) ? GameEventResult.RunAll : GameEventResult.Rejected;

    public GameEventResult CanEcsInvokeGameEvent(GameEventContextRegistry contexts)
        => GameEventResult.RunAll;

    private bool CanSummon(GameEventContextRegistry contexts)
    {
        if (Summoner != null && (MainCharacterEntity.IsMainCharacter(Summoner.Value) || TamerEntity.IsTamer(Summoner.Value)))
        {
            // If a player is the summoner, apply ownership semantics.
            return contexts.GetContext<OwnershipContext>().OwnsEntity(Summoner.Value);
        }

        // Summoner is not a mapped entity, e.g. a BGU_QuestActor spawn point
        var wukong = contexts.GetContext<WukongPlayerContext>();
        var playerState = wukong.PlayerState;
        var areaState = wukong.AreaState;

        var localMainEntity = playerState.LocalMainCharacter;
        if (localMainEntity == null)
            return false;

        if (playerState.LocalPlayerId == null)
            return false;

        if (areaState.IsMasterClient) // Master client can always summon, to avoid issues with distant summons and no players around.
            return true;

        var localPlayerId = playerState.LocalPlayerId.Value;
        var localPosition = localMainEntity.Value.GetTransform().Position;
        var squaredDistanceToSummon = FVector.DistSquared(localPosition.ToFVector(), Location);
        const float squaredSpawnOwnershipRadius = Constants.SpawnOwnershipRadius * Constants.SpawnOwnershipRadius;
        if (squaredDistanceToSummon > squaredSpawnOwnershipRadius)
        {
            return false; // Distant summon -> master as owner
        }

        // Check if master or another player with lower id is nearby
        var canSummon = true;
        wukong.World.Query<MainCharacterComponent, TransformComponent>().ForEachEntity((ref mainComp, ref trans, entity) =>
        {
            if (entity == localMainEntity.Value.Entity)
                return;

            var squaredDistance = Vector3.DistanceSquared(localPosition, trans.Position);
            if (squaredDistance < squaredSpawnOwnershipRadius && (areaState.MasterClientId == mainComp.PlayerId || mainComp.PlayerId.RawValue < localPlayerId.RawValue))
            {
                canSummon = false;
            }
        });

        return canSummon;
    }

    public bool Equals(SpawnSummonEvent other)
    {
        if (!(Summoner != other.Summoner ||
            SummonGuid != other.SummonGuid ||
            SummonClassPath != other.SummonClassPath ||
            Location != other.Location ||
            Rotation != other.Rotation ||
            SafeClampToLand != other.SafeClampToLand ||
            SummonId != other.SummonId ||
            SummonInstanceId != other.SummonInstanceId ||
            ServantType != other.ServantType ||
            SearchTargetType != other.SearchTargetType ||
            CooperativeSCGuid != other.CooperativeSCGuid ||
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            AliveTime != other.AliveTime ||
            CatchTarget != other.CatchTarget ||
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            DelayBornTime != other.DelayBornTime ||
            BornMontagePath != other.BornMontagePath ||
            BornSkill != other.BornSkill ||
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            DelayEffectTime != other.DelayEffectTime ||
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            DelaySummonTime != other.DelaySummonTime ||
            IsSummonerAsMaster != other.IsSummonerAsMaster ||
            EquipmentState.Equals(other.EquipmentState) ||
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            InitSpeed != other.InitSpeed ||
            BornEffectPath != other.BornEffectPath ||
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            DestroyDelayTime != other.DestroyDelayTime))
        {
            return false;
        }
        
        if (DisappearMontagePathList.Count != other.DisappearMontagePathList.Count)
            return false;
        
        for (var i = 0; i < DisappearMontagePathList.Count; i++)
        {
            if (DisappearMontagePathList[i] != other.DisappearMontagePathList[i])
                return false;
        }

        return true;
    }

    public override bool Equals(object? obj)
        => obj is SpawnSummonEvent other && Equals(other);

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = Summoner.GetHashCode();
            hashCode = (hashCode * 397) ^ SummonGuid.GetHashCode();
            hashCode = (hashCode * 397) ^ SummonClassPath.GetHashCode();
            hashCode = (hashCode * 397) ^ Location.GetHashCode();
            hashCode = (hashCode * 397) ^ Rotation.GetHashCode();
            hashCode = (hashCode * 397) ^ SafeClampToLand.GetHashCode();
            hashCode = (hashCode * 397) ^ SummonId;
            hashCode = (hashCode * 397) ^ SummonInstanceId.GetHashCode();
            hashCode = (hashCode * 397) ^ (int)ServantType;
            hashCode = (hashCode * 397) ^ (int)SearchTargetType;
            hashCode = (hashCode * 397) ^ CooperativeSCGuid.GetHashCode();
            hashCode = (hashCode * 397) ^ AliveTime.GetHashCode();
            hashCode = (hashCode * 397) ^ CatchTarget.GetHashCode();
            hashCode = (hashCode * 397) ^ DelayBornTime.GetHashCode();
            hashCode = (hashCode * 397) ^ BornMontagePath.GetHashCode();
            hashCode = (hashCode * 397) ^ BornSkill;
            hashCode = (hashCode * 397) ^ DelayEffectTime.GetHashCode();
            hashCode = (hashCode * 397) ^ DelaySummonTime.GetHashCode();
            hashCode = (hashCode * 397) ^ IsSummonerAsMaster.GetHashCode();
            hashCode = (hashCode * 397) ^ EquipmentState.GetHashCode();
            hashCode = (hashCode * 397) ^ InitSpeed.GetHashCode();
            hashCode = (hashCode * 397) ^ BornEffectPath.GetHashCode();
            hashCode = (hashCode * 397) ^ DestroyDelayTime.GetHashCode();
            
            hashCode = (hashCode * 397) ^ DisappearMontagePathList.Count;
            foreach (var item in DisappearMontagePathList)
            {
                hashCode = (hashCode * 397) ^ item.GetHashCode();
            }
            
            return hashCode;
        }
    }
}