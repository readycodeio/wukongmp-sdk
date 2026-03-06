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
                if (TamerEntity.TryGetTamer(entity, out var tamerEntity))
                {
                    ref var hpComp = ref tamerEntity.Value.GetHp();
                    return hpComp.Hp;
                }

                if (MainCharacterEntity.TryGetMainCharacter(entity, out var mainEntity))
                {
                    ref var mainComp = ref mainEntity.Value.GetState();
                    return mainComp.Hp;
                }

                throw new InvalidOperationException();
            }
            set
            {
                default(TSelf).Deconstruct(obj, out _, out var entity);
                if (TamerEntity.TryGetTamer(entity, out var tamerEntity))
                {
                    if (DI.Instance.MappedField.CanSetFromApi<HpComponent>(tamerEntity.Value.Entity, out var sync))
                    {
                        sync.SetFromApi(HpComponent.Fields.Hp, value);
                    }
                }
                else if (MainCharacterEntity.TryGetMainCharacter(entity, out var mainEntity))
                {
                    if (DI.Instance.MappedField.CanSetFromApi<MainCharacterComponent>(mainEntity.Value.Entity, out var sync))
                    {
                        sync.SetFromApi(MainCharacterComponent.Fields.Hp, value);
                    }
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
                if (TamerEntity.TryGetTamer(entity, out var tamerEntity))
                {
                    ref var hpComp = ref tamerEntity.Value.GetHp();
                    return hpComp.HpMaxBase;
                }

                if (MainCharacterEntity.TryGetMainCharacter(entity, out var mainEntity))
                {
                    ref var mainComp = ref mainEntity.Value.GetState();
                    return mainComp.HpMaxBase;
                }

                throw new InvalidOperationException();
            }
            set
            {
                default(TSelf).Deconstruct(obj, out _, out var entity);
                if (TamerEntity.TryGetTamer(entity, out var tamerEntity))
                {
                    if (DI.Instance.MappedField.CanSetFromApi<HpComponent>(tamerEntity.Value.Entity, out var sync))
                    {
                        sync.SetFromApi(HpComponent.Fields.HpMaxBase, value);
                    }
                }
                else if (MainCharacterEntity.TryGetMainCharacter(entity, out var mainEntity))
                {
                    if (DI.Instance.MappedField.CanSetFromApi<MainCharacterComponent>(mainEntity.Value.Entity, out var sync))
                    {
                        sync.SetFromApi(MainCharacterComponent.Fields.HpMaxBase, value);
                    }
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
                if (TamerEntity.TryGetTamer(entity, out var tamerEntity))
                {
                    ref var hpComp = ref tamerEntity.Value.GetHp();
                    return hpComp.IsDead;
                }

                if (MainCharacterEntity.TryGetMainCharacter(entity, out var mainEntity))
                {
                    ref var mainComp = ref mainEntity.Value.GetState();
                    return mainComp.IsDead;
                }

                throw new InvalidOperationException();
            }
        }
    }
}