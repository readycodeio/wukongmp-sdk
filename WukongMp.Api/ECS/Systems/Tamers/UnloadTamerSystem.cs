using b1;
using Friflo.Engine.ECS.Systems;
using ReadyM.Wukong.Common.ECS.Components;
using WukongMp.Api.ECS.Components;
using WukongMp.Api.ECS.Entities;

namespace WukongMp.Api.ECS.Systems.Tamers;

public sealed class UnloadTamersSystem : QuerySystem<TamerComponent, LocalTamerComponent>
{
    protected override void OnUpdate()
    {
        Query.ForEachEntity((ref tamerComp, ref localTamerComp, entity) =>
        {
            var tamerEntity = new TamerEntity(entity);

            if (!localTamerComp.IsTamerSynced || tamerEntity.Tamer == null || tamerEntity.Tamer.CurrentRef == null || tamerEntity.Pawn == null)
            {
                return;
            }

            if (localTamerComp is { IsMonsterActive: true, IsLocallySpawned: false, HasPendingUnload: true }
                && !tamerComp.ForceKeepSpawned
                && tamerEntity.Tamer.CurrentRef.Phase != ETamerPhase.Loaded)
            {
                tamerEntity.Tamer.CurrentRef.TurnBack2Loaded();
            }
        });
    }

    private bool CanTurnBack2Loaded(FTamerRef tamerRef)
    {
        if (tamerRef.MonsterInstancePtr.IsValid())
        {
            BUC_BattleStateData persistentReadOnlyData1 = BGU_DataUtil.GetUnPersistentReadOnlyData<BUC_BattleStateData>(tamerRef.MonsterInstancePtr.Get());
            if (persistentReadOnlyData1 != null && persistentReadOnlyData1.IsUnitInBattle())
                return false;
            BUC_PatrolData persistentReadOnlyData2 = BGU_DataUtil.GetUnPersistentReadOnlyData<BUC_PatrolData>(tamerRef.MonsterInstancePtr.Get());
            if (persistentReadOnlyData2 != null && persistentReadOnlyData2.bIsPatroling)
                return false;
            BUC_UnitStateData persistentReadOnlyData3 = BGU_DataUtil.GetUnPersistentReadOnlyData<BUC_UnitStateData>(tamerRef.MonsterInstancePtr.Get());
            if (persistentReadOnlyData3 != null && persistentReadOnlyData3.HasState(EBGUUnitState.Dead))
                return false;
        }

        return true;
    }
}