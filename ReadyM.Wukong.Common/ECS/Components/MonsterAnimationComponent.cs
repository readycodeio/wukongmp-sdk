using System.Runtime.InteropServices;
using ReadyM.Api.Multiplayer.Generators;

namespace ReadyM.Wukong.Common.ECS.Components;

/// <summary>
/// Holds the state of the monster's animation.
/// </summary>
[DeriveINetworkedComponent]
[StructLayout(LayoutKind.Auto)]
public partial struct MonsterAnimationComponent
{
    private byte _moveAiType;
    private float _animationPlayRate;
}