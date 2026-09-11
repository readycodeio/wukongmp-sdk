using System.Runtime.InteropServices;
using ReadyM.Api.Mapping.Tags;
using ReadyM.Api.Multiplayer.Generators;

namespace ReadyM.Wukong.Common.ECS.Components;

/// <summary>
/// Holds the state of the monster's animation.
/// </summary>
[DeriveINetworkedComponent]
[StructLayout(LayoutKind.Auto)]
public partial struct MonsterAnimationComponent : IOwnershipBased
{
    private byte _moveAiType;
    private float _animationPlayRate;
    
    private byte _moveSpeedLevel;
    private byte _moveSpeedState;
    private bool _shouldWaitRotateFinished;
}