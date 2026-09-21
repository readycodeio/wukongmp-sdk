using ReadyM.SDK.Attributes;
using WukongMp.Sdk.Common.Archetypes.Mixins;

namespace WukongMp.Sdk.Common.Archetypes;

[Archetype]
[Include(typeof(Character))]
[Include(typeof(TamerData))]
[Include(typeof(Animation))]
[Include(typeof(MonsterAnimation))]
public readonly partial struct Tamer;