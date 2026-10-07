using ReadyM.SDK.Attributes;
using WukongMp.Sdk.Common.Archetypes.Mixins;

namespace WukongMp.Sdk.Common.Archetypes;

[Archetype(replicated: true)]
[Include(typeof(Character))]
[Include(typeof(MainCharacterData))]
public readonly partial struct MainCharacter;