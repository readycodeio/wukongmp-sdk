using System.Runtime.InteropServices;
using ReadyM.Api.Multiplayer.Generators;
using ReadyM.Relay.Client.Mapping;

namespace ReadyM.Wukong.Common.ECS.Components;

[DeriveINetworkedComponent]
[StructLayout(LayoutKind.Auto)]
public partial struct TeamComponent : IOwnershipManaged
{
    private int _teamId;
}