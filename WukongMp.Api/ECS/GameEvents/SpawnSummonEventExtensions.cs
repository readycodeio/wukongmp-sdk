using System.Collections.Generic;
using System.Linq;
using b1;
using b1.BGW;
using ReadyM.Wukong.Common.ECS.Values;
using UnrealEngine.Engine;
using UnrealEngine.Plugins.Niagara;
using UnrealEngine.Runtime;
using WukongMp.Api.ECS.Values;
using WukongMp.Api.State;
using WukongMp.Api.WukongUtils;
using EquipPosition = BtlB1.EquipPosition;

namespace WukongMp.Api.ECS.GameEvents
{
    public static class SpawnSummonEventExtensions
    {
        // FIXME(api): Move to utils
        public static FServantReq ToGame(this SpawnSummonEvent value, WukongPawnState pawnState)
        {
            BGW_PreloadAssetMgr preloadAssetMgr = BGW_PreloadAssetMgr.Get(GameUtils.GetWorld());

            var summoner = pawnState.GetPawnByEntity(value.Summoner);
            var catchTarget = pawnState.GetPawnByEntity(value.CatchTarget);

            UClass tamerTemplate = preloadAssetMgr.TryGetCachedResourceObj<UClass>(value.SummonClassPath, ELoadResourceType.SyncLoadAndCache);
            UAnimMontage? bornMontage = null;
            if (!string.IsNullOrEmpty(value.BornMontagePath))
                bornMontage = preloadAssetMgr.TryGetCachedResourceObj<UAnimMontage>(value.BornMontagePath, ELoadResourceType.SyncLoadAndCache);

            Dictionary<EquipPosition, int> equipmentMap = [];
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
                CatchTarget = catchTarget,
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

        public static SpawnSummonEvent? FromGame(this FServantReq value, WukongPawnState pawnState)
        {
            var summoner = pawnState.GetEntityByActor(value.Summoner);
            var catchTarget = pawnState.GetEntityByActor(value.CatchTarget);
            
            if (!summoner.HasValue)
            {
                Logging.LogWarning("Summoner not found for SpawnSummonEvent.FromGame");
                return null;
            }

            var summonClassPath = value.TamerTemplate.PathName;
            var bornMontagePath = value.BornMontage?.PathName ?? "";

            EquipmentState equipment = new();
            if (value.MapEquip != null)
            {
                equipment = new EquipmentState(value.MapEquip.Select(kvp => (EquipPositionExtensions.FromGame(kvp.Key), kvp.Value)));
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

            return new SpawnSummonEvent(
                summoner: summoner.Value,
                summonGuid: value.ServantTamerGuid,
                summonClassPath: summonClassPath,

                location: value.BornTransform.GetLocation(),
                rotation: value.BornTransform.GetRotation().Rotator(),
                safeClampToLand: value.SafeClampToLand,
                summonId: value.SummonID,
                summonInstanceId: value.SummonInstanceID.ConvertToGuid(),
                servantType: value.ServantType,
                searchTargetType: value.SearchTargetType,
                cooperativeSCGuid: value.CooperativeSCGuid,
                aliveTime: value.AliveTime,
                catchTarget: catchTarget ?? default,

                delayBornTime: value.DelayBornTime,
                bornMontagePath: bornMontagePath,
                bornSkill: value.BornSkill,
                delayEffectTime: value.DelayEffectTime,
                delaySummonTime: value.DelaySummonTime,
                isSummonerAsMaster: value.MasterActor == value.Summoner,
                equipmentState: equipment,
                initSpeed: value.InitSpeed,
                bornEffectPath: bornEffectPath,
                disappearMontagePathList: value.DisappearMontagePathList ?? [],
                destroyDelayTime: value.DestroyDelayTime
            );
        }
    }
}