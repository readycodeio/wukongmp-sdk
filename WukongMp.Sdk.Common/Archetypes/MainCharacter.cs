using ReadyM.SDK.Attributes;
using WukongMp.Sdk.Common.Archetypes.Mixins;

namespace WukongMp.Sdk.Common.Archetypes;

[Archetype]
[Include(typeof(MainCharacterData))]
[Include(typeof(Nickname))]
[Include(typeof(Health))]
[Include(typeof(Transform))]
[Include(typeof(Team))]
public readonly partial struct MainCharacter;