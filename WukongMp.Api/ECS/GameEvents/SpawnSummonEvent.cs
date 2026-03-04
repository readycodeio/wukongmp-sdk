using System;
using System.Collections.Generic;
using b1;
using Friflo.Engine.ECS;
using ReadyM.Api.Multiplayer.Mapping;
using ReadyM.Wukong.Common.ECS.Values;
using UnrealEngine.Runtime;
using WukongMp.Api.Mapping.Policies.Event;

namespace WukongMp.Api.ECS.GameEvents;

public readonly struct SpawnSummonEvent(Entity summoner, string summonGuid, string summonClassPath)
    : IEquatable<SpawnSummonEvent>, IMappingContext<SpawnSummonContext>
{
    public readonly Entity Summoner = summoner;
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
    public readonly Entity CatchTarget;

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

    public SpawnSummonEvent(
        Entity summoner,
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
        Entity catchTarget,
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