using System.Runtime.InteropServices;
using ReadyM.Api.Mapping.Tags;
using ReadyM.Api.Multiplayer.Generators;

namespace ReadyM.Wukong.Common.ECS.Components;

[DeriveINetworkedComponent]
[StructLayout(LayoutKind.Auto)]
public partial struct HpComponent : IReadyComponent, IOwnershipManaged
{
    private float _hp;
    private float _hpMaxBase;
    private bool _isDead;
    private float _hpMultiplier;
}