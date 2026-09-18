using ReadyM.SDK.Attributes;
using UnrealEngine.Engine;
using WukongMp.Api.ECS.Components;
using WukongMp.Sdk.Common.Archetypes;

namespace WukongMp.Sdk.Archetypes.Mixins;

[ArchetypeMixin]
[Extends(typeof(MainCharacter))]
[Extends(typeof(Tamer))]
[ExplicitComponent(typeof(MarkerComponent))]
public readonly partial struct Marker
{
    public partial bool DestroyQueued { get; set; }
    public partial AActor? MarkerActor { get; set; }
}