using ReadyM.SDK.Attributes;
using ReadyM.Wukong.Common.ECS.Components;

namespace WukongMp.Sdk.Common.Archetypes.Mixins;

[ArchetypeMixin]
[ExplicitComponent(typeof(HpComponent))]
public readonly partial struct Health
{
    public partial float Hp { get; set; }
    public partial float HpMaxBase { get; set; }
    public partial int HpMaxMulPercent { get; set; }
    public partial bool IsDead { get; set; }
}