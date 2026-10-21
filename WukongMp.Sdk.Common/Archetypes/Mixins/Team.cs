using ReadyM.SDK.Attributes;
using ReadyM.Wukong.Common.ECS.Components;

namespace WukongMp.Sdk.Common.Archetypes.Mixins;

[ArchetypeMixin(Replication.OwnershipBased)]
[ExplicitComponent(typeof(TeamComponent))]
public readonly partial struct Team
{
    public partial int TeamId { get; set; }
}