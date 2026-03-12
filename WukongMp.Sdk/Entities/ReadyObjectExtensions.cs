using System;
using System.Numerics;
using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.Wukong.Common.ECS.Components;
using WukongMp.Api;
using WukongMp.Api.ECS.Entities;
using AreaScopeComponent = ReadyM.Api.Multiplayer.ECS.Components.AreaScopeComponent;

namespace WukongMp.Sdk.Entities;

public static class ReadyObjectExtensions
{
    extension<TSelf>(TSelf obj)
        where TSelf : struct, IReadyEntity<TSelf>, IReadyConvertable<TSelf, ReadyObject> 
    {
        public PlayerId Owner
        {
            get
            {
                obj.Deconstruct(out _, out var entity);
                if (!entity.TryGetComponent<MetadataComponent>(out var metaComp))
                    throw new InvalidOperationException($"Entity does not have MetadataComponent: {entity.GetNetId()}");
                return metaComp.Owner;
            }
        }

        public AreaId? AreaId
        {
            get
            {
                obj.Deconstruct(out _, out var entity);
                if (!entity.TryGetComponent<InScopeComponent>(out var inScopeComp))
                    return null;
                if (!inScopeComp.ScopeEntity.TryGetComponent<AreaScopeComponent>(out var areaScopeEntity))
                    return null;
                return areaScopeEntity.AreaId;
            }
        }
        
        public Vector3 Location
        {
            get
            {
                obj.Deconstruct(out _, out var entity);
                return entity.TryGetComponent(out TransformComponent transComp) ? transComp.Position : throw new InvalidOperationException();
            }
            set
            {
                obj.Deconstruct(out _, out var entity);
                if (DI.Instance.MappedField.CanSetFromApi<TransformComponent>(entity, out var sync))
                {
                    sync.SetFromApi(TransformComponent.Fields.Position, value);
                }
                else
                {
                    throw new InvalidOperationException();
                }
            }
        }

        public Vector3 Rotation
        {
            get
            {
                obj.Deconstruct(out _, out var entity);
                return entity.TryGetComponent(out TransformComponent transComp) ? transComp.Rotation : throw new InvalidOperationException();
            }
            set
            {
                obj.Deconstruct(out _, out var entity);
                if (DI.Instance.MappedField.CanSetFromApi<TransformComponent>(entity, out var sync))
                {
                    sync.SetFromApi(TransformComponent.Fields.Rotation, value);
                }
                else
                {
                    throw new InvalidOperationException();
                }
            }
        }

        public void SetLocationRotation(Vector3 location, Vector3 rotation)
        {
            obj.Deconstruct(out _, out var entity);
            if (DI.Instance.MappedField.CanSetFromApi<TransformComponent>(entity, out var sync))
            {
                sync.SetFromApi(TransformComponent.Fields.Position, location);
                sync.SetFromApi(TransformComponent.Fields.Rotation, rotation);
            }
            else
            {
                throw new InvalidOperationException();
            }
        }
    }
}