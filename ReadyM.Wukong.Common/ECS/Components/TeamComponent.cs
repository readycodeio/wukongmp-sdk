using System.Runtime.InteropServices;
using ReadyM.Api.Multiplayer.Generators;
using ReadyM.Api.Multiplayer.Mapping.Tags;

namespace ReadyM.Wukong.Common.ECS.Components;

[DeriveINetworkedComponent]
[StructLayout(LayoutKind.Auto)]
public partial struct TeamComponent : IReadyComponent, IOwnershipManaged
{
    private int _teamId;
}