using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using b1;
using BtlShare;
using Friflo.Engine.ECS;
using ReadyM.Api.Multiplayer.Idents;
using UnrealEngine.Runtime;
using WukongMp.Api.ECS.Values;

namespace WukongMp.Api.ECS.Components;

[StructLayout(LayoutKind.Sequential)]
public struct MainCharacterComponent() : IIndexedComponent<PlayerId>
{
    public PlayerId PlayerId { get; set; }
    
    public FVector Location { get; set; }
    public FRotator Rotation { get; set; }
    public FVector Velocity { get; set; }
    public FVector MoveAcceleration { get; set; }
    public EMoveSpeedLevel MoveSpeedLevel { get; set; } = EMoveSpeedLevel.Run;
    public EMoveSpeedLevel MoveSpeedState { get; set; } = EMoveSpeedLevel.Run;

    public float Hp { get; set; }
    public float HpMaxBase { get; set; }
    
    // NOTE: This describes the nick displayed over the Wukong character
    public string CharacterNickName { get; set; } = "";
    
    public bool IsDead => Hp <= 0;
    
    #region Animation

    public bool InJump { get; set; }
    public bool IsFlying { get; set; }
    public bool IsFalling { get; set; }
    public bool IsLandingMove { get; set; }
    public FRotator TurnInplaceTargetRotation { get; set; }
    public bool IsStandRotate { get; set; }
    public float TurnInplaceRemainAngle { get; set; }
    public bool IsAttacking { get; set; }
    public bool OrientRotationToMovement { get; set; }
    public bool ShouldWaitRotateFinished { get; set; }

    #endregion

    public ConcurrentDictionary<EBGUAttrFloat, float> Attributes { get; }
    public EquipmentState Equipment { get; set; }

    public PlayerId GetIndexedValue()
        => PlayerId;
}
