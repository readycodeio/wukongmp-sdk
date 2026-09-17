using ReadyM.SDK.Attributes;

namespace WukongMp.Sdk.Common.Archetypes;

[Archetype]
[Include(typeof(MainCharacterData))]
[Include(typeof(Nickname))]
[Include(typeof(Hp))]
[Include(typeof(Transform))]
[Include(typeof(Team))]
public readonly partial struct MainCharacter;