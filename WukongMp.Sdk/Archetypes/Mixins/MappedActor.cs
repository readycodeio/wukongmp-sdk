using ReadyM.Api.ECS.Components;
using ReadyM.SDK.Attributes;
using UnrealEngine.Engine;
using WukongMp.Sdk.Common.Archetypes;

namespace WukongMp.Sdk.Archetypes.Mixins;

[ArchetypeMixin]
[Extends(typeof(MainCharacter))]
[Extends(typeof(Tamer))]
[ExplicitComponent(typeof(MappingComponent<AActor>))]
public readonly partial struct MappedActor
{
    [ExplicitMember(nameof(MappingComponent<>.GameObject))]
    public partial AActor Pawn { get; }
}