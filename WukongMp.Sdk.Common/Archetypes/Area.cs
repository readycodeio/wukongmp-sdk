using ReadyM.SDK.Attributes;

namespace WukongMp.Sdk.Common.Archetypes;

[Archetype]
[Include(typeof(AreaScope))]
[Include(typeof(Movie))]
public readonly partial struct Area;