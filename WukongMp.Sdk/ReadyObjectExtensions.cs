using System;
using System.Numerics;
using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.Relay.Common.ECS.Components;
using WukongMp.Api.ECS.Entities;

namespace WukongMp.Sdk;

public static class ReadyObjectExtensions
{
    extension<TSelf>(TSelf obj)
        where TSelf : struct, IReadyEntity<TSelf>, IReadyConvertable<TSelf, ReadyObject> 
    {
        public PlayerId Owner
        {
            get
            {
                default(TSelf).Deconstruct(obj, out _, out var entity);
                if (!entity.TryGetComponent<MetadataComponent>(out var metaComp))
                    throw new InvalidOperationException($"Entity does not have MetadataComponent: {entity}");
                return metaComp.Owner;
            }
        }

        public AreaId? AreaId
        {
            get
            {
                default(TSelf).Deconstruct(obj, out _, out var entity);
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
                default(TSelf).Deconstruct(obj, out _, out var entity);
                if (MainCharacterEntity.TryGetMainCharacter(entity, out var mainEntity))
                {
                    ref var mainComp = ref mainEntity.Value.GetState();
                    return mainComp.Location;
                }
                else if (TamerEntity.TryGetTamer(entity, out var tamerEntity))
                {
                    ref var transComp = ref tamerEntity.Value.GetTransform();
                    return transComp.Position;
                }
                else
                    throw new InvalidOperationException($"Invalid entity type for GetLocation: {entity}");
            }
            set
            {
                default(TSelf).Deconstruct(obj, out _, out var entity);
                if (MainCharacterEntity.TryGetMainCharacter(entity, out var mainEntity))
                {
                    ref var mainComp = ref mainEntity.Value.GetState();
                    mainComp.Location = value;
                }
                else if (TamerEntity.TryGetTamer(entity, out var tamerEntity))
                {
                    ref var transComp = ref tamerEntity.Value.GetTransform();
                    transComp.Position = value;
                }
                else
                    throw new InvalidOperationException($"Invalid entity type for SetLocation: {entity}");
            }
        }

        public Vector3 Rotation
        {
            get
            {
                default(TSelf).Deconstruct(obj, out _, out var entity);
                if (MainCharacterEntity.TryGetMainCharacter(entity, out var mainEntity))
                {
                    ref var mainComp = ref mainEntity.Value.GetState();
                    return mainComp.Rotation;
                }
                else if (TamerEntity.TryGetTamer(entity, out var tamerEntity))
                {
                    ref var transComp = ref tamerEntity.Value.GetTransform();
                    return transComp.Rotation;
                }
                else
                    throw new InvalidOperationException($"Invalid entity type for GetRotation: {entity}");
            }
            set
            {
                default(TSelf).Deconstruct(obj, out _, out var entity);
                if (MainCharacterEntity.TryGetMainCharacter(entity, out var mainEntity))
                {
                    ref var mainComp = ref mainEntity.Value.GetState();
                    mainComp.Rotation = value;
                }
                else if (TamerEntity.TryGetTamer(entity, out var tamerEntity))
                {
                    ref var transComp = ref tamerEntity.Value.GetTransform();
                    transComp.Rotation = value;
                }
                else
                    throw new InvalidOperationException($"Invalid entity type for SetRotation: {entity}");
            }
        }

        public void SetLocationRotation(Vector3 location, Vector3 rotation)
        {
            default(TSelf).Deconstruct(obj, out _, out var entity);
            if (MainCharacterEntity.TryGetMainCharacter(entity, out var mainEntity))
            {
                ref var mainComp = ref mainEntity.Value.GetState();
                mainComp.Location = location;
                mainComp.Rotation = rotation;
            }
            else if (TamerEntity.TryGetTamer(entity, out var tamerEntity))
            {
                ref var transComp = ref tamerEntity.Value.GetTransform();
                transComp.Position = location;
                transComp.Rotation = rotation;
            }
            else
                throw new InvalidOperationException($"Invalid entity type for SetRotation: {entity}");
        }
    }
}