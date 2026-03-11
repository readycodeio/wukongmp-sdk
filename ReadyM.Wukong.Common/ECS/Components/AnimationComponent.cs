using System.Numerics;
using System.Runtime.InteropServices;
using ReadyM.Api.Multiplayer.Generators;
using ReadyM.Api.Multiplayer.Mapping;
using ReadyM.Api.Multiplayer.Mapping.Tags;

namespace ReadyM.Wukong.Common.ECS.Components;

[DeriveINetworkedComponent]
[StructLayout(LayoutKind.Auto)]
public partial struct AnimationComponent : IReadyComponent, IOwnershipManaged
{
    private Vector3 _velocity;
    private Vector3 _moveAcceleration;
    private byte _moveSpeedLevel;
    private byte _moveSpeedState;
    private bool _shouldWaitRotateFinished;
}