using b1;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using UnrealEngine.Engine;

namespace WukongMp.Api
{
    public class ColliderDisableData(ILogger logger)
    {
        private readonly Dictionary<AActor, float> _colliderDisableTimes = []; 

        public void DisableCollider(AActor actor, float disableDuration)
        {
            _colliderDisableTimes[actor] = disableDuration;
        }

        public void TryReEnableColliders(float deltaTime)
        {
            var collidersToEnable = new List<AActor>();
            foreach (var kvp in _colliderDisableTimes)
            {
                var collider = kvp.Key;
                var remainingTime = kvp.Value - deltaTime;
                if (remainingTime <= 0f)
                {
                    collidersToEnable.Add(collider);
                }
                else
                {
                    _colliderDisableTimes[collider] = remainingTime;
                }
            }
            foreach (var collider in collidersToEnable)
            {
                collider.SetActorEnableCollision(true);
                _colliderDisableTimes.Remove(collider);
                logger.LogDebug("Re-enabled collider for actor: {Actor}", BGU_DataUtil.GetActorGuid(collider));
            }
        }
    }
}
