using Friflo.Engine.ECS;
using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.SDK.Client.Entities;
using ReadyM.SDK.Entities;
using WukongMp.Api;
using WukongMp.Api.WukongUtils;
using WukongMp.Sdk.Common.Archetypes;

namespace WukongMp.Sdk.Archetypes.Extensions;

public static class CharacterExtensions
{
    // TODO: Generic over everything that includes this archetype
    extension(Character character)
    {
        public void SetMarkerMessage(string message, string color)
        {
            var entity = EntityHandle.Of(character).ToEntity(DI.Instance.Resolve<EntityStore>());
            MarkerUtils.CreateMarkerForPlayer(entity, message, color);
        }

        public void HideMarker()
        {
            var entity = EntityHandle.Of(character).ToEntity(DI.Instance.Resolve<EntityStore>());
            MarkerUtils.DestroyMarkerForCharacter(entity);
        }

        public PlayerId Owner
        {
            get
            {
                var entity = EntityHandle.Of(character).ToEntity(DI.Instance.Resolve<EntityStore>());
                return entity.GetComponent<MetadataComponent>().Owner;
            }
        }
    }
}