using b1;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using WukongMp.Api.Configuration;
using WukongMp.Api.ECS.Components;

namespace WukongMp.Api.ECS.Systems.Tamers;

public sealed class ChangeTamerTargetSystem : QuerySystem<LocalTamerComponent>
{
    private float _elapsedTime;

    protected override void OnUpdate()
    {
        if (_elapsedTime >= Constants.MonsterUpdateTargetTime)
        {
            _elapsedTime = 0;

            Query.ForEachEntity((
                ref localTamerComp, entity) =>
            {
                if (DI.Instance.ClientOwnership.OwnsEntity(entity) && BGUFunctionLibraryCS.BGUIsUnitInBattle(localTamerComp.Pawn))
                {
                    BGUFuncLibAICS.SearchTargetSP(localTamerComp.Pawn);
                }
            });
        }
        else
        {
            _elapsedTime += Tick.deltaTime;
        }
    }
}