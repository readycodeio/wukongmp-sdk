using b1;
using ReadyM.Api.Mapping.Api;
using ReadyM.Wukong.Common.ECS.Components;
using ReadyM.Wukong.Common.ECS.Values;

namespace WukongMp.Api.Mapping;

public class WukongDataMappings
{
    public BoundField<MainCharacterComponent, float, BUC_AttrContainer> PlayerHp { get; set; }
    public BoundField<MainCharacterComponent, float, BUC_AttrContainer> PlayerHpMax { get; set; }
    public BoundField<MainCharacterComponent, AttributesState, BUC_AttrContainer> PlayerAttributes { get; set; }
    public BoundField<HpComponent, float, BUC_AttrContainer> Hp { get; set; }
    public BoundField<HpComponent, float, BUC_AttrContainer> HpMax { get; set; }
}