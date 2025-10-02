using b1;
using LiteNetLib.Utils;
using ReadyM.Api.Multiplayer.ECS.Values;
using ReadyM.Api.Serialization;
using WukongMp.Api.Serialization;

namespace WukongMp.Api.DTO;

[DeriveJsonSerializable]
public partial struct UnitSummonData(NetworkId summonerId/*, NetworkId catchTargetId*/, FServantReq servantReq)
{
    public NetworkId SummonerId = summonerId;
    //public NetworkId CatchTargetId = catchTargetId;
    public FServantReq ServantReq = servantReq;


    //public string SummonGuid = guid;
    //public string Class = summonClass;
    //public FVector Location = location;
    //public FRotator Rotation = rotation;
    //public bool SafeClampToLand = safeClampToLand;

    //// Additional FServantReq fields
    //public int SummonId;
    //public Guid SummonInstanceId;
    //public EServantType ServantType;
    //public EServantSearchTargetType SearchTargetType;
    //public string CooperativeSCGuid;
    //public float DelayBornTime;
    //public string BornMontagePath;
    //public int BornSkill;
    //public float DelayEffectTime;
    //public float DelaySummonTime;
    //public float AliveTime;
    //public NetworkId MasterActorId;
    //public Dictionary<EquipPosition, int> MapEquip;
    //public float InitSpeed;
    //public string BornEffectPath;
    //public List<string> DisappearMontagePathList;
    //public float DestroyDelayTime;

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(SummonerId);
        //writer.Put(CatchTargetId);
        SerializationHelpers.SerializeServantRequest(writer, ServantReq);
    }

    public void Deserialize(NetDataReader reader)
    {
        SummonerId = reader.Get<NetworkId>();
        //CatchTargetId = reader.Get<NetworkId>();
        ServantReq = (FServantReq)SerializationHelpers.DeserializeServantRequest(reader);
    }
}
