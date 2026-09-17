using ReadyM.SDK.Attributes;

namespace WukongMp.Sdk.Common.Archetypes;

[Archetype]
[Include(typeof(TamerData))]
[Include(typeof(Transform))]
[Include(typeof(Hp))]
[Include(typeof(Nickname))]
[Include(typeof(Team))]
[Include(typeof(Animation))]
[Include(typeof(MonsterAnimation))]
public readonly partial struct Tamer;