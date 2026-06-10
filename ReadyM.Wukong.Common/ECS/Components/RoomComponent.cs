using System.Runtime.InteropServices;
using ReadyM.Api.Multiplayer.Generators;

namespace ReadyM.Wukong.Common.ECS.Components;

[DeriveINetworkedComponent]
[StructLayout(LayoutKind.Auto)]
public partial struct RoomComponent
{
    private bool _chatEnabled;
    private bool _cheatsAllowed;
}