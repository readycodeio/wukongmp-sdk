using ReadyM.SDK.Attributes;
using WukongMp.Sdk.Common.Archetypes.Mixins;

namespace WukongMp.Sdk.Common.Archetypes;

[Archetype]
[Include(typeof(TamerData))]
[Include(typeof(Transform))]
[Include(typeof(Health))]
[Include(typeof(Nickname))]
[Include(typeof(Team))]
[Include(typeof(Animation))]
[Include(typeof(MonsterAnimation))]
public readonly partial struct Tamer;