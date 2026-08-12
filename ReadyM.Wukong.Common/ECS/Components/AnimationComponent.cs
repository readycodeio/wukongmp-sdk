using System.Numerics;
using System.Runtime.InteropServices;
using ReadyM.Api.Mapping.Tags;
using ReadyM.Api.Multiplayer.Generators;

namespace ReadyM.Wukong.Common.ECS.Components;

/// <summary>
/// Holds entity movement animation state.
/// </summary>
[DeriveINetworkedComponent]
[StructLayout(LayoutKind.Auto)]
public partial struct AnimationComponent : IOwnershipBased
{
    private Vector3 _velocity;
    private Vector3 _moveAcceleration;
    private byte _moveSpeedLevel;
    private byte _moveSpeedState;
    private bool _shouldWaitRotateFinished;
}