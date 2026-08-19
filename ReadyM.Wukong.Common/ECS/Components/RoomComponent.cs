using System.Runtime.InteropServices;
using ReadyM.Api.Multiplayer.Generators;

namespace ReadyM.Wukong.Common.ECS.Components;

/// <summary>
/// Holds room configuration.
/// </summary>
[DeriveINetworkedComponent]
[StructLayout(LayoutKind.Auto)]
public partial struct RoomComponent
{
    private int _levelId;
    private int _tournamentRounds;
    private bool _gourdAllowed;
    private bool _consumablesAllowed;
    private bool _immobilizeAllowed;
    private bool _phantomRushAllowed;
    private int _enemiesNgPlusLevel;
    private bool _cheatsAllowed;
    private bool _chatEnabled;
    private bool _antiStallEnabled;
}