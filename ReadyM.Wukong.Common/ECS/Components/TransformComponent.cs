using System.Numerics;
using System.Runtime.InteropServices;
using ReadyM.Api.Multiplayer.Generators;
using ReadyM.Relay.Client.Mapping;

namespace ReadyM.Wukong.Common.ECS.Components;

[DeriveINetworkedComponent]
[StructLayout(LayoutKind.Auto)]
public partial struct TransformComponent : IOwnershipManaged
{
    private Vector3 _position;
    private Vector3 _rotation;
}