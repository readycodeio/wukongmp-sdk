using System.Runtime.InteropServices;
using ReadyM.Api.Multiplayer.Generators;

namespace ReadyM.Wukong.Common.ECS.Components;

[DeriveINetworkedComponent]
[StructLayout(LayoutKind.Auto)]
public partial struct PlayerComponent
{
    // NOTE: This defines the player name used in chat etc.
    private string _nickname;
    
    // NOTE: This is the players' Team ID, used in PvP, possibly in the future in creative mode
    // This is separate separated out from the TeamID on the main character which describes directly the team of the
    // underlying game actor.
    private int _teamId;
}