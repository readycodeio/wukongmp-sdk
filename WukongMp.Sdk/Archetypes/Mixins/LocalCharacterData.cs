using ReadyM.SDK.Attributes;
using WukongMp.Api.ECS.Components;
using WukongMp.Sdk.Common.Archetypes;

namespace WukongMp.Sdk.Archetypes.Mixins;

[ArchetypeMixin]
[Extends(typeof(MainCharacter))]
[ExplicitComponent(typeof(LocalMainCharacterComponent))]
public readonly partial struct LocalCharacterData
{
    public partial bool IsWaitingForSequence { get; set; }
    public partial bool IsRespawning { get; set; }
}