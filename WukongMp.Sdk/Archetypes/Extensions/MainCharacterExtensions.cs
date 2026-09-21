using System.Numerics;
using Friflo.Engine.ECS;
using ReadyM.Api.Mapping.Tags;
using ReadyM.SDK.Client.Entities;
using ReadyM.SDK.Entities;
using ReadyM.Wukong.Common.ECS.Values;
using WukongMp.Api;
using WukongMp.Api.ECS.GameEvents;
using WukongMp.Api.WukongUtils;
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

        public bool IsObserver => main is { IsSpectator: true, SpectatorReason: SpectatorReason.Api };

        public void Teleport(Vector3 location, Vector3 rotation)
        {
            DI.Instance.MappedEvent.InvokeInGameAndNotifyEcs(new RequestTeleportEvent(
                entity: EntityHandle.Of(main).ToEntity(DI.Instance.Resolve<EntityStore>()),
                location: location.ToFVector(),
                rotation: rotation.ToFRotator()
            ), default(EmptyContext));
        }
        
        public void EnableInteraction(bool enabled)
        {
            PlayerUtils.SetPlayerInteractionEnabled(main.Pawn, enabled);
        }
    }
}