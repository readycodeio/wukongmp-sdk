using System.Runtime.InteropServices;
using ReadyM.Api.Multiplayer.Generators;

namespace ReadyM.Wukong.Common.ECS.Components;

[DeriveINetworkedComponent]
[StructLayout(LayoutKind.Auto)]
public partial struct MonsterAnimationComponent
{
    private byte _moveAiType;
    private float _animationPlayRate;
}