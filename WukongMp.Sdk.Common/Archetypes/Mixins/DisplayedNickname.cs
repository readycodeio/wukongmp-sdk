using ReadyM.SDK.Attributes;
using ReadyM.Wukong.Common.ECS.Components;
using Yooni.Native.Container;

namespace WukongMp.Sdk.Common.Archetypes.Mixins;

[ArchetypeMixin]
[ExplicitComponent(typeof(NicknameComponent))]
public readonly partial struct DisplayedNickname
{
    public partial NativeString256 Nickname { get; set; }
}