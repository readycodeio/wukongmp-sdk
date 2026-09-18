using System.Numerics;
using ReadyM.Api.Idents;
using ReadyM.SDK.Attributes;
using ReadyM.Wukong.Common.ECS.Components;
using ReadyM.Wukong.Common.ECS.Values;

namespace WukongMp.Sdk.Common.Archetypes.Mixins;

[ArchetypeMixin]
[ExplicitComponent(typeof(MainCharacterComponent))]
public readonly partial struct MainCharacterData
{
    [Index]
    public partial PlayerId PlayerId { get; set; }

    public partial Vector3 Velocity { get; set; }
    public partial Vector3 MoveAcceleration { get; set; }
    public partial MoveSpeedLevel MoveSpeedLevel { get; set; }
    public partial MoveSpeedLevel MoveSpeedState { get; set; }

    public partial int RebirthPointId { get; set; }
    public partial int WaitingSequenceId { get; set; }

    public partial bool IsTransformed { get; set; }

    public partial SpectatorReason SpectatorReason { get; set; }
    public partial bool IsSpectator { get; set; }

    public partial bool BeguilingChantEligible { get; set; }

    #region Animation

    public partial bool InJump { get; set; }
    public partial bool IsFlying { get; set; }
    public partial bool IsFalling { get; set; }
    public partial bool IsLandingMove { get; set; }
    public partial Vector3 TurnInplaceTargetRotation { get; set; }
    public partial bool IsStandRotate { get; set; }
    public partial float TurnInplaceRemainAngle { get; set; }
    public partial bool IsAttacking { get; set; }
    public partial bool OrientRotationToMovement { get; set; }
    public partial bool ShouldWaitRotateFinished { get; set; }

    #endregion

    public partial AttributesState Attributes { get; set; }
    public partial EquipmentState Equipment { get; set; }

    public PlayerId GetIndexedValue()
        => PlayerId;
}