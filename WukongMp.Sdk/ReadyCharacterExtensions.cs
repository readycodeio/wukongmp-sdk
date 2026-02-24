using System;
using ReadyM.Relay.Common.Wukong.ECS.Components;
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
                else if (MainCharacterEntity.TryGetMainCharacter(entity, out var mainEntity))
                {
                    ref var mainComp = ref mainEntity.Value.GetState();
                    return mainComp.Hp;
                }
                else
                {
                    throw new InvalidOperationException();
                }
            }
            set
            {
                default(TSelf).Deconstruct(obj, out _, out var entity);
                if (TamerEntity.TryGetTamer(entity, out var tamerEntity))
                {
                    ref var hpComp = ref tamerEntity.Value.GetHp();
                    hpComp.Hp = value;
                }
                else if (MainCharacterEntity.TryGetMainCharacter(entity, out var mainEntity))
                {
                    ref var mainComp = ref mainEntity.Value.GetState();
                    mainComp.Hp = value;
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
                else if (MainCharacterEntity.TryGetMainCharacter(entity, out var mainEntity))
                {
                    ref var mainComp = ref mainEntity.Value.GetState();
                    return mainComp.HpMaxBase;
                }
                else
                {
                    throw new InvalidOperationException();
                }
            }
            set
            {
                default(TSelf).Deconstruct(obj, out _, out var entity);
                if (TamerEntity.TryGetTamer(entity, out var tamerEntity))
                {
                    ref var hpComp = ref tamerEntity.Value.GetHp();
                    hpComp.HpMaxBase = value;
                }
                else if (MainCharacterEntity.TryGetMainCharacter(entity, out var mainEntity))
                {
                    ref var mainComp = ref mainEntity.Value.GetState();
                    mainComp.HpMaxBase = value;
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
            set
            {
                default(TSelf).Deconstruct(obj, out _, out var entity);
                if (!entity.TryGetComponent<TeamComponent>(out var teamComp))
                    throw new InvalidOperationException($"Entity does not have TeamComponent: {entity}");
                teamComp.TeamId = value;
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
                else if (MainCharacterEntity.TryGetMainCharacter(entity, out var mainEntity))
                {
                    ref var mainComp = ref mainEntity.Value.GetState();
                    return mainComp.IsDead;
                }
                else
                {
                    throw new InvalidOperationException();
                }
            }
        }
    }
}