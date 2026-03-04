using System.Numerics;
using System.Runtime.InteropServices;
using Friflo.Engine.ECS;
using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer.Generators;
using ReadyM.Relay.Client.Mapping;
using ReadyM.Wukong.Common.ECS.Values;

namespace ReadyM.Wukong.Common.ECS.Components;

[DeriveINetworkedComponent]
[StructLayout(LayoutKind.Auto)]
public partial struct MainCharacterComponent() : IIndexedComponent<PlayerId>, IOwnershipManaged
{
    private PlayerId _playerId;

    private Vector3 _location;
    private Vector3 _rotation;
    private Vector3 _velocity;
    private Vector3 _moveAcceleration;
    private MoveSpeedLevel _moveSpeedLevel = MoveSpeedLevel.Run;
    private MoveSpeedLevel _moveSpeedState = MoveSpeedLevel.Run;

    private float _hp;
    private float _hpMaxBase;

    private int _rebirthPointId;
    private int _waitingSequenceId;

    private bool _isTransformed;

    // NOTE: This describes the nick displayed over the Wukong character
    private string _characterNickName = "";
    
    private bool _isDead;
    private bool _beguilingChantEligible;
    
    #region Animation

    private bool _inJump;
    private bool _isFlying;
    private bool _isFalling;
    private bool _isLandingMove;
    private Vector3 _turnInplaceTargetRotation;
    private bool _isStandRotate;
    private float _turnInplaceRemainAngle;
    private bool _isAttacking;
    private bool _orientRotationToMovement;
    private bool _shouldWaitRotateFinished;

    #endregion

    private AttributesState _attributes = new();
    private EquipmentState _equipment = new();

    public PlayerId GetIndexedValue()
        => PlayerId;
}
