using System;
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
                return entity.TryGetComponent(out HpComponent hpComp) 
                    ? hpComp.Hp 
                    : throw new InvalidOperationException($"{nameof(HpComponent)} not present on entity");
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
                    Logging.LogError("Not allowed to set another player's HP");
                }
            }
        }

        public float HpMaxBase
        {
            get
            {
                obj.Deconstruct(out _, out var entity);
                return entity.TryGetComponent(out HpComponent hpComp) 
                    ? hpComp.HpMaxBase 
                    : throw new InvalidOperationException($"{nameof(HpComponent)} not present on entity");
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
                    Logging.LogError("Not allowed to set another player's max HP");
                }
            }
        }

        public int TeamId
        {
            get
            {
                obj.Deconstruct(out _, out var entity);
                return entity.TryGetComponent(out TeamComponent teamComp) 
                    ? teamComp.TeamId 
                    : throw new InvalidOperationException($"{nameof(TeamComponent)} not present on entity");
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
                    Logging.LogError("Not allowed to set another player's team ID");
                }
            }
        }

        public bool IsDead
        {
            get
            {
                obj.Deconstruct(out _, out var entity);
                return entity.TryGetComponent(out HpComponent hpComp) 
                    ? hpComp.IsDead 
                    : throw new InvalidOperationException($"{nameof(HpComponent)} not present on entity");
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