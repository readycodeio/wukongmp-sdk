using ReadyM.SDK.Attributes;
using ReadyM.SDK.Core;
using ReadyM.Wukong.Common.ECS.Components;
using Yooni.Native.Container;

namespace WukongMp.Sdk.Common.Archetypes.Mixins;

[ArchetypeMixin]
[ExplicitComponent(typeof(PlayerComponent))]
[Extends(typeof(Player))]
public readonly partial struct PlayerData
{
    public partial NativeString256 Nickname { get; set; }
    public partial int TeamId { get; set; }
}