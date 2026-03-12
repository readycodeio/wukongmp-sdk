using System.Numerics;
using System.Runtime.InteropServices;
using ReadyM.Api.Multiplayer.Generators;
using ReadyM.Api.Multiplayer.Mapping.Tags;

namespace ReadyM.Wukong.Common.ECS.Components;

[DeriveINetworkedComponent]
[StructLayout(LayoutKind.Auto)]
public partial struct TransformComponent : IReadyComponent, IOwnershipManaged
{
    private Vector3 _position;
    private Vector3 _rotation;
}