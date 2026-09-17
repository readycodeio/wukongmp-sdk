using ReadyM.SDK.Attributes;
using ReadyM.Wukong.Common.ECS.Components;

namespace WukongMp.Sdk.Common.Archetypes.Mixins;

[ArchetypeMixin]
[ExplicitComponent(typeof(MovieComponent))]
[ExplicitCollection("StartedSequences")]
public readonly partial struct Movie;