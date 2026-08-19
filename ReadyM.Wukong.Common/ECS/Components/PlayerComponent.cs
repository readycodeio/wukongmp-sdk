using System.Runtime.InteropServices;
using ReadyM.Api.Multiplayer.Generators;
using Yooni.Native.Container;

namespace ReadyM.Wukong.Common.ECS.Components;

[DeriveINetworkedComponent]
[StructLayout(LayoutKind.Auto)]
public partial struct PlayerComponent
{
    /// <summary>
    /// Globally persisted player nickname.
    /// <see cref="NicknameComponent"/> attached to an area-scoped player character entity is recreated from this value on area change.
    /// </summary>
    private NativeString256 _nickname;

    /// <summary>
    /// This is the players' Team ID, used in PvP, possibly in the future in creative mode
    /// This is separate separated out from the TeamID on the main character which describes directly the team of the
    /// underlying game actor.
    /// </summary>
    private int _teamId;
}