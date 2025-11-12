using Friflo.Engine.ECS;
using System;

namespace WukongMp.Api
{
    public class GameplayEventRouter
    {
        public event Action<Entity, Entity>? OnUnitDead;
        public event Action<Entity, int>? OnRebirthPointChanged;

        public void RaiseOnUnitDead(Entity victimEntity, Entity attackerEntity)
        {
            OnUnitDead?.Invoke(victimEntity, attackerEntity);
        }

        public void RaiseOnRebirthPointChanged(Entity playerEntity, int rebirthPointId)
        {
            OnRebirthPointChanged?.Invoke(playerEntity, rebirthPointId);
        }
    }
}
