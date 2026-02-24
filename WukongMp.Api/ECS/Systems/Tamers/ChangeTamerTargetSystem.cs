using System.Diagnostics;
using b1;
using Friflo.Engine.ECS.Systems;
using ReadyM.Relay.Common.Wukong.ECS.Components;
using ReadyM.Relay.Client.State;
using WukongMp.Api.Configuration;
using WukongMp.Api.ECS.Components;
using WukongMp.Api.ECS.Entities;
using WukongMp.Api.Mapping;

namespace WukongMp.Api.ECS.Systems.Tamers;

public sealed class ChangeTamerTargetSystem(
    WukongMappingPolicyDirectory policyDir, 
    // NOTE(api): API refactoring only
    ClientOwnershipManager ownershipManager) : QuerySystem<LocalTamerComponent>
{
    private float _elapsedTime;

    protected override void OnUpdate()
    {
        if (_elapsedTime >= Constants.MonsterUpdateTargetTime)
        {
            _elapsedTime = 0;

            Query.ForEachEntity((ref _, entity) =>
            {
                var tamerEntity = new TamerEntity(entity);
                var pawn = tamerEntity.Pawn;

                if (policyDir.TamerData<TamerComponent>().ShouldGameCopyToEcs(tamerEntity))
                {
                    // NOTE(api): API refactoring only
                    Debug.Assert(ownershipManager.OwnsEntity(entity));

                    if (BGUFunctionLibraryCS.BGUIsUnitInBattle(pawn))
                    {
                        BGUFuncLibAICS.SearchTargetSP(pawn);
                    }
                }
                else
                {
                    // NOTE(api): API refactoring only
                    Debug.Assert(!ownershipManager.OwnsEntity(entity));
                }
            });
        }
        else
        {
            _elapsedTime += Tick.deltaTime;
        }
    }
}