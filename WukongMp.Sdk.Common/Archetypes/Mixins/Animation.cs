using ReadyM.SDK.Attributes;
using ReadyM.Wukong.Common.ECS.Components;

namespace WukongMp.Sdk.Common.Archetypes.Mixins;

[ArchetypeMixin(Replication.OwnershipBased)]
[ExplicitComponent(typeof(AnimationComponent))]
public readonly partial struct Animation;