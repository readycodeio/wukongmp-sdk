using b1;
using Friflo.Engine.ECS.Systems;
using ReadyM.Relay.Client.State;
using WukongMp.Api.Configuration;
using WukongMp.Api.ECS.Components;
using WukongMp.Api.ECS.Entities;

namespace WukongMp.Api.ECS.Systems.Tamers;

public sealed class ChangeTamerTargetSystem(ClientOwnershipManager clientOwnership) : QuerySystem<LocalTamerComponent>
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
                if (clientOwnership.OwnsEntity(entity) && BGUFunctionLibraryCS.BGUIsUnitInBattle(tamerEntity.Pawn))
                {
                    BGUFuncLibAICS.SearchTargetSP(tamerEntity.Pawn);
                }
            });
        }
        else
        {
            _elapsedTime += Tick.deltaTime;
        }
    }
}