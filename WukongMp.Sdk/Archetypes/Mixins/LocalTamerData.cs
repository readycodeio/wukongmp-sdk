using ReadyM.SDK.Attributes;
using WukongMp.Api.ECS.Components;
using WukongMp.Sdk.Common.Archetypes;

namespace WukongMp.Sdk.Archetypes.Mixins;

[ArchetypeMixin]
[Extends(typeof(Tamer))]
[ExplicitComponent(typeof(LocalTamerComponent))]
public readonly partial struct LocalTamerData
{
    public partial bool IsMonsterActive { get; set; }
}