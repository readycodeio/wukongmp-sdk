using b1;
using b1.BGW;
using LiteNetLib.Utils;
using UnrealEngine.Runtime;
using WukongMp.Api.WukongUtils;

namespace WukongMp.Api.Serialization;

public static class SerializationHelpers
{
    public static void SerializeFVector(NetDataWriter outStream, object obj)
    {
        var vec = (FVector)obj;
        outStream.Put(vec.X);
        outStream.Put(vec.Y);
        outStream.Put(vec.Z);
    }

    public static object DeserializeFVector(NetDataReader inStream)
    {
        var x = inStream.GetFloat();
        var y = inStream.GetFloat();
        var z = inStream.GetFloat();
        return new FVector(x, y, z);
    }

    public static void SerializeFVector2D(NetDataWriter outStream, object obj)
    {
        var vec = (FVector2D)obj;
        outStream.Put(vec.X);
        outStream.Put(vec.Y);
    }

    public static object DeserializeFVector2D(NetDataReader inStream)
    {
        var x = inStream.GetFloat();
        var y = inStream.GetFloat();
        return new FVector2D(x, y);
    }

    public static void SerializeFRotator(NetDataWriter outStream, object obj)
    {
        var vec = (FRotator)obj;
        outStream.Put(vec.Pitch);
        outStream.Put(vec.Yaw);
        outStream.Put(vec.Roll);
    }

    public static object DeserializeFRotator(NetDataReader inStream)
    {
        var pitch = inStream.GetFloat();
        var yaw = inStream.GetFloat();
        var roll = inStream.GetFloat();
        return new FRotator(pitch, yaw, roll);
    }

    public static void SerializeDamageNumParam(NetDataWriter outStream, object obj)
    {
        var dmg = (DamageNumParam)obj;

        outStream.Put(dmg.DamageNum);
        outStream.Put((byte)dmg.DamageType);
        SerializeFVector(outStream, dmg.RealHitLocation);
        outStream.Put(dmg.Amplitude);
        outStream.Put((byte)dmg.AttackerTeamType);
        SerializeFVector(outStream, dmg.RealHitDir);
    }

    public static object DeserializeDamageNumParam(NetDataReader inStream)
    {
        var damageNum = inStream.GetInt();
        var damageType = (EDamageNumberType)inStream.GetByte();
        var realHitLocation = (FVector)DeserializeFVector(inStream);
        var amplitude = inStream.GetFloat();
        var attackerTeamType = (EDmgNumUITeamType)inStream.GetByte();
        var realHitDir = (FVector)DeserializeFVector(inStream);
        return new DamageNumParam(damageType, damageNum, amplitude, realHitLocation, realHitDir, attackerTeamType);
    }


    public static void SerializeServantRequest(NetDataWriter outStream, object obj)
    {
        var servantReq = (FServantReq)obj;

        outStream.Put(servantReq.SummonID);
        outStream.Put(GameplayTagExtension.ConvertToGuid(servantReq.SummonInstanceID));
        outStream.Put(servantReq.ServantTamerGuid);
        outStream.Put((byte)servantReq.ServantType);
        outStream.Put((byte)servantReq.SearchTargetType);
        outStream.Put(servantReq.CooperativeSCGuid);
        //outStream.Put(servantReq.DelayBornTime);
        //outStream.Put(servantReq.BornMontage.PathName);
        //outStream.Put(servantReq.BornSkill);
        //outStream.Put(servantReq.DelayEffectTime);
        //outStream.Put(servantReq.DelaySummonTime);
        SerializeFVector(outStream, servantReq.BornTransform.GetLocation());
        SerializeFRotator(outStream, servantReq.BornTransform.GetRotation());
        outStream.Put(servantReq.AliveTime);
        outStream.Put(servantReq.TamerTemplate.PathName);
        //var isSummonerAsMaster = servantReq.MasterActor == servantReq.Summoner;
        //outStream.Put(isSummonerAsMaster);
        //var eq = new EquipmentState(servantReq.MapEquip.Select(kvp => (kvp.Key.FromGame(), kvp.Value)));
        //eq.Serialize(outStream);
        //outStream.Put(servantReq.InitSpeed);
        //string bornEffectPath = "";
        //if (servantReq.BornDBC != null)
        //{
        //    bornEffectPath = servantReq.BornDBC.PathName;
        //}
        //else if (servantReq.BornNiagara != null)
        //{
        //    bornEffectPath = servantReq.BornNiagara.PathName;
        //}
        //else if (servantReq.BornParticle != null)
        //{
        //    bornEffectPath = servantReq.BornParticle.PathName;
        //}
        //outStream.Put(bornEffectPath);
        //outStream.Put(servantReq.DisappearMontagePathList.Count);
        //foreach(var path in  servantReq.DisappearMontagePathList)
        //    outStream.Put(path);
        //outStream.Put(servantReq.DestroyDelayTime);
        outStream.Put(servantReq.SafeClampToLand);
    }

