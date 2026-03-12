using Friflo.Engine.ECS;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.Api.Multiplayer.ECS.Values;

namespace WukongMp.Api.ECS.Entities;

internal static class EntityExtensions
{
    extension(Entity entity)
    {
        public MetadataComponent GetMeta()
        {
            if (entity.IsNull)
                return default;
            if (!entity.TryGetComponent<MetadataComponent>(out var meta))
                return default;
            return meta;
        }

        public NetworkId GetNetId()
            => entity.GetMeta().NetId;
    }
}