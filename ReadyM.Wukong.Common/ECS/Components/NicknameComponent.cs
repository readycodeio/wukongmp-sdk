using System.Runtime.InteropServices;
using ReadyM.Api.Mapping.Tags;
using ReadyM.Api.Multiplayer.Generators;
using Yooni.Native.Container;

namespace ReadyM.Wukong.Common.ECS.Components;

/// <summary>
/// Holds the displayed nickname of an entity.
/// </summary>
[DeriveINetworkedComponent]
[StructLayout(LayoutKind.Auto)]
public partial struct NicknameComponent : IOwnershipBased
{
    private NativeString256 _nickname;
}