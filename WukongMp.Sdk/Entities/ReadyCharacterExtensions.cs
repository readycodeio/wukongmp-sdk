using System;
using ReadyM.Api.Multiplayer.ECS.Managers;
using ReadyM.Wukong.Common.ECS.Components;
using WukongMp.Api;
using WukongMp.Api.WukongUtils;

namespace WukongMp.Sdk.Entities;

public static class ReadyCharacterExtensions
{
    extension<TSelf>(TSelf obj)
        where TSelf : struct, IReadyEntity<TSelf>, IReadyConvertable<TSelf, ReadyCharacter>
    {
        public float Hp
        {
            get
            {
                obj.Deconstruct(out _, out var entity);
                return entity.TryGetComponent(out HpComponent hpComp) ? hpComp.Hp : throw new InvalidOperationException();
            }
            set
            {
                obj.Deconstruct(out _, out var entity);

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
                obj.Deconstruct(out _, out var entity);
                return entity.TryGetComponent(out HpComponent hpComp) ? hpComp.HpMaxBase : throw new InvalidOperationException();
            }
            set
            {
                obj.Deconstruct(out _, out var entity);

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
                obj.Deconstruct(out _, out var entity);
                if (!entity.TryGetComponent<TeamComponent>(out var teamComp))
                    throw new InvalidOperationException($"Entity does not have TeamComponent: {entity.GetNetId()}");
                return teamComp.TeamId;
            }
            set
            {
                obj.Deconstruct(out _, out var entity);

                if (DI.Instance.MappedField.CanSetFromApi<TeamComponent>(entity, out var sync))
                {
                    sync.SetFromApi(TeamComponent.Fields.TeamId, value);

                    if (DI.Instance.PlayerState.LocalPlayerEntity is { } player)
                    {
                        player.GetState().TeamId = value;
                    }
                }
                else
                {
                    throw new InvalidOperationException();
                }
            }
        }

        public bool IsDead
        {
            get
            {
                obj.Deconstruct(out _, out var entity);
                return entity.TryGetComponent(out HpComponent hpComp) ? hpComp.IsDead : throw new InvalidOperationException();
            }
        }

        public void SetMarkerMessage(string message, string color)
        {
            obj.Deconstruct(out _, out var entity);
            MarkerUtils.CreateMarkerForPlayer(entity, message, color);
        }

        public void HideMarker()
        {
            obj.Deconstruct(out _, out var entity);
            MarkerUtils.DestroyMarkerForCharacter(entity);
        }
    }
}