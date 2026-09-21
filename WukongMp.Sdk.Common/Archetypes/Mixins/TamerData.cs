using ReadyM.SDK.Attributes;
using ReadyM.Wukong.Common.ECS.Components;
using Yooni.Native.Container;

namespace WukongMp.Sdk.Common.Archetypes.Mixins;

[ArchetypeMixin]
[ExplicitComponent(typeof(TamerComponent))]
[ExplicitCollection("HoldingPlayers")]
public readonly partial struct TamerData
{
    public partial NativeString256 Guid { get; set; }
    public partial NativeString256 UnitPath { get; set; }
    public partial bool IsBossOrElite { get; set; }
    public partial bool HasFsmPaused { get; set; }
}