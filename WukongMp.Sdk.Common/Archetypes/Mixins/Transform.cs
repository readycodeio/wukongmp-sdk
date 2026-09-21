using System.Numerics;
using ReadyM.SDK.Attributes;
using ReadyM.Wukong.Common.ECS.Components;

namespace WukongMp.Sdk.Common.Archetypes.Mixins;

[ArchetypeMixin]
[ExplicitComponent(typeof(TransformComponent))]
public readonly partial struct Transform
{
    public partial Vector3 Position { get; set; }
    public partial Vector3 Rotation { get; set; }
}