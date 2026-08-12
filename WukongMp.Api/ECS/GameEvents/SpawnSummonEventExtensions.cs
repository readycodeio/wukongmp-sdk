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
    internal static class SpawnSummonEventExtensions
    {
        // FIXME(api): Move to utils
        public static FServantReq ToGame(this SpawnSummonEvent value, WukongPawnState pawnState)
        {
            BGW_PreloadAssetMgr preloadAssetMgr = BGW_PreloadAssetMgr.Get(GameUtils.GetWorld());

            var summoner = pawnState.GetPawnByEntity(value.Summoner ?? default);
            var catchTarget = pawnState.GetPawnByEntity(value.CatchTarget ?? default);

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

        public static SpawnSummonEvent? FromGame(this FServantReq req, WukongPawnState pawnState)
        {
            var summoner = pawnState.GetEntityByActor(req.Summoner);
            var catchTarget = pawnState.GetEntityByActor(req.CatchTarget);

            var summonClassPath = req.TamerTemplate.PathName;
            var bornMontagePath = req.BornMontage?.PathName ?? "";

            EquipmentState equipment = new();
            if (req.MapEquip != null)
            {
                equipment = new EquipmentState(req.MapEquip.Select(kvp => (kvp.Key.FromGame(), kvp.Value)));
            }

            var bornEffectPath = "";
            if (req.BornDBC != null)
            {
                bornEffectPath = req.BornDBC.PathName;
            }
            else if (req.BornNiagara != null)
            {
                bornEffectPath = req.BornNiagara.PathName;
            }
            else if (req.BornParticle != null)
            {
                bornEffectPath = req.BornParticle.PathName;
            }

            return new SpawnSummonEvent(
                summoner: summoner,
                summonGuid: req.ServantTamerGuid,
                summonClassPath: summonClassPath,

                location: req.BornTransform.GetLocation(),
                rotation: req.BornTransform.GetRotation().Rotator(),
                safeClampToLand: req.SafeClampToLand,
                summonId: req.SummonID,
                summonInstanceId: req.SummonInstanceID.ConvertToGuid(),
                servantType: req.ServantType,
                searchTargetType: req.SearchTargetType,
                cooperativeSCGuid: req.CooperativeSCGuid,
                aliveTime: req.AliveTime,
                catchTarget: catchTarget,

                delayBornTime: req.DelayBornTime,
                bornMontagePath: bornMontagePath,
                bornSkill: req.BornSkill,
                delayEffectTime: req.DelayEffectTime,
                delaySummonTime: req.DelaySummonTime,
                isSummonerAsMaster: req.MasterActor == req.Summoner,
                equipmentState: equipment,
                initSpeed: req.InitSpeed,
                bornEffectPath: bornEffectPath,
                disappearMontagePathList: req.DisappearMontagePathList ?? [],
                destroyDelayTime: req.DestroyDelayTime
            );
        }
    }
}