using System.Runtime.InteropServices;
using ReadyM.Api.Multiplayer.Generators;
using ReadyM.Relay.Client.Mapping;

namespace ReadyM.Wukong.Common.ECS.Components;

[DeriveINetworkedComponent]
[StructLayout(LayoutKind.Auto)]
public partial struct HpComponent : IOwnershipManaged
{
    private float _hp;
    private float _hpMaxBase;
    private float _hpMultiplier;
    public bool IsDead => Hp <= 0 && HpMaxBase > 0;
}