using b1;
using b1.BGW;
using ReadyM.Relay.Common.Wukong.ECS.Values;
using System.Collections.Generic;
using System.Linq;
using UnrealEngine.Engine;
using UnrealEngine.Plugins.Niagara;
using UnrealEngine.Runtime;
using WukongMp.Api.ECS.Values;
using WukongMp.Api.WukongUtils;

namespace WukongMp.Api.DTO
{
    public static class SummonRequestExtensions
    {
        public static FServantReq ToGame(this SummonRequestData value)
        {
            BGW_PreloadAssetMgr preloadAssetMgr = BGW_PreloadAssetMgr.Get(GameUtils.GetWorld());

            var summoner = DI.Instance.PawnState.GetPawnByNetworkId(value.SummonerId);
            var target = DI.Instance.PawnState.GetPawnByNetworkId(value.CatchTargetId);

            UClass tamerTemplate = preloadAssetMgr.TryGetCachedResourceObj<UClass>(value.SummonClassPath, ELoadResourceType.SyncLoadAndCache);
            UAnimMontage? bornMontage = null;
            if (!string.IsNullOrEmpty(value.BornMontagePath))
                bornMontage = preloadAssetMgr.TryGetCachedResourceObj<UAnimMontage>(value.BornMontagePath, ELoadResourceType.SyncLoadAndCache);

            Dictionary<BtlB1.EquipPosition, int> equipmentMap = [];
            foreach (var (position, item) in value.EquipmentState.GetItems())
            {
                equipmentMap.Add(position.ToGame(), item);
            }

            BGWDataAsset_B1DBC? bornBDC = null;
            UNiagaraSystem? bornNiagara = null;
            UParticleSystem? bornParticle = null;
            if (!string.IsNullOrEmpty(value.BornEffectPath))
            {
                UObject uObject = preloadAssetMgr.TryGetCachedResourceObj<UObject>(value.BornEffectPath, ELoadResourceType.AsyncLoadAndCache, EAssetPriority.Medium);
                if (uObject != null)
                {
                    bornBDC = uObject as BGWDataAsset_B1DBC;
                    if (bornBDC == null)
                    {
                        bornNiagara = uObject as UNiagaraSystem;
                        if (bornNiagara == null)
                        {
                            bornParticle = uObject as UParticleSystem;
                        }
                    }
                }
            }

            return new FServantReq
            {
                Summoner = summoner,
                SummonID = value.SummonId,
                SummonInstanceID = value.SummonInstanceId.ConvertToCalliopeGuid(),
                ServantTamerGuid = value.SummonGuid,
                ServantType = value.ServantType,
                SearchTargetType = value.SearchTargetType,
                CooperativeSCGuid = value.CooperativeSCGuid,
                BornTransform = new FTransform(value.Rotation, value.Location),
                AliveTime = value.AliveTime,
                TamerTemplate = tamerTemplate,
                SafeClampToLand = value.SafeClampToLand,
                CatchTarget = target,
                BirthBuffIDs = [],
                MasterActor = value.IsSummonerAsMaster ? summoner : null,
                DelayBornTime = value.DelayBornTime,
                BornMontage = bornMontage,
                BornSkill = value.BornSkill,
                DelayEffectTime = value.DelayEffectTime,
                DelaySummonTime = value.DelaySummonTime,
                MapEquip = equipmentMap,
                InitSpeed = value.InitSpeed,
                BornDBC = bornBDC,
                BornNiagara = bornNiagara,
                BornParticle = bornParticle,
                DisappearMontagePathList = value.DisappearMontagePathList,
                DestroyDelayTime = value.DestroyDelayTime,
            };
        }

        public static SummonRequestData FromGame(this FServantReq value)
        {
            var summonerNetId = DI.Instance.PawnState.GetNetworkIdByActor(value.Summoner);
            var catchTargetNetId = DI.Instance.PawnState.GetNetworkIdByActor(value.CatchTarget);

            var summonClassPath = value.TamerTemplate.PathName;
            var bornMontagePath = value.BornMontage?.PathName ?? "";

            EquipmentState equipment = new();
            if (value.MapEquip != null)
            {
                equipment = new EquipmentState(value.MapEquip.Select(kvp => (kvp.Key.FromGame(), kvp.Value)));
            }

            string bornEffectPath = "";
            if (value.BornDBC != null)
            {
                bornEffectPath = value.BornDBC.PathName;
            }
            else if (value.BornNiagara != null)
            {
                bornEffectPath = value.BornNiagara.PathName;
            }
            else if (value.BornParticle != null)
            {
                bornEffectPath = value.BornParticle.PathName;
            }

            return new SummonRequestData()
            {
                SummonerId = summonerNetId ?? default,
                SummonGuid = value.ServantTamerGuid,
                SummonClassPath = summonClassPath,

                Location = value.BornTransform.GetLocation(),
                Rotation = value.BornTransform.GetRotation().Rotator(),
                SafeClampToLand = value.SafeClampToLand,
                SummonId = value.SummonID,
                SummonInstanceId = value.SummonInstanceID.ConvertToGuid(),
                ServantType = value.ServantType,
                SearchTargetType = value.SearchTargetType,
                CooperativeSCGuid = value.CooperativeSCGuid,
                AliveTime = value.AliveTime,
                CatchTargetId = catchTargetNetId ?? default,

                DelayBornTime = value.DelayBornTime,
                BornMontagePath = bornMontagePath,
                BornSkill = value.BornSkill,
                DelayEffectTime = value.DelayEffectTime,
                DelaySummonTime = value.DelaySummonTime,
                IsSummonerAsMaster = value.MasterActor == value.Summoner,
                EquipmentState = equipment,
                InitSpeed = value.InitSpeed,
                BornEffectPath = bornEffectPath,
                DisappearMontagePathList = value.DisappearMontagePathList ?? [],
                DestroyDelayTime = value.DestroyDelayTime,
            };
        }
    }
}