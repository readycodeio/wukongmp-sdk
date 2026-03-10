using System;
using ReadyM.Wukong.Common.ECS.Components;
using WukongMp.Api;
using WukongMp.Api.ECS.Entities;

namespace WukongMp.Sdk;

public static class ReadyCharacterExtensions
{
    extension<TSelf>(TSelf obj)
        where TSelf : struct, IReadyEntity<TSelf>, IReadyConvertable<TSelf, ReadyCharacter>
    {
        public float Hp
        {
            get
            {
                default(TSelf).Deconstruct(obj, out _, out var entity);
                return entity.TryGetComponent(out HpComponent hpComp) ? hpComp.Hp : throw new InvalidOperationException();
            }
            set
            {
                default(TSelf).Deconstruct(obj, out _, out var entity);

                if (DI.Instance.MappedField.CanSetFromApi<HpComponent>(entity, out var sync))
                {
                    sync.SetFromApi(HpComponent.Fields.Hp, value);
                }
                else
                {
                    throw new InvalidOperationException();
                }
            }
        }

        public float HpMaxBase
        {
            get
            {
                default(TSelf).Deconstruct(obj, out _, out var entity);
                return entity.TryGetComponent(out HpComponent hpComp) ? hpComp.HpMaxBase : throw new InvalidOperationException();
            }
            set
            {
                default(TSelf).Deconstruct(obj, out _, out var entity);

                if (DI.Instance.MappedField.CanSetFromApi<HpComponent>(entity, out var sync))
                {
                    sync.SetFromApi(HpComponent.Fields.HpMaxBase, value);
                }
                else
                {
                    throw new InvalidOperationException();
                }
            }
        }

        public int TeamId
        {
            get
            {
                default(TSelf).Deconstruct(obj, out _, out var entity);
                if (!entity.TryGetComponent<TeamComponent>(out var teamComp))
                    throw new InvalidOperationException($"Entity does not have TeamComponent: {entity}");
                return teamComp.TeamId;
            }
        }

        public bool IsDead
        {
            get
            {
                default(TSelf).Deconstruct(obj, out _, out var entity);
                return entity.TryGetComponent(out HpComponent hpComp) ? hpComp.IsDead : throw new InvalidOperationException();
            }
        }
    }
}