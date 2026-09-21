using b1;
using ReadyM.Api.ECS.Components;
using ReadyM.SDK.Attributes;
using WukongMp.Sdk.Common.Archetypes;

namespace WukongMp.Sdk.Archetypes.Mixins;

[ArchetypeMixin]
[Extends(typeof(Tamer))]
[ExplicitComponent(typeof(MappingComponent<BUTamerActor>))]
public readonly partial struct MappedMonster
{
    [ExplicitMember(nameof(MappingComponent<>.GameObject))]
    public partial BUTamerActor? TamerActor { get; }

    public BGUCharacterCS? Pawn => TamerActor?.GetMonster();
}