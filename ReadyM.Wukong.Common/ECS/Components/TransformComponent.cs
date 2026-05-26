using System.Numerics;
using System.Runtime.InteropServices;
using ReadyM.Api.Mapping.Tags;
using ReadyM.Api.Multiplayer.Generators;

namespace ReadyM.Wukong.Common.ECS.Components;

[DeriveINetworkedComponent]
[StructLayout(LayoutKind.Auto)]
public partial struct TransformComponent : IReadyComponent, IOwnershipManaged
{
    private Vector3 _position;
    private Vector3 _rotation;
}