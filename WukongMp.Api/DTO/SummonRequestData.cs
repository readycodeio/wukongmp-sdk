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
public partial struct SummonRequestData(NetworkId summonerId, string summonGuid, string summonClassPath) : INetSerializable
{
    public NetworkId SummonerId = summonerId;
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
    public NetworkId CatchTargetId;

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

    public SummonRequestData(
        NetworkId summonerId, string summonGuid, string summonClassPath, FVector location, FRotator rotation, bool safeClampToLand, int summonId,
        Guid summonInstanceId, EServantType servantType, EServantSearchTargetType searchTargetType, string cooperativeSCGuid, float aliveTime, NetworkId catchTargetId,
        float delayBornTime, string bornMontagePath, int bornSkill, float delayEffectTime, float delaySummonTime, bool isSummonerAsMaster,
        EquipmentState equipmentState, float initSpeed, string bornEffectPath, List<string> disappearMontagePathList, float destroyDelayTime
        ) : this(summonerId, summonGuid, summonClassPath)
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
        CatchTargetId=catchTargetId;
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
        writer.Put(SummonerId);
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
        writer.Put(CatchTargetId);

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
        SummonerId = reader.Get<NetworkId>();
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
        CatchTargetId = reader.Get<NetworkId>();

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
}
