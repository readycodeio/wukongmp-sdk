using System.Runtime.InteropServices;
using ReadyM.Api.Multiplayer.Generators;
using ReadyM.Api.Multiplayer.Mapping;
using ReadyM.Api.Multiplayer.Mapping.Tags;

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