using b1;
using ReadyM.Api.ECS.Components;
using ReadyM.SDK.Attributes;
using UnrealEngine.Engine;
using WukongMp.Sdk.Common.Archetypes;

namespace WukongMp.Sdk.Archetypes.Mixins;

[ArchetypeMixin]
[Extends(typeof(MainCharacter))]
[ExplicitComponent(typeof(MappingComponent<AActor>))]
public readonly partial struct MappedCharacter
{
    [ExplicitMember(nameof(MappingComponent<>.GameObject))]
    public partial BGUCharacterCS? Pawn { get; }
}