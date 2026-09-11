using System.Numerics;
using System.Runtime.InteropServices;
using ReadyM.Api.Mapping.Tags;
using ReadyM.Api.Multiplayer.Generators;

namespace ReadyM.Wukong.Common.ECS.Components;

/// <summary>
/// Holds character's or tamers velocity and acceleration, used by the animation system.
/// </summary>
[DeriveINetworkedComponent]
[StructLayout(LayoutKind.Auto)]
public partial struct VelocityComponent : IOwnershipBased
{
    private Vector3 _velocity;
    private Vector3 _moveAcceleration;
}