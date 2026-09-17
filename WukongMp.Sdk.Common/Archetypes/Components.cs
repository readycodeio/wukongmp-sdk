using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.SDK.Attributes;
using ReadyM.Wukong.Common.ECS.Components;
using Yooni.Native.Container;

namespace WukongMp.Sdk.Common.Archetypes;

[ArchetypeMixin]
[ExplicitComponent(typeof(TamerComponent))]
public readonly partial struct TamerData
{
    public partial NativeString256 Guid { get; set; }
    public partial bool IsBossOrElite { get; set; }
}

[ArchetypeMixin]
[ExplicitComponent(typeof(TransformComponent))]
public readonly partial struct Transform;

[ArchetypeMixin]
[ExplicitComponent(typeof(HpComponent))]
public readonly partial struct Hp
{
    public partial float HpMaxBase { get; set; }
    public partial int HpMaxMulPercent { get; set; }
    public partial bool IsDead { get; set; }
}

[ArchetypeMixin]
[ExplicitComponent(typeof(NicknameComponent))]
public readonly partial struct Nickname;

[ArchetypeMixin]
[ExplicitComponent(typeof(TeamComponent))]
public readonly partial struct Team;

[ArchetypeMixin]
[ExplicitComponent(typeof(AnimationComponent))]
public readonly partial struct Animation;

[ArchetypeMixin]
[ExplicitComponent(typeof(MonsterAnimationComponent))]
public readonly partial struct MonsterAnimation;

[ArchetypeMixin]
[ExplicitComponent(typeof(MovieComponent))]
public readonly partial struct Movie
{
    // TODO: Native collections emit custom accessor methods
    // This wouldn't work anyway since _startedSequences is private 
    // [ExplicitMember("_startedSequences")]
    // public partial NativeList<int> StartedSequences { get; }
}

// TODO: Move to Core
[ArchetypeMixin]
[ExplicitComponent(typeof(AreaScopeComponent))]
public readonly partial struct AreaScope
{
    public partial AreaId AreaId { get; }
}