    public static object DeserializeServantRequest(NetDataReader inStream)
    {
        BGW_PreloadAssetMgr preloadAssetMgr = BGW_PreloadAssetMgr.Get(GameUtils.GetWorld());

        var summonID = inStream.GetInt();
        var summonInstanceId = inStream.GetGuid();
        var servantTamerGuid = inStream.GetString();
        EServantType servantType = (EServantType)inStream.GetByte();
        EServantSearchTargetType servantTargetType = (EServantSearchTargetType)inStream.GetByte();
        var cooperativeSCGuid = inStream.GetString();
        //var delayBornTime = inStream.GetFloat();
        //var bornMontagePath = inStream.GetString();
        //UAnimMontage bornMontage = preloadAssetMgr.TryGetCachedResourceObj<UAnimMontage>(bornMontagePath, ELoadResourceType.SyncLoadAndCache);
        //var bornSkill = inStream.GetInt();
        //var delayEffectTime = inStream.GetFloat();
        //var delaySummonTime = inStream.GetFloat();
        var location = (FVector)DeserializeFVector(inStream);
        var rotation = (FRotator)DeserializeFRotator(inStream);
        var aliveTime = inStream.GetFloat();
        var tamerTemplatePath = inStream.GetString();
        UClass tamerTemplate = preloadAssetMgr.TryGetCachedResourceObj<UClass>(tamerTemplatePath, ELoadResourceType.SyncLoadAndCache);
        //var isSummonerAsMaster = inStream.GetBool();
        //if (isSummonerAsMaster)
        //{

        //}
        //EquipmentState eq = (EquipmentState)EquipmentState.DeserializeUntyped(inStream);
        //Dictionary<BtlB1.EquipPosition, int> equipmentMap = [];
        //foreach(var (position, item) in eq.GetItems())
        //{
        //    equipmentMap.Add(position.ToGame(), item);
        //}
        //var initSpeed = inStream.GetFloat();
        //var bornEffectPath = inStream.GetString();
        //BGWDataAsset_B1DBC? bornBDC = null;
        //UNiagaraSystem? bornNiagara = null;
        //UParticleSystem? bornParticle = null;
        //if (!string.IsNullOrEmpty(bornEffectPath))
        //{
        //    UObject uObject = preloadAssetMgr.TryGetCachedResourceObj<UObject>(bornEffectPath, ELoadResourceType.AsyncLoadAndCache, EAssetPriority.Medium);
        //    if (uObject != null)
        //    {
        //        bornBDC = uObject as BGWDataAsset_B1DBC;
        //        if (bornBDC == null)
        //        {
        //            bornNiagara = uObject as UNiagaraSystem;
        //            if (bornNiagara == null)
        //            {
        //                bornParticle = uObject as UParticleSystem;
        //            }
        //        }
        //    }
        //}
        //var count = inStream.GetInt();
        //List<string> disappearMontagePathList = [];
        //for (var i = 0; i < count; i++)
        //{
        //    disappearMontagePathList.Add(inStream.GetString());
        //}
        //var destroyDelayTime = inStream.GetFloat();
        var safeClampToLand = inStream.GetBool();

        return new FServantReq
        {
            SummonID = summonID,
            SummonInstanceID = GameplayTagExtension.ConvertToCalliopeGuid(summonInstanceId),
            ServantTamerGuid = servantTamerGuid,
            ServantType = servantType,
            SearchTargetType = servantTargetType,
            CooperativeSCGuid = cooperativeSCGuid,
            //DelayBornTime = delayBornTime,
            //BornMontage = bornMontage,
            //BornSkill = bornSkill,
            //DelayEffectTime = delayEffectTime,
            //DelaySummonTime = delaySummonTime,
            BornTransform = new FTransform(rotation, location),
            AliveTime = aliveTime,
            TamerTemplate = tamerTemplate,
            //MapEquip = equipmentMap,
            //InitSpeed = initSpeed,
            //BornDBC = bornBDC,
            //BornNiagara = bornNiagara,
            //BornParticle = bornParticle,
            //DisappearMontagePathList = disappearMontagePathList,
            //DestroyDelayTime = destroyDelayTime,
            SafeClampToLand = safeClampToLand
        };
    }
}
