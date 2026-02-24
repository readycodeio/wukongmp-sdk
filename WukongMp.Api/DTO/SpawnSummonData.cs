using b1;
using LiteNetLib.Utils;
using ReadyM.Api.Multiplayer.ECS.Values;
using ReadyM.Api.Serialization;
using ReadyM.Relay.Common.Wukong.ECS.Values;
using System;
using System.Collections.Generic;
using UnrealEngine.Runtime;
using WukongMp.Api.Serialization;

namespace WukongMp.Api.DTO;

[DeriveJsonSerializable]
public partial struct SpawnSummonData(NetworkId summonerNetId, string summonGuid, string summonClassPath) 
    : INetSerializable, IEquatable<SpawnSummonData>
{
    public NetworkId SummonerNetId = summonerNetId;
    public string SummonGuid = summonGuid;
    public string SummonClassPath = summonClassPath;

    public FVector Location;
    public FRotator Rotation;
    public bool SafeClampToLand;
    public int SummonId;
    public Guid SummonInstanceId;
    public EServantType ServantType;
    public EServantSearchTargetType SearchTargetType;
    public string CooperativeSCGuid = "";
    public float AliveTime;
    public NetworkId CatchTargetNetId;

    public float DelayBornTime;
    public string BornMontagePath = "";
    public int BornSkill;
    public float DelayEffectTime;
    public float DelaySummonTime;
    public bool IsSummonerAsMaster;
    public EquipmentState EquipmentState;
    public float InitSpeed;
    public string BornEffectPath = "";
    public List<string> DisappearMontagePathList = [];
    public float DestroyDelayTime;

    public SpawnSummonData(
        NetworkId summonerNetId, 
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
        NetworkId catchTargetNetId,
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
        ) : this(summonerNetId, summonGuid, summonClassPath)
    {
        Location=location;
        Rotation=rotation;
        SafeClampToLand=safeClampToLand;
        SummonId=summonId;
        SummonInstanceId=summonInstanceId;
        ServantType=servantType;
        SearchTargetType=searchTargetType;
        CooperativeSCGuid=cooperativeSCGuid;
        AliveTime=aliveTime;
        CatchTargetNetId=catchTargetNetId;
        DelayBornTime=delayBornTime;
        BornMontagePath=bornMontagePath;
        BornSkill=bornSkill;
        DelayEffectTime=delayEffectTime;
        DelaySummonTime=delaySummonTime;
        IsSummonerAsMaster=isSummonerAsMaster;
        EquipmentState=equipmentState;
        InitSpeed=initSpeed;
        BornEffectPath=bornEffectPath;
        DisappearMontagePathList=disappearMontagePathList;
        DestroyDelayTime=destroyDelayTime;
    }

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(SummonerNetId);
        writer.Put(SummonGuid);
        writer.Put(SummonClassPath);

        SerializationHelpers.SerializeFVector(writer, Location);
        SerializationHelpers.SerializeFRotator(writer, Rotation);
        writer.Put(SafeClampToLand);    
        writer.Put(SummonId);
        writer.Put(SummonInstanceId);
        writer.Put((byte)ServantType);
        writer.Put((byte)SearchTargetType);
        writer.Put(CooperativeSCGuid);
        writer.Put(AliveTime);
        writer.Put(CatchTargetNetId);

        writer.Put(DelayBornTime);
        writer.Put(BornMontagePath);
        writer.Put(BornSkill);
        writer.Put(DelayEffectTime);
        writer.Put(DelaySummonTime);
        writer.Put(IsSummonerAsMaster);
        EquipmentState.Serialize(writer);
        writer.Put(InitSpeed);
        writer.Put(BornEffectPath);
        writer.Put(DisappearMontagePathList.Count);
        foreach (var path in DisappearMontagePathList)
            writer.Put(path);
        writer.Put(DestroyDelayTime);
    }

    public void Deserialize(NetDataReader reader)
    {
        SummonerNetId = reader.Get<NetworkId>();
        SummonGuid = reader.GetString();
        SummonClassPath = reader.GetString();

        Location = (FVector)SerializationHelpers.DeserializeFVector(reader);
        Rotation = (FRotator)SerializationHelpers.DeserializeFRotator(reader);
        SafeClampToLand = reader.GetBool();
        SummonId = reader.GetInt();
        SummonInstanceId = reader.GetGuid();
        ServantType = (EServantType)reader.GetByte();
        SearchTargetType = (EServantSearchTargetType)reader.GetByte();
        CooperativeSCGuid = reader.GetString();
        AliveTime = reader.GetFloat();
        CatchTargetNetId = reader.Get<NetworkId>();

        DelayBornTime = reader.GetFloat();
        BornMontagePath = reader.GetString();
        BornSkill = reader.GetInt();
        DelayEffectTime = reader.GetFloat();
        DelaySummonTime = reader.GetFloat();
        IsSummonerAsMaster = reader.GetBool();
        EquipmentState.Deserialize(reader);
        InitSpeed = reader.GetFloat();
        BornEffectPath = reader.GetString();
        var count = reader.GetInt();
        DisappearMontagePathList = [];
        for (var i = 0; i < count; i++)
        {
            DisappearMontagePathList.Add(reader.GetString());
        }
        DestroyDelayTime = reader.GetFloat();
    }

    public bool Equals(SpawnSummonData other)
    {
        if (!(
            SummonerNetId == other.SummonerNetId && 
            SummonGuid == other.SummonGuid && 
            SummonClassPath == other.SummonClassPath && 
            Location == other.Location && 
            Rotation == other.Rotation && 
            SafeClampToLand == other.SafeClampToLand && 
            SummonId == other.SummonId && 
            SummonInstanceId == other.SummonInstanceId && 
            ServantType == other.ServantType && 
            SearchTargetType == other.SearchTargetType && 
            CooperativeSCGuid == other.CooperativeSCGuid && 
            AliveTime == other.AliveTime && 
            CatchTargetNetId == other.CatchTargetNetId && 
            DelayBornTime == other.DelayBornTime && 
            BornMontagePath == other.BornMontagePath && 
            BornSkill == other.BornSkill && 
            DelayEffectTime == other.DelayEffectTime &&
            DelaySummonTime == other.DelaySummonTime && 
            IsSummonerAsMaster == other.IsSummonerAsMaster && 
            EquipmentState.Equals(other.EquipmentState) && 
            InitSpeed == other.InitSpeed && 
            BornEffectPath == other.BornEffectPath && 
            DestroyDelayTime == other.DestroyDelayTime))
        {
            return false;
        }

        if (DisappearMontagePathList.Count != other.DisappearMontagePathList.Count)
        {
            return false;
        }
        
        for (var i = 0; i < DisappearMontagePathList.Count; i++)
        {
            if (DisappearMontagePathList[i] != other.DisappearMontagePathList[i])
                return false;
        }
        
        return true;
    }

    public override bool Equals(object? obj)
    {
        return obj is SpawnSummonData other && Equals(other);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = SummonerNetId.GetHashCode();
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
            hashCode = (hashCode * 397) ^ CatchTargetNetId.GetHashCode();
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
            foreach (var path in DisappearMontagePathList)
            {
                hashCode = (hashCode * 397) ^ path.GetHashCode();
            }
            return hashCode;
        }
    }

    public static bool operator ==(SpawnSummonData left, SpawnSummonData right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(SpawnSummonData left, SpawnSummonData right)
    {
        return !left.Equals(right);
    }
}
