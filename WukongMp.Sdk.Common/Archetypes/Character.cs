using ReadyM.SDK.Attributes;
using WukongMp.Sdk.Common.Archetypes.Mixins;

namespace WukongMp.Sdk.Common.Archetypes;

[Archetype]
[Include(typeof(Transform))]
[Include(typeof(Health))]
[Include(typeof(DisplayedNickname))]
public readonly partial struct Character;