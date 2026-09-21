using ReadyM.SDK.Attributes;
using ReadyM.SDK.Core;
using ReadyM.Wukong.Common.ECS.Components;

namespace WukongMp.Sdk.Common.Archetypes.Mixins;

[ArchetypeMixin]
[ExplicitComponent(typeof(MovieComponent))]
[ExplicitCollection("StartedSequences")]
[Extends(typeof(Area))]
public readonly partial struct Movie;