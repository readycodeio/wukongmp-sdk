using b1;
using BtlShare;
using Friflo.Engine.ECS;
using ReadyM.Relay.Common.ECS;
using ReadyM.Relay.Common.Wukong.Components;

namespace WukongMp.Api.ECS.Jobs;

public class ScaleMonsterHpJob(int scaling)
{
    public void Execute(EntityStore world)
    {
        world.Query<HpComponent, LocalTamerComponent>().ForEachEntity((ref hp, ref tamer, entity) =>
        {
            hp.HpMult = scaling;

            if (hp.LastMult == 0)
                hp.LastMult = 1;

            if (hp is { Hp: 0, HpMaxBase: 0 })
                return;

            // apply update immediately to discovered monsters
            if (hp.HpMult != hp.LastMult)
            {
                if (tamer.Pawn == null)
                    return;

                var attrs = BGU_DataUtil.GetReadOnlyData<BUC_AttrContainer>(tamer.Pawn);
                var currentHp = attrs.GetFloatValue(EBGUAttrFloat.Hp);
                var maxHp = attrs.GetFloatValue(EBGUAttrFloat.HpMax);

                hp.HpMaxBase = maxHp / hp.LastMult * hp.HpMult;
                hp.Hp = currentHp / hp.LastMult * hp.HpMult;

                attrs.SetFloatValue(EBGUAttrFloat.HpMaxBase, hp.HpMaxBase);
                attrs.SetFloatValue(EBGUAttrFloat.Hp, hp.Hp);

                hp.LastMult = hp.HpMult;
                Logging.LogDebug("Monster {Entity} HP scaling set to {Scaling}x", entity.Id, hp.HpMult);
            }
        });
    }
}