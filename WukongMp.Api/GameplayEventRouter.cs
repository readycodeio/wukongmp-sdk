using Friflo.Engine.ECS;
using System;

namespace WukongMp.Api
{
    public class GameplayEventRouter
    {
        public event Action<Entity, Entity>? OnUnitDead;

        public void RaiseOnUnitDead(Entity unitEntity, Entity killerEntity)
        {
            OnUnitDead?.Invoke(unitEntity, killerEntity);
        }
    }
}
