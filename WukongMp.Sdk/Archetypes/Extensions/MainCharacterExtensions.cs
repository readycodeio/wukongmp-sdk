using Friflo.Engine.ECS;
using ReadyM.Api.Mapping.Tags;
using ReadyM.SDK.Client.Entities;
using ReadyM.SDK.Entities;
using WukongMp.Api;
using WukongMp.Api.ECS.GameEvents;
using WukongMp.Sdk.Archetypes.Mixins;
using WukongMp.Sdk.Common.Archetypes;

namespace WukongMp.Sdk.Archetypes.Extensions;

public static class MainCharacterExtensions
{
    extension(MainCharacter main)
    {
        public void RebirthInPlace()
        {
            // TODO: Cumbersome archetype -> Entity conversion
            DI.Instance.MappedEvent.InvokeInGameAndNotifyEcs(new RebirthPlayerEvent(EntityHandle.Of(main).ToEntity(DI.Instance.Resolve<EntityStore>()), false), default(EmptyContext));
        }

        public void RebirthAtShrine(int shrineId)
        {
            main.SetIsRespawning(true);

            // TODO: Cumbersome archetype -> Entity conversion
            DI.Instance.MappedEvent.InvokeInGameAndNotifyEcs(new PartyRespawnEvent(
                entity: EntityHandle.Of(main).ToEntity(DI.Instance.Resolve<EntityStore>()),
                birthShrineId: shrineId
            ), default(EmptyContext));
        }
    }
}