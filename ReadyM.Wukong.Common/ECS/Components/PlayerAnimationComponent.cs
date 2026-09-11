using System.Numerics;
using System.Runtime.InteropServices;
using ReadyM.Api.Mapping.Tags;
using ReadyM.Api.Multiplayer.Generators;

namespace ReadyM.Wukong.Common.ECS.Components;

/// <summary>
/// Holds the state of the player character's animation.
/// </summary>
[DeriveINetworkedComponent]
[StructLayout(LayoutKind.Auto)]
public partial struct PlayerAnimationComponent : IOwnershipBased
{
    private bool _inJump;
    private bool _isFlying;
    private bool _isFalling;
    private bool _isLandingMove;
    private bool _isStandRotate;
    private bool _isAttacking;
    private bool _orientRotationToMovement;
    private bool _shouldWaitRotateFinished;
    
    private Vector3 _turnInplaceTargetRotation;
    private float _turnInplaceRemainAngle;
}