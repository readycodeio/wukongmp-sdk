using System.Numerics;
using System.Runtime.InteropServices;
using ReadyM.Api.Mapping.Tags;
using ReadyM.Api.Multiplayer.Generators;

namespace ReadyM.Wukong.Common.ECS.Components;

/// <summary>
/// Holds entity position and rotation state.
/// </summary>
[DeriveINetworkedComponent]
[StructLayout(LayoutKind.Auto)]
public partial struct TransformComponent : IOwnershipBased
{
    private Vector3 _position;
    private Vector3 _rotation;
